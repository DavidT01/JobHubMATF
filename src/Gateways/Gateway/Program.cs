using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Učitavanje ocelot.json konfiguracionog fajla
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 2. Podešavanje CORS-a za Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // URL tvog Angular WebSPA-a
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Obavezno zbog SignalR WebSockets konekcije!
    });
});

// 3. Registracija Ocelot servisa u .NET kontejner
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// 4. Aktivacija CORS-a pre Ocelot middleware-a
app.UseCors("AllowAngular");

// 5. Pokretanje Ocelot Gateway-a
await app.UseOcelot();

app.Run();