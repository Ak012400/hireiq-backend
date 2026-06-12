using HireIQ.API.Data;
using HireIQ.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Controllers
builder.Services.AddControllers();

// ✅ JWT Authentication — YE MISSING THA!
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ✅ CORS — env-configurable allowed origins (no more AllowAnyOrigin)
// Set Cors__AllowedOrigins on Render, e.g. "https://hireiq.vercel.app;http://localhost:3000"
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(o => o.TrimEnd('/')) // ✅ CORS origins never have trailing slashes
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ Rate limiting (built-in .NET) — protects auth from brute force, AI endpoints from abuse
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth: 10 attempts/min per IP
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });

    // AI endpoints: 30 requests/min per IP (Groq/HF calls are expensive)
    options.AddFixedWindowLimiter("ai", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 30;
        o.QueueLimit = 0;
    });
});

// ✅ Render/Vercel sit behind a reverse proxy — trust X-Forwarded-* headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // ✅ Swagger mein JWT support
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<PdfExtractorService>();
builder.Services.AddScoped<MLService>();
builder.Services.AddScoped<GroqService>();
builder.Services.AddScoped<EmailService>(); // ✅ interview invites etc.
builder.Services.AddSingleton<MongoDbService>();

// HttpClient
builder.Services.AddHttpClient("FlaskService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["FlaskService:BaseUrl"]
        ?? "http://127.0.0.1:5000"
    );
});
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.PropertyNamingPolicy =
        JsonNamingPolicy.CamelCase);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();   // ✅ must run before HTTPS redirect (proxy-aware)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // ✅ re-enabled for production
}
app.UseCors("Frontend");
app.UseRateLimiter();        // ✅ rate limiting
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();