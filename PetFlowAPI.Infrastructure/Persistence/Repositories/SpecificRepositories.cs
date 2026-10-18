using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Remoção em cascata feita na aplicação, pois o DDL Oracle não define ON DELETE CASCADE.
/// Sempre marca os filhos antes do pai; a confirmação ocorre em um único SaveChanges (transação).
/// </summary>
internal static class CascadeDelete
{
    public static async Task PetDependentsAsync(PetFlowContext context, IReadOnlyCollection<long> petIds, CancellationToken ct)
    {
        if (petIds.Count == 0) return;
        context.HealthEvents.RemoveRange(await context.HealthEvents.Where(h => petIds.Contains(h.PetId)).ToListAsync(ct));
        context.Subscriptions.RemoveRange(await context.Subscriptions.Where(s => petIds.Contains(s.PetId)).ToListAsync(ct));
        context.RiskScores.RemoveRange(await context.RiskScores.Where(r => petIds.Contains(r.PetId)).ToListAsync(ct));
    }

    public static async Task CouponDependentsAsync(PetFlowContext context, IReadOnlyCollection<long> couponIds, CancellationToken ct)
    {
        if (couponIds.Count == 0) return;
        context.Redeems.RemoveRange(await context.Redeems.Where(r => couponIds.Contains(r.CouponId)).ToListAsync(ct));
    }
}

public sealed class TutorRepository : Repository<Tutor>, ITutorRepository
{
    public TutorRepository(PetFlowContext context) : base(context) { }

    public Task<Tutor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return Set.FirstOrDefaultAsync(t => t.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task RemoveWithDependentsAsync(Tutor tutor, CancellationToken cancellationToken = default)
    {
        Context.Redeems.RemoveRange(await Context.Redeems.Where(r => r.TutorId == tutor.Id).ToListAsync(cancellationToken));
        Context.RewardPoints.RemoveRange(await Context.RewardPoints.Where(r => r.TutorId == tutor.Id).ToListAsync(cancellationToken));
        Context.Addresses.RemoveRange(await Context.Addresses.Where(a => a.TutorId == tutor.Id).ToListAsync(cancellationToken));

        var pets = await Context.Pets.Where(p => p.TutorId == tutor.Id).ToListAsync(cancellationToken);
        await CascadeDelete.PetDependentsAsync(Context, pets.Select(p => p.Id).ToList(), cancellationToken);
        Context.Pets.RemoveRange(pets);

        Context.Tutors.Remove(tutor);
    }
}

public sealed class PetRepository : Repository<Pet>, IPetRepository
{
    public PetRepository(PetFlowContext context) : base(context) { }

    public async Task RemoveWithDependentsAsync(Pet pet, CancellationToken cancellationToken = default)
    {
        await CascadeDelete.PetDependentsAsync(Context, new[] { pet.Id }, cancellationToken);
        Context.Pets.Remove(pet);
    }
}

public sealed class ClinicRepository : Repository<Clinic>, IClinicRepository
{
    public ClinicRepository(PetFlowContext context) : base(context) { }

    public async Task RemoveWithDependentsAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        var partnerIds = await Context.PartnerDiscounts.Where(p => p.ClinicId == clinic.Id).Select(p => p.Id).ToListAsync(cancellationToken);
        var templateIds = await Context.CouponTemplates.Where(t => partnerIds.Contains(t.PartnerDiscountId)).Select(t => t.Id).ToListAsync(cancellationToken);
        var coupons = await Context.Coupons.Where(c => templateIds.Contains(c.TemplateId)).ToListAsync(cancellationToken);

        await CascadeDelete.CouponDependentsAsync(Context, coupons.Select(c => c.Id).ToList(), cancellationToken);
        Context.Coupons.RemoveRange(coupons);
        Context.CouponTemplates.RemoveRange(await Context.CouponTemplates.Where(t => partnerIds.Contains(t.PartnerDiscountId)).ToListAsync(cancellationToken));
        Context.PartnerDiscounts.RemoveRange(await Context.PartnerDiscounts.Where(p => p.ClinicId == clinic.Id).ToListAsync(cancellationToken));

        var plans = await Context.Plans.Where(p => p.ClinicId == clinic.Id).ToListAsync(cancellationToken);
        var planIds = plans.Select(p => p.Id).ToList();
        Context.Subscriptions.RemoveRange(await Context.Subscriptions.Where(s => planIds.Contains(s.PlanId)).ToListAsync(cancellationToken));
        Context.Plans.RemoveRange(plans);

        Context.HealthEvents.RemoveRange(await Context.HealthEvents.Where(h => h.ClinicId == clinic.Id).ToListAsync(cancellationToken));
        Context.Clinics.Remove(clinic);
    }
}

public sealed class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(PetFlowContext context) : base(context) { }

    public async Task RemoveWithDependentsAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await CascadeDelete.CouponDependentsAsync(Context, new[] { coupon.Id }, cancellationToken);
        Context.Coupons.Remove(coupon);
    }
}

public sealed class RewardRepository : IRewardRepository
{
    private readonly PetFlowContext _context;

    public RewardRepository(PetFlowContext context) => _context = context;

    public async Task<PointsSummary> GetPointsSummaryAsync(long tutorId, CancellationToken cancellationToken = default)
    {
        var earned = await _context.RewardPoints.Where(p => p.TutorId == tutorId).SumAsync(p => (int?)p.Points, cancellationToken) ?? 0;
        var redeemed = await _context.Redeems.Where(r => r.TutorId == tutorId).SumAsync(r => (int?)r.PointsUsed, cancellationToken) ?? 0;
        return new PointsSummary(earned, redeemed);
    }
}
