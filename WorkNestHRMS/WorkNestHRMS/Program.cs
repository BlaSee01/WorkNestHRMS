using Microsoft.EntityFrameworkCore;
using WorkNestHRMS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// Dodanie CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Dodanie Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dodanie JWT Authentication
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);

/*IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
{
    // Tu mo�na pobiera� klucz z bazy danych lub us�ugi
    return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)) };
},*/

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            NameClaimType = ClaimTypes.Name, // Poprawne mapowanie dla User.Identity.Name
            RoleClaimType = ClaimTypes.Role
        };


    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("user"));
});


builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);


var app = builder.Build();

// W��czenie CORS
app.UseCors("AllowAll");

// W��czenie uwierzytelniania i autoryzacji
app.UseAuthentication();

app.Use(async (context, next) =>
{
    Console.WriteLine("=== Middleware Debug Start ===");
    Console.WriteLine($"Request Path: {context.Request.Path}");

    if (context.User.Identity?.IsAuthenticated == true)
    {
        var username = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Brak ClaimTypes.Name";
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "Brak ClaimTypes.Role";

        Console.WriteLine($"Authenticated User: {username}, Role: {role}");

        // Debugowanie wszystkich claim�w
        foreach (var claim in context.User.Claims)
        {
            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
        }
    }
    else
    {
        Console.WriteLine("User not authenticated.");
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        Console.WriteLine($"Authorization Header: {authHeader}");
    }

    Console.WriteLine("=== Middleware Debug End ===");
    await next();
});


app.UseAuthorization();

app.MapControllers();

app.Run();




