using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;

namespace PetFlowAPI.Domain.Entities;

public class RewardAction
{
    /// <summary>Nome da ação cadastrada no DDL que pontua o registro de eventos clínicos.</summary>
    public const string HealthEventActionName = "Registrar Evento de Saude";

    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PointsValue { get; set; }
    public string? Description { get; set; }
}

public class RewardPoint
{
    public const string HealthEventReference = "HEALTH_EVENT";

    public long Id { get; set; }
    public long TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public long RewardActionId { get; set; }
    public RewardAction? RewardAction { get; set; }
    public int Points { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PartnerDiscount
{
    public long Id { get; set; }
    public long ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
}

public class CouponTemplate
{
    public long Id { get; set; }
    public long PartnerDiscountId { get; set; }
    public PartnerDiscount? PartnerDiscount { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public int PointsRequired { get; set; }
}

public class Coupon
{
    public long Id { get; set; }
    public long TemplateId { get; set; }
    public CouponTemplate? Template { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponStatus Status { get; set; } = CouponStatus.DISPONIVEL;
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired(DateTime today) => ExpirationDate.HasValue && ExpirationDate.Value.Date < today.Date;

    /// <summary>Marca o cupom como resgatado, validando disponibilidade e validade.</summary>
    public void MarkAsRedeemed(DateTime today)
    {
        if (Status != CouponStatus.DISPONIVEL)
            throw new BusinessRuleException($"O cupom {Code} não está disponível para resgate (status atual: {Status}).");

        if (IsExpired(today))
            throw new BusinessRuleException($"O cupom {Code} está expirado.");

        Status = CouponStatus.RESGATADO;
    }
}

public class Redeem
{
    public long Id { get; set; }
    public long TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public long CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public int PointsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
