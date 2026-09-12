using Catalog.Data;
using Catalog.Repositories;
using System.Text.Json.Serialization;
using Catalog.Clients;
using Catalog.Services;
using Catalog.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICatalogContext, CatalogContext>();
builder.Services.AddScoped<IJobRepository,JobRepository>();
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

builder.Services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProfileApi"]!);
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
