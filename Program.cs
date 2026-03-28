using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using TaskManager.API.Data;
using TaskManager.API.Interfaces;
using TaskManager.API.Middleware;
using TaskManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskManager API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token in the format: Bearer {your token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});


// Database (Render: link Postgres so DATABASE_URL uses the internal host, or set ConnectionStrings__DefaultConnection to Internal Database URL)

var postgresConnection = ResolvePostgresConnectionString(builder.Configuration);
if (string.IsNullOrWhiteSpace(postgresConnection))
    throw new InvalidOperationException("PostgreSQL connection missing: set ConnectionStrings:DefaultConnection or DATABASE_URL (e.g. from Render linked DB).");

// Log connection details for debugging (without sensitive data)
var connectionBuilder = new NpgsqlConnectionStringBuilder(postgresConnection);
Console.WriteLine($"Connecting to PostgreSQL at: {connectionBuilder.Host}:{connectionBuilder.Port}, Database: {connectionBuilder.Database}, SSL Mode: {connectionBuilder.SslMode}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(postgresConnection));


// Dependency Injection

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();


// JWT Authentication

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key!)
            )
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<ITaskService, TaskService>();

/*builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});*/

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<GeminiService>();

var app = builder.Build();

app.UseCors("AllowAll");


// Middleware pipeline

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Render (and similar hosts) set PORT; the edge proxy forwards to that port, not a hardcoded 8080.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var listenPort))
    app.Urls.Add($"http://0.0.0.0:{listenPort}");
else
    app.Urls.Add("http://0.0.0.0:8080");

app.Run();

 /*
 {
  "name": "Admin User",
  "email": "admin@test.com",
  "password": "Password@123",
  "roleId": 1
 }
 */

static string? ResolvePostgresConnectionString(IConfiguration configuration)
{
    var configured = configuration.GetConnectionString("DefaultConnection")?.Trim().Trim('"');
    if (!string.IsNullOrWhiteSpace(configured))
    {
        if (IsPostgresUri(configured))
            return ConvertDatabaseUrlToNpgsql(configured);
        return configured;
    }

    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")?.Trim().Trim('"');
    return string.IsNullOrWhiteSpace(databaseUrl) ? null : ConvertDatabaseUrlToNpgsql(databaseUrl);
}

static bool IsPostgresUri(string s) =>
    s.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
    || s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

static string ConvertDatabaseUrlToNpgsql(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo;
    var colonIndex = userInfo.IndexOf(':');
    var username = colonIndex >= 0
        ? Uri.UnescapeDataString(userInfo[..colonIndex])
        : Uri.UnescapeDataString(userInfo);
    var password = colonIndex >= 0
        ? Uri.UnescapeDataString(userInfo[(colonIndex + 1)..])
        : string.Empty;

    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port > 0 ? uri.Port : 5432;

    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = port,
        Database = database,
        Username = username,
        Password = password
    };

    // Handle different database providers
    if (uri.Host.EndsWith(".render.internal", StringComparison.OrdinalIgnoreCase))
    {
        // Render internal database
        csb.SslMode = SslMode.Disable;
    }
    else if (uri.Host.Contains("supabase.co", StringComparison.OrdinalIgnoreCase))
    {
        // Supabase database - use SSL and specific settings
        csb.SslMode = SslMode.Require;
        // Force IPv4 for Supabase to avoid IPv6 connectivity issues in Docker
        csb.Host = GetIPv4Host(uri.Host);
    }
    else
    {
        // Other external databases
        csb.SslMode = SslMode.Require;
    }

    return csb.ConnectionString;
}

static string GetIPv4Host(string host)
{
    try
    {
        var addresses = System.Net.Dns.GetHostAddresses(host);
        var ipv4Address = addresses.FirstOrDefault(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        return ipv4Address?.ToString() ?? host;
    }
    catch
    {
        return host; // Fallback to original host if DNS resolution fails
    }
}
