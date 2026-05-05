using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Interfaces;
using Siscomat.Repositories;
using Siscomat.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var frontendUrl = builder.Configuration.GetValue<string>("FrontendSettings:Url") ?? "http://localhost:3000";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(frontendUrl)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SiscomatAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.LoginPath = "/api/auth/login";
        options.AccessDeniedPath = "/api/auth/access-denied";
        options.LogoutPath = "/api/auth/logout";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

// ==========================================================
// INYECCIÓN DE DEPENDENCIAS (Repositorios y servicios)
// ==========================================================

// 1. Repositorios
builder.Services.AddScoped<IGestorRepository, GestorRepository>();
builder.Services.AddScoped<IParticipanteRepository, ParticipanteRepository>();
builder.Services.AddScoped<IConstanciaRepository, ConstanciaRepository>();
builder.Services.AddScoped<IPlantillaRepository, PlantillaRepository>();

// 2. Servicios
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PublicService>();
builder.Services.AddScoped<PlantillaService>();
builder.Services.AddScoped<GestorService>();

// ==========================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReact");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();