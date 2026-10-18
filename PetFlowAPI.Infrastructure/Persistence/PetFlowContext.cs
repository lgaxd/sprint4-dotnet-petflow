using System.Text;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Domain.Entities;

namespace PetFlowAPI.Infrastructure.Persistence;

public class PetFlowContext : DbContext
{
    public PetFlowContext(DbContextOptions<PetFlowContext> options) : base(options) { }

    public DbSet<Tutor> Tutors => Set<Tutor>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Species> Species => Set<Species>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<HealthEvent> HealthEvents => Set<HealthEvent>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<RewardAction> RewardActions => Set<RewardAction>();
    public DbSet<RewardPoint> RewardPoints => Set<RewardPoint>();
    public DbSet<PartnerDiscount> PartnerDiscounts => Set<PartnerDiscount>();
    public DbSet<CouponTemplate> CouponTemplates => Set<CouponTemplate>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Redeem> Redeems => Set<Redeem>();
    public DbSet<RiskLevel> RiskLevels => Set<RiskLevel>();
    public DbSet<RiskScore> RiskScores => Set<RiskScore>();
    public DbSet<LogErro> LogErros => Set<LogErro>();
    public DbSet<LogAuditoriaHealthEvent> LogAuditoriaHealthEvents => Set<LogAuditoriaHealthEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PetFlowContext).Assembly);

        // Convenção do DDL Oracle: colunas em UPPER_SNAKE_CASE (PasswordHash -> PASSWORD_HASH).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToUpperSnakeCase(property.Name));
    }

    internal static string ToUpperSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) builder.Append('_');
            builder.Append(char.ToUpperInvariant(name[i]));
        }
        return builder.ToString();
    }
}
