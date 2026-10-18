using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;

namespace PetFlowAPI.Domain.Entities;

public class EventType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PointsReward { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class HealthEvent
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public Pet? Pet { get; set; }
    public long EventTypeId { get; set; }
    public EventType? EventType { get; set; }
    public long? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public HealthEventStatus Status { get; set; } = HealthEventStatus.AGENDADO;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Indica se a transição de <paramref name="previous"/> para o status atual conclui o evento.</summary>
    public bool WasJustCompleted(HealthEventStatus previous)
        => Status == HealthEventStatus.REALIZADO && previous != HealthEventStatus.REALIZADO;

    public void ChangeStatus(HealthEventStatus newStatus)
    {
        if (Status == HealthEventStatus.CANCELADO && newStatus == HealthEventStatus.REALIZADO)
            throw new BusinessRuleException("Um evento cancelado não pode ser marcado como realizado.");

        Status = newStatus;
    }
}
