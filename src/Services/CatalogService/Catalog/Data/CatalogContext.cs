using Catalog.Entities;
using MongoDB.Driver;

namespace Catalog.Data;

public class CatalogContext : ICatalogContext
{
    public IMongoCollection<Job> Jobs { get; }
    public IMongoCollection<Bookmark> Bookmarks { get; }
    public CatalogContext(IConfiguration configuration)
    {
        var connStr = configuration.GetValue<string>("DatabaseSettings:ConnectionString");
        var client = new MongoClient(connStr);
        var database = client.GetDatabase("JobHubDB");
        
        Jobs = database.GetCollection<Job>("Jobs");
        Bookmarks = database.GetCollection<Bookmark>("Bookmarks");
        CatalogContextSeed.SeedData(Jobs);
    }
}