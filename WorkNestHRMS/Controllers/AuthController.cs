using Microsoft.AspNetCore.Mvc;
using WorkNestHRMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace WorkNestHRMS.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AuthController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = new JwtSettings();
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("JwtSettings")
            .Bind(jwtSettings);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        //new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // tu userId, chyba sie uda 12.12.24 - TEMP (stosować w następnych kontrolerach jak sie uda)
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("role", user.Role),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(0.5),
            signingCredentials: creds
        );
        Console.WriteLine($"Claims in generated token: {string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}"))}");
        return new JwtSecurityTokenHandler().WriteToken(token);

    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        Console.WriteLine($"Register request: Username={registerDto.Username}, Password={registerDto.Password}, Role={registerDto.Role}");  // czy mamy dane do rejestracji w kontekscie DbContnext

        if (await _dbContext.Users.AnyAsync(u => u.Username == registerDto.Username))
            return BadRequest("Użytkownik o takiej nazwie już istnieje.");

        var validRoles = new[] { "user", "admin" };
        if (!validRoles.Contains(registerDto.Role.ToLower()))
        {
            return BadRequest("Nieprawidłowa rola użytkownika.");
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        Console.WriteLine($"Hashed password: {hashedPassword}");

        var newUser = new User
        {
            Username = registerDto.Username,
            PasswordHash = hashedPassword,
            Role = registerDto.Role.ToLower()   // raczej rozwiąże problem z "no resources" - TEMP
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"Nowy użytkownik zapisany z ID: {newUser.Id}");

        // pustey employee
        var newEmployee = new Employee
        {
            UserId = newUser.Id,
            Id = newUser.Id 
        };

        Console.WriteLine($"Tworzenie nowego pracownika z UserId: {newEmployee.UserId}");

        _dbContext.Employees.Add(newEmployee);
        try
        {
            await _dbContext.SaveChangesAsync(); 
            Console.WriteLine("Zapisano obiekt Employee w bazie danych.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu obiektu Employee: {ex.Message}");
            return StatusCode(500, "Wystąpił błąd podczas zapisywania danych.");
        }
        return Ok("Użytkownik zarejestrowany pomyślnie.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        Console.WriteLine($"Login request: Username={loginDto.Username}, Password={loginDto.Password}");

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Username == loginDto.Username);
        if (user == null)
        {
            Console.WriteLine("User not found.");
            return Unauthorized("Nieprawidłowa nazwa użytkownika lub hasło.");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        Console.WriteLine($"Password valid: {isPasswordValid}");

        if (!isPasswordValid)
            return Unauthorized("Nieprawidłowa nazwa użytkownika lub hasło.");

        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }

    [HttpPost("refresh")]
    public IActionResult RefreshToken([FromBody] string expiredToken)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtSettings = new JwtSettings();
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("JwtSettings")
            .Bind(jwtSettings);

        try
        {
            var principal = jwtHandler.ValidateToken(expiredToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // TEMP (póki nie rozwiążemy problemu z wyrzucaniem edit1: rozciągnąć exp w tokenie i włączyć na true)
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            }, out var securityToken);

            if (securityToken is JwtSecurityToken jwtSecurityToken &&
                jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var username = principal.Identity?.Name;
                var user = _dbContext.Users.SingleOrDefault(u => u.Username == username);

                if (user == null)
                    return Unauthorized("Invalid token.");

                var newToken = GenerateJwtToken(user);
                return Ok(new { Token = newToken });
            }
            return Unauthorized("Invalid token.");
        }
        catch (Exception ex)
        {
            return Unauthorized("Invalid token: " + ex.Message);
        }
    }
}
