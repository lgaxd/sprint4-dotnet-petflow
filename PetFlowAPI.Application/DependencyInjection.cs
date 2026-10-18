using Microsoft.Extensions.DependencyInjection;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Services;

namespace PetFlowAPI.Application;

public static class DependencyInjection
{
    /// <summary>Registra os casos de uso (serviços de aplicação) e serviços de domínio.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRewardPointCalculator, RewardPointCalculator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITutorService, TutorService>();
        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IPetNoteService, PetNoteService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IHealthEventService, HealthEventService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IRedeemService, RedeemService>();
        return services;
    }
}
