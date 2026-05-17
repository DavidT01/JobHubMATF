using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Profile.API.Data;
using Profile.API.Exceptions;
using Profile.API.Features.Behaviors;
using Scalar.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("ProfileDbConnection") ?? throw new InvalidOperationException("Connection string 'ProfileDbConnection' is not valid.");
builder.Services.AddDbContext<ProfileContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IProfileContext, ProfileContext>();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if(!Directory.Exists(app.Environment.WebRootPath))
{
    Directory.CreateDirectory(app.Environment.WebRootPath);
}

app.UseStaticFiles();

app.MapControllers();

app.Run();
