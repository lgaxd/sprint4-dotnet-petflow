using System.ComponentModel.DataAnnotations;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Domain.Enums;

namespace PetFlowAPI.Application.DTOs;

// ───────────── Auth ─────────────
public sealed class LoginRequestDTO
{
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResponseDTO(string AccessToken, DateTime ExpiresAt, long TutorId, string Name, string Email, string Role);

// ───────────── Tutor ─────────────
public class TutorRequestDTO
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class TutorUpdateRequestDTO
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>Opcional: informe apenas para trocar a senha.</summary>
    [MinLength(8), MaxLength(100)]
    public string? Password { get; set; }
}

public class TutorResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed record PointsBalanceDTO(long TutorId, int TotalEarned, int TotalRedeemed, int Balance);

public class TutorFilter
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

// ───────────── Pet ─────────────
public class PetRequestDTO
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Breed { get; set; }

    public DateTime? BirthDate { get; set; }

    [Range(0.01, 999.99)]
    public decimal? Weight { get; set; }

    [Range(1, long.MaxValue)]
    public long SpeciesId { get; set; }

    /// <summary>Tutor dono do pet. Tutores comuns só podem informar o próprio id.</summary>
    [Range(1, long.MaxValue)]
    public long TutorId { get; set; }
}

public class PetResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateTime? BirthDate { get; set; }
    public decimal? Weight { get; set; }
    public long SpeciesId { get; set; }
    public long TutorId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PetFilter
{
    public string? Name { get; set; }
    public long? TutorId { get; set; }
    public long? SpeciesId { get; set; }
}

// ───────────── Pet notes (MongoDB) ─────────────
public class PetNoteRequestDTO
{
    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();
}

public class PetNoteResponseDTO : ResourceBase
{
    public string Id { get; set; } = string.Empty;
    public long PetId { get; set; }
    public long AuthorTutorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ───────────── Clinic ─────────────
public class ClinicRequestDTO
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(18)]
    public string? Cnpj { get; set; }
}

public class ClinicResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Cnpj { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClinicFilter
{
    public string? Name { get; set; }
}

// ───────────── Plan ─────────────
public class PlanRequestDTO
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    [Range(0.01, 99999999.99)]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    public int DurationDays { get; set; }

    [Range(1, 100)]
    public int PointsPerEvent { get; set; } = 1;

    [Range(1, long.MaxValue)]
    public long ClinicId { get; set; }
}

public class PlanResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int PointsPerEvent { get; set; }
    public long ClinicId { get; set; }
}

public class PlanFilter
{
    public string? Name { get; set; }
    public long? ClinicId { get; set; }
}

// ───────────── Subscription ─────────────
public class SubscriptionRequestDTO
{
    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.ATIVO;

    [Range(1, long.MaxValue)]
    public long PetId { get; set; }

    [Range(1, long.MaxValue)]
    public long PlanId { get; set; }
}

public class SubscriptionResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public long PetId { get; set; }
    public long PlanId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SubscriptionFilter
{
    public long? PetId { get; set; }
    public long? PlanId { get; set; }
    public SubscriptionStatus? Status { get; set; }
}

// ───────────── Health event ─────────────
public class HealthEventRequestDTO
{
    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    public HealthEventStatus Status { get; set; } = HealthEventStatus.AGENDADO;

    [Range(1, long.MaxValue)]
    public long EventTypeId { get; set; }

    [Range(1, long.MaxValue)]
    public long PetId { get; set; }

    public long? ClinicId { get; set; }
}

public class HealthEventResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public HealthEventStatus Status { get; set; }
    public long EventTypeId { get; set; }
    public long PetId { get; set; }
    public long? ClinicId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Pontos concedidos ao tutor quando o evento é concluído (preenchido apenas na operação que concluiu o evento).</summary>
    public int? PointsAwarded { get; set; }
}

public class HealthEventFilter
{
    public long? PetId { get; set; }
    public HealthEventStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// ───────────── Coupon ─────────────
public class CouponRequestDTO
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public CouponStatus Status { get; set; } = CouponStatus.DISPONIVEL;

    public DateTime? ExpirationDate { get; set; }

    [Range(1, long.MaxValue)]
    public long TemplateId { get; set; }
}

public class CouponResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponStatus Status { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public long TemplateId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CouponFilter
{
    public string? Code { get; set; }
    public CouponStatus? Status { get; set; }
}

// ───────────── Redeem ─────────────
public class RedeemRequestDTO
{
    [Range(1, long.MaxValue)]
    public long TutorId { get; set; }

    [Range(1, long.MaxValue)]
    public long CouponId { get; set; }
}

public class RedeemResponseDTO : ResourceBase
{
    public long Id { get; set; }
    public int PointsUsed { get; set; }
    public long TutorId { get; set; }
    public long CouponId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RedeemFilter
{
    public long? TutorId { get; set; }
    public long? CouponId { get; set; }
}
