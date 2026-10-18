using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Infrastructure.Mongo;
using PetFlowAPI.Infrastructure.Persistence;
using PetFlowAPI.Infrastructure.Persistence.Repositories;
using PetFlowAPI.Infrastructure.Security;

namespace PetFlowAPI.Infrastructure;

public static class DependencyInjection
{
    public const string OracleConnectionName = "OracleConnection";

    /// <summary>Registra persistência relacional (EF Core + Oracle), NoSQL (MongoDB), repositórios e segurança.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
    {
        var connectionString = configuration.GetConnectionString(OracleConnectionName);
        services.AddDbContext<PetFlowContext>(options =>
        {
            options.UseOracle(connectionString);
            options.EnableDetailedErrors(isDevelopment);
            options.EnableSensitiveDataLogging(isDevelopment);
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITutorRepository, TutorRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IRewardRepository, RewardRepository>();

        services.Configure<MongoSettings>(configuration.GetSection(MongoSettings.SectionName));
        services.AddSingleton<MongoContext>();
        services.AddSingleton<IPetNoteRepository, MongoPetNoteRepository>();
        services.AddHostedService<MongoIndexInitializer>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        return services;
    }
}

/// <summary>Fábrica usada apenas pelas ferramentas do EF Core (dotnet ef migrations ...).</summary>
internal sealed class PetFlowContextFactory : IDesignTimeDbContextFactory<PetFlowContext>
{
    public PetFlowContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PetFlowContext>()
            .UseOracle("Data Source=localhost:1521/XEPDB1;User Id=design;Password=design;")
            .Options;
        return new PetFlowContext(options);
    }
}
