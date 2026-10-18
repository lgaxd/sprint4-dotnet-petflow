using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;

namespace PetFlowAPI.Domain.Entities;

public class Clinic
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Cnpj { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
}

public class Plan
{
    public long Id { get; set; }
    public long ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }

    /// <summary>Multiplicador de pontos por evento (plano premium = 2x).</summary>
    public int PointsPerEvent { get; set; } = 1;
}

public class Subscription
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public Pet? Pet { get; set; }
    public long PlanId { get; set; }
    public Plan? Plan { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.ATIVO;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Garante que o período da assinatura é coerente.</summary>
    public void EnsureValidPeriod()
    {
        if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
            throw new BusinessRuleException("A data de término da assinatura não pode ser anterior à data de início.");
    }

    public void ChangeStatus(SubscriptionStatus newStatus)
    {
        if (Status == SubscriptionStatus.CANCELADO && newStatus == SubscriptionStatus.ATIVO)
            throw new BusinessRuleException("Uma assinatura cancelada não pode ser reativada.");

        Status = newStatus;
    }
}
