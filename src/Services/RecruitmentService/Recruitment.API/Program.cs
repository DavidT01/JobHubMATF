using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Scalar.AspNetCore;
using MediatR;
using FluentValidation;
using Recruitment.API.Features.Behaviors;
using System.Reflection;
using Recruitment.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<RecruitmentContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RecruitmentDatabase")));

builder.Services.AddScoped<IMeetingService, GoogleMeetingService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));
builder.Services.AddExceptionHandler<Recruitment.API.Exceptions.RecruitmentExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseAuthorization();

app.MapControllers();

app.Run();
