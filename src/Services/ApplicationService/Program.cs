using ApplicationService.Application;
using ApplicationService.Infrastructure;
using ApplicationService.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddApplicationLayer(builder.Configuration);
builder.Services.AddApplicationInfrastructure();
builder.Services.AddApplicationPersistence(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.Run();
