using Catalog.Entities;
using MongoDB.Driver;

namespace Catalog.Data;

public interface ICatalogContext
{
    IMongoCollection<Job> Jobs { get; }   
}