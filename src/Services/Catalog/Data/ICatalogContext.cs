using Catalog.Entities;
using MongoDB.Driver;

namespace Catalog.Data;

public class ICatalogContext
{
    IMongoCollection<Job> Jobs { get; }   
}