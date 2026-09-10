using Microsoft.EntityFrameworkCore;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Recruitment.API.Data;
using Scalar.AspNetCore;
using MediatR;
using FluentValidation;
using JobHub.Grpc.Contracts.Profile;
using Recruitment.API.Features.Behaviors;
using System.Reflection;
using Recruitment.API.Infrastructure;
using Recruitment.API.Services.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddHttpContextAccessor();

var jwtSection = builder.Configuration.GetRequiredSection("JwtSettings");
var jwtSecret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EmployerOrAdmin", policy => policy.RequireAuthenticatedUser().RequireRole("Employer", "Admin"))
    .AddPolicy("CandidateOrAdmin", policy => policy.RequireAuthenticatedUser().RequireRole("Candidate", "Admin"));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<RecruitmentContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RecruitmentDatabase")));

builder.Services.AddScoped<IMeetingService, GoogleMeetingService>();
builder.Services.AddGrpcClient<CandidateProfileGrpcService.CandidateProfileGrpcServiceClient>(options =>
{
    var profileAddress = builder.Configuration["GrpcServices:ProfileApi"]
        ?? throw new InvalidOperationException("gRPC Profile API address is not configured.");
    options.Address = new Uri(profileAddress);
});
builder.Services.AddScoped<IProfileServiceClient, ProfileServiceClient>();
builder.Services.AddScoped<IRecruitmentAuthorization, RecruitmentAuthorization>();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<RecruitmentGrpcService>();

app.Run();
