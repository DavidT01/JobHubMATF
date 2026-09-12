using MongoDB.Bson;
using  MongoDB.Bson.Serialization.Attributes;

namespace Catalog.Entities;

public enum JobType
{
    FullTime,
    PartTime,
    Contract,
    Internship
}
    
public enum ExperienceLevel
{
    Junior,
    Mid,
    Senior,
    Lead
}
    
    
public enum WorkMode
{
    OnSite,
    Hybrid,
    Remote
}

public class Job
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    [BsonRepresentation(BsonType.String)]
    public JobType JobType { get; set; } = JobType.FullTime;

    [BsonRepresentation(BsonType.String)] 
    public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.Junior;

    [BsonRepresentation(BsonType.String)]
    public WorkMode WorkMode { get; set; } = WorkMode.OnSite;
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    
    public string? ApplyUrl { get; set; }
    
    public string? ContactEmail { get; set; }
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    
    public decimal? SalaryMin { get; set; }
    
    public decimal? SalaryMax { get; set; }
    
    public string Currency { get; set; } = "EUR";
    
    public List<string> Skills { get; set; } = new();
    
    public List<string> Requirements { get; set; } = new();
    
    public List<string> Responsibilities { get; set; } = new();
    
    public int? YearsOfExperience { get; set; }
    
    public string? EducationLevel { get; set; }
    
    public string? City { get; set; } 
    
    public string? Country { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? ExpirationDate { get; set; }
    
    
}