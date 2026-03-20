using Microsoft.EntityFrameworkCore;
using HireIQ.API.Data;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using HireIQ.API.Services;
{
    
}
    
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Controllers
builder.Services.AddControllers();

// CORS — React frontend ke liye
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MLService>();
builder.Services.AddScoped<GroqService>();

// HttpClient — Flask service ke liye
builder.Services.AddHttpClient("FlaskService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["FlaskService:BaseUrl"] 
        ?? "http://127.0.0.1:5000"
    );
});

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();