using Catalog.Data;
using Catalog.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ICatalogContext, CatalogContext>();
builder.Services.AddScoped<IJobRepository,JobRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
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

app.Run();