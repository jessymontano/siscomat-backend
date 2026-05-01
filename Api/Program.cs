using Microsoft.EntityFrameworkCore;
using Siscomat.Repositories;
using Siscomat.Core.Interfaces;
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

builder.Services.AddScoped<IParticipanteRepository, ParticipanteRepository>();
builder.Services.AddScoped<IConstanciaRepository, ConstanciaRepository>();
builder.Services.AddScoped<PublicService>();

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
