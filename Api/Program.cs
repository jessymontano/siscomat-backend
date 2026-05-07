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
var portalUrl = builder.Configuration.GetValue<string>("FrontendSettings:PortalPublicoUrl") ?? "http://localhost:5173";
var panelUrl = builder.Configuration.GetValue<string>("FrontendSettings:PanelAdminUrl") ?? "http://localhost:5174";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Siscomat.Repositories"))
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(portalUrl, panelUrl)
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

// 3. HttpClient para microservicio Python
builder.Services.AddHttpClient("microservicio", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MicroservicioSettings:Url"]!);
});

// ==========================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReact");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// migraciones
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        if (!context.Gestores.Any(g => g.Correo == "admin@admin.com"))
        {
            var admin = new Siscomat.Core.Entities.Gestor
            {
                Nombre = "Karl",
                Apellido1 = "Marx",
                Apellido2 = "Pressman",
                Correo = "admin@admin.com",
                EsAdmin = true,
                PasswordHash = "$2a$11$TyFYT4/OunoilD7K176mI.dqEjv.p8Gp950zLORjXbZrV5JvtVEV2"
            };
            context.Gestores.Add(admin);
            context.SaveChanges();

            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Usuario admin creado");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error durante la migración de la base de datos.");
    }
}

app.Run();