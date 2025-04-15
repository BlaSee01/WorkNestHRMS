using Microsoft.EntityFrameworkCore;
using WorkNestHRMS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// £adujemy .env dopiero TERAZ
Env.Load();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Connection string z .env
var connectionString = $"Host={Env.GetString("DB_HOST")};Database={Env.GetString("DB_NAME")};Username={Env.GetString("DB_USER")};Password={Env.GetString("DB_PASSWORD")}";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT z .env
var jwtSettings = new JwtSettings
{
    Issuer = Env.GetString("JWT_ISSUER"),
    Audience = Env.GetString("JWT_AUDIENCE"),
    SecretKey = Env.GetString("JWT_SECRET")
};

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
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("user"));
});

builder.Services.Configure<JwtSettings>(opts =>
{
    opts.Issuer = jwtSettings.Issuer;
    opts.Audience = jwtSettings.Audience;
    opts.SecretKey = jwtSettings.SecretKey;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

var app = builder.Build();

app.UseCors("AllowAll");

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
