using Catalog.Data;
using Catalog.Repositories;
using System.Text.Json.Serialization;
using Catalog.Clients;
using Catalog.Services;
using Catalog.Services.Grpc;
using JobHub.Grpc.Contracts.Profile;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICatalogContext, CatalogContext>();
builder.Services.AddScoped<IJobRepository,JobRepository>();
builder.Services.AddScoped<IProfileApiClient, ProfileApiClient>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => 
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigin"]!)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddGrpcClient<CandidateProfileGrpcService.CandidateProfileGrpcServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["GrpcServices:ProfileApi"]!);
});

builder.Services.AddScoped<IMatchingService,MatchingService>();
builder.Services.AddScoped<IBookmarkRepository,BookmarkRepository>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisSettings:ConnectionString"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapControllers();
app.MapGrpcService<CatalogJobGrpcService>();

app.Run();
