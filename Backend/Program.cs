using Backend.Data;
using Resend;
using Backend.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Brak ConnectionStrings:DefaultConnection. Ustaw go przez dotnet user-secrets " +
        "albo zmienną środowiskową ConnectionStrings__DefaultConnection (instrukcja w komendy.txt).");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// JWT
// Bez poprawnego klucza aplikacja nie wystartuje - lepiej to niż po cichu wyłączone logowanie.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32 || jwtKey.StartsWith("TUTAJ_"))
{
    throw new InvalidOperationException(
        "Brak klucza Jwt:Key (minimum 32 znaki). Ustaw go przez dotnet user-secrets " +
        "albo zmienną środowiskową Jwt__Key (instrukcja w komendy.txt).");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// CORS
var frontendUrl = (builder.Configuration["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
    {
        policy.WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Prawdziwe IP klienta za nginx 
// Bez tego każde żądanie wygląda, jakby przyszło z 127.0.0.1 (nginx), więc limit logowań
// byłby wspólny dla wszystkich użytkowników. nginx podaje IP klienta w X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Loopback);
});

// Limit żądań na endpointy logowania/rejestracji/resetu hasła (ochrona przed zgadywaniem haseł i spamem maili)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Wirtualne pulpity 
builder.Services.AddSingleton<DesktopSessionStore>();
builder.Services.AddHostedService<ContainerHeartBeatCleanUpService>();

// Cloudflare Turnstile (CAPTCHA przy logowaniu i rejestracji) ---
// Lokalnie można pracować bez klucza (weryfikacja jest wtedy pomijana z ostrzeżeniem w logach),
// ale poza trybem Development brak klucza oznaczałby, że CAPTCHA nic nie chroni - nie startujemy.
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Turnstile:SecretKey"]))
{
    throw new InvalidOperationException(
        "Brak klucza Turnstile:SecretKey. Ustaw zmienną środowiskową Turnstile__SecretKey (instrukcja w komendy.txt).");
}

builder.Services.AddHttpClient<ITurnstileService, TurnstileService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Resend
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiKey"] ?? string.Empty;
});

builder.Services.AddScoped<IEmailService, ResendEmailService>();

var app = builder.Build();

// Musi być PIERWSZE - zanim cokolwiek odczyta IP klienta.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("AllowVue");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();