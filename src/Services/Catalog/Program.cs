using Catalog.Data;
using Catalog.Repositories;
using System.Text.Json.Serialization;
using Catalog.Clients;
using Catalog.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
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
    options.AddPolicy("AllowFrontend", policy => policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProfileApi"]!);
});

builder.Services.AddScoped<IMatchingService,MatchingService>();
builder.Services.AddScoped<IBookmarkRepository,BookmarkRepository>();

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

app.Run();