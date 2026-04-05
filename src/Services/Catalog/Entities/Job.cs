using MongoDB.Bson;
using  MongoDB.Bson.Serialization.Attributes;

namespace Catalog.Entities;

public class Job
{
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;

    public decimal Salary { get; set; }
    public string Location { get; set; } = string.Empty;

    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
}