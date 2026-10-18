using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Domain.Entities;

namespace PetFlowAPI.Application.Mappings;

/// <summary>Mapeamentos explícitos entre entidades de domínio e DTOs (sem reflexão, fáceis de testar).</summary>
public static class Mappers
{
    public static TutorResponseDTO ToResponse(this Tutor e) => new()
    {
        Id = e.Id, Name = e.Name, Email = e.Email, Phone = e.Phone, Role = e.Role, CreatedAt = e.CreatedAt
    };

    public static PetResponseDTO ToResponse(this Pet e) => new()
    {
        Id = e.Id, Name = e.Name, Breed = e.Breed, BirthDate = e.BirthDate, Weight = e.Weight,
        SpeciesId = e.SpeciesId, TutorId = e.TutorId, CreatedAt = e.CreatedAt
    };

    public static void Apply(this Pet e, PetRequestDTO r)
    {
        e.Name = r.Name; e.Breed = r.Breed; e.BirthDate = r.BirthDate; e.Weight = r.Weight;
        e.SpeciesId = r.SpeciesId; e.TutorId = r.TutorId;
    }

    public static PetNoteResponseDTO ToResponse(this PetNote e) => new()
    {
        Id = e.Id, PetId = e.PetId, AuthorTutorId = e.AuthorTutorId, Title = e.Title,
        Content = e.Content, Tags = e.Tags.ToList(), CreatedAt = e.CreatedAt
    };

    public static ClinicResponseDTO ToResponse(this Clinic e) => new()
    {
        Id = e.Id, Name = e.Name, Address = e.Address, Phone = e.Phone, Cnpj = e.Cnpj, CreatedAt = e.CreatedAt
    };

    public static void Apply(this Clinic e, ClinicRequestDTO r)
    {
        e.Name = r.Name; e.Address = r.Address; e.Phone = r.Phone;
        e.Cnpj = string.IsNullOrWhiteSpace(r.Cnpj) ? null : r.Cnpj;
    }

    public static PlanResponseDTO ToResponse(this Plan e) => new()
    {
        Id = e.Id, Name = e.Name, Description = e.Description, Price = e.Price,
        DurationDays = e.DurationDays, PointsPerEvent = e.PointsPerEvent, ClinicId = e.ClinicId
    };

    public static void Apply(this Plan e, PlanRequestDTO r)
    {
        e.Name = r.Name; e.Description = r.Description; e.Price = r.Price;
        e.DurationDays = r.DurationDays; e.PointsPerEvent = r.PointsPerEvent; e.ClinicId = r.ClinicId;
    }

    public static SubscriptionResponseDTO ToResponse(this Subscription e) => new()
    {
        Id = e.Id, StartDate = e.StartDate, EndDate = e.EndDate, Status = e.Status,
        PetId = e.PetId, PlanId = e.PlanId, CreatedAt = e.CreatedAt
    };

    public static HealthEventResponseDTO ToResponse(this HealthEvent e) => new()
    {
        Id = e.Id, Description = e.Description, EventDate = e.EventDate, Status = e.Status,
        EventTypeId = e.EventTypeId, PetId = e.PetId, ClinicId = e.ClinicId, CreatedAt = e.CreatedAt
    };

    public static CouponResponseDTO ToResponse(this Coupon e) => new()
    {
        Id = e.Id, Code = e.Code, Status = e.Status, ExpirationDate = e.ExpirationDate,
        TemplateId = e.TemplateId, CreatedAt = e.CreatedAt
    };

    public static RedeemResponseDTO ToResponse(this Redeem e) => new()
    {
        Id = e.Id, PointsUsed = e.PointsUsed, TutorId = e.TutorId, CouponId = e.CouponId, CreatedAt = e.CreatedAt
    };
}
