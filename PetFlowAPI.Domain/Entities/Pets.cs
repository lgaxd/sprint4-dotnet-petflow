namespace PetFlowAPI.Domain.Entities;

public class Species
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class Pet
{
    public long Id { get; set; }
    public long TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public long SpeciesId { get; set; }
    public Species? Species { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateTime? BirthDate { get; set; }
    public decimal? Weight { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HealthEvent> HealthEvents { get; set; } = new List<HealthEvent>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<RiskScore> RiskScores { get; set; } = new List<RiskScore>();
}

public class RiskLevel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
}

public class RiskScore
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public Pet? Pet { get; set; }
    public int Score { get; set; }
    public long RiskLevelId { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
