using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Infrastructure.Mongo;
using PetFlowAPI.Infrastructure.Persistence;
using PetFlowAPI.Infrastructure.Persistence.Repositories;
using PetFlowAPI.Infrastructure.Security;

namespace PetFlowAPI.Tests.Infrastructure;

internal static class TestDb
{
    public static PetFlowContext Create()
    {
        var options = new DbContextOptionsBuilder<PetFlowContext>()
            .UseInMemoryDatabase($"infra-{Guid.NewGuid():N}")
            .Options;
        return new PetFlowContext(options);
    }
}

public class RepositoryTests
{
    private static async Task<PetFlowContext> SeedPetsAsync()
    {
        var context = TestDb.Create();
        context.Tutors.Add(new Tutor { Id = 1, Name = "Ana", Email = "ana@x.com", PasswordHash = "h" });
        context.Species.Add(new Species { Id = 1, Name = "Cachorro" });
        foreach (var (name, weight) in new[] { ("Thor", 32m), ("Rex", 28.5m), ("Bolt", 14m), ("Nala", 5.6m), ("Luna", 3.8m) })
            context.Pets.Add(new Pet { TutorId = 1, SpeciesId = 1, Name = name, Weight = weight });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task GetPaged_DeveOrdenarPorPropriedadeAscendente()
    {
        // Arrange
        await using var context = await SeedPetsAsync();
        var sut = new Repository<Pet>(context);

        // Act
        var page = await sut.GetPagedAsync(null, new PageRequest(0, 10, "Name", false));

        // Assert
        Assert.Equal(new[] { "Bolt", "Luna", "Nala", "Rex", "Thor" }, page.Items.Select(p => p.Name));
        Assert.Equal(5, page.TotalItems);
    }

    [Fact]
    public async Task GetPaged_DeveOrdenarDecrescentePorPeso()
    {
        // Arrange
        await using var context = await SeedPetsAsync();
        var sut = new Repository<Pet>(context);

        // Act
        var page = await sut.GetPagedAsync(null, new PageRequest(0, 10, "weight", true));

        // Assert
        Assert.Equal("Thor", page.Items[0].Name);
        Assert.Equal("Luna", page.Items[^1].Name);
    }

    [Fact]
    public async Task GetPaged_DevePaginarEInformarTotais()
    {
        // Arrange
        await using var context = await SeedPetsAsync();
        var sut = new Repository<Pet>(context);

        // Act
        var page = await sut.GetPagedAsync(null, new PageRequest(1, 2, "Name", false));

        // Assert
        Assert.Equal(new[] { "Nala", "Rex" }, page.Items.Select(p => p.Name));
        Assert.Equal(5, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(1, page.Page);
    }

    [Fact]
    public async Task GetPaged_ComPredicado_DeveFiltrarETotalizarSomenteOsFiltrados()
    {
        // Arrange
        await using var context = await SeedPetsAsync();
        var sut = new Repository<Pet>(context);

        // Act
        var page = await sut.GetPagedAsync(p => p.Weight > 10, new PageRequest(0, 10, "Name", false));

        // Assert
        Assert.Equal(3, page.TotalItems);
        Assert.DoesNotContain(page.Items, p => p.Name == "Luna");
    }

    [Theory]
    [InlineData("PasswordHash")]
    [InlineData("Tutor")]
    [InlineData("inexistente")]
    [InlineData(null)]
    public async Task GetPaged_ComCampoInvalidoOuNaoEscalar_DeveCairParaOrdenacaoPorId(string? sortBy)
    {
        // Arrange
        await using var context = await SeedPetsAsync();
        var sut = new Repository<Pet>(context);

        // Act
        var page = await sut.GetPagedAsync(null, new PageRequest(0, 10, sortBy, false));

        // Assert
        Assert.Equal(page.Items.Select(p => p.Id).OrderBy(i => i), page.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task ExistsAsync_DeveDistinguirRegistrosExistentesEInexistentes()
    {
        // Arrange
        await using var context = await SeedPetsAsync();
        var sut = new Repository<Pet>(context);

        // Act
        var exists = await sut.ExistsAsync(p => p.Name == "Rex");
        var missing = await sut.ExistsAsync(p => p.Name == "Fantasma");

        // Assert
        Assert.True(exists);
        Assert.False(missing);
    }

    [Fact]
    public async Task AddEUnitOfWork_DevePersistirEPermitirBuscaPorId()
    {
        // Arrange
        await using var context = TestDb.Create();
        var repository = new Repository<Species>(context);
        var uow = new UnitOfWork(context);

        // Act
        await repository.AddAsync(new Species { Name = "Gato" });
        await uow.SaveChangesAsync();
        var found = await repository.GetByIdAsync(1);

        // Assert
        Assert.Equal("Gato", found!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente_DeveRetornarNulo()
    {
        // Arrange
        await using var context = TestDb.Create();

        // Act
        var found = await new Repository<Species>(context).GetByIdAsync(404);

        // Assert
        Assert.Null(found);
    }
}

public class CascadeRepositoryTests
{
    [Fact]
    public async Task TutorRepository_GetByEmail_DeveIgnorarCapitalizacaoEEspacos()
    {
        // Arrange
        await using var context = TestDb.Create();
        context.Tutors.Add(new Tutor { Name = "Ana", Email = "Ana@Petflow.com", PasswordHash = "h" });
        await context.SaveChangesAsync();

        // Act
        var found = await new TutorRepository(context).GetByEmailAsync("  ana@petflow.COM ");

        // Assert
        Assert.NotNull(found);
    }

    [Fact]
    public async Task TutorRepository_RemoveWithDependents_DeveRemoverTodosOsRegistrosFilhos()
    {
        // Arrange
        await using var context = TestDb.Create();
        var tutor = new Tutor { Name = "Ana", Email = "ana@x.com", PasswordHash = "h" };
        var other = new Tutor { Name = "Bruno", Email = "bruno@x.com", PasswordHash = "h" };
        context.AddRange(tutor, other, new Species { Id = 1, Name = "Cachorro" }, new EventType { Id = 1, Name = "Vacina", Category = "PREVENTIVO" },
            new RewardAction { Id = 1, Name = "A" });
        await context.SaveChangesAsync();

        var pet = new Pet { TutorId = tutor.Id, SpeciesId = 1, Name = "Rex" };
        var otherPet = new Pet { TutorId = other.Id, SpeciesId = 1, Name = "Thor" };
        context.AddRange(pet, otherPet);
        await context.SaveChangesAsync();

        context.AddRange(
            new Address { TutorId = tutor.Id, Street = "Rua", City = "SP", State = "SP", ZipCode = "01310100" },
            new HealthEvent { PetId = pet.Id, EventTypeId = 1, EventDate = DateTime.Today, Status = HealthEventStatus.AGENDADO },
            new HealthEvent { PetId = otherPet.Id, EventTypeId = 1, EventDate = DateTime.Today, Status = HealthEventStatus.AGENDADO },
            new RewardPoint { TutorId = tutor.Id, RewardActionId = 1, Points = 10 });
        await context.SaveChangesAsync();

        var repository = new TutorRepository(context);

        // Act
        await repository.RemoveWithDependentsAsync(tutor);
        await new UnitOfWork(context).SaveChangesAsync();

        // Assert
        Assert.Equal(1, await context.Tutors.CountAsync());
        Assert.Equal(new[] { "Thor" }, await context.Pets.Select(p => p.Name).ToListAsync());
        Assert.Equal(1, await context.HealthEvents.CountAsync());
        Assert.Empty(context.Addresses);
        Assert.Empty(context.RewardPoints);
    }

    [Fact]
    public async Task ClinicRepository_RemoveWithDependents_DeveRemoverPlanosAssinaturasECupons()
    {
        // Arrange
        await using var context = TestDb.Create();
        var clinic = new Clinic { Name = "VetVida" };
        var keep = new Clinic { Name = "Outra" };
        context.AddRange(clinic, keep, new Tutor { Id = 1, Name = "Ana", Email = "a@x.com", PasswordHash = "h" }, new Species { Id = 1, Name = "Cachorro" });
        await context.SaveChangesAsync();

        var plan = new Plan { ClinicId = clinic.Id, Name = "Premium", Price = 10, DurationDays = 30 };
        var keptPlan = new Plan { ClinicId = keep.Id, Name = "Basico", Price = 5, DurationDays = 30 };
        var pet = new Pet { TutorId = 1, SpeciesId = 1, Name = "Rex" };
        var partner = new PartnerDiscount { ClinicId = clinic.Id, PartnerName = "Pet", Category = "RACAO", DiscountPercent = 10 };
        context.AddRange(plan, keptPlan, pet, partner);
        await context.SaveChangesAsync();

        var template = new CouponTemplate { PartnerDiscountId = partner.Id, Title = "10%", DiscountType = "PERCENTUAL", DiscountValue = 10, PointsRequired = 50 };
        context.Add(template);
        await context.SaveChangesAsync();

        var coupon = new Coupon { TemplateId = template.Id, Code = "C1" };
        context.AddRange(coupon, new Subscription { PetId = pet.Id, PlanId = plan.Id, StartDate = DateTime.Today });
        await context.SaveChangesAsync();
        context.Add(new Redeem { TutorId = 1, CouponId = coupon.Id, PointsUsed = 50 });
        await context.SaveChangesAsync();

        // Act
        await new ClinicRepository(context).RemoveWithDependentsAsync(clinic);
        await new UnitOfWork(context).SaveChangesAsync();

        // Assert
        Assert.Equal(new[] { "Outra" }, await context.Clinics.Select(c => c.Name).ToListAsync());
        Assert.Equal(new[] { "Basico" }, await context.Plans.Select(p => p.Name).ToListAsync());
        Assert.Empty(context.Subscriptions);
        Assert.Empty(context.PartnerDiscounts);
        Assert.Empty(context.CouponTemplates);
        Assert.Empty(context.Coupons);
        Assert.Empty(context.Redeems);
    }

    [Fact]
    public async Task PetRepository_RemoveWithDependents_DeveRemoverEventosEAssinaturasDoPet()
    {
        // Arrange
        await using var context = TestDb.Create();
        context.AddRange(new Tutor { Id = 1, Name = "Ana", Email = "a@x.com", PasswordHash = "h" }, new Species { Id = 1, Name = "Cachorro" },
            new EventType { Id = 1, Name = "Vacina", Category = "PREVENTIVO" });
        await context.SaveChangesAsync();
        var pet = new Pet { TutorId = 1, SpeciesId = 1, Name = "Rex" };
        context.Add(pet);
        await context.SaveChangesAsync();
        context.Add(new HealthEvent { PetId = pet.Id, EventTypeId = 1, EventDate = DateTime.Today });
        await context.SaveChangesAsync();

        // Act
        await new PetRepository(context).RemoveWithDependentsAsync(pet);
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(context.Pets);
        Assert.Empty(context.HealthEvents);
    }

    [Fact]
    public async Task CouponRepository_RemoveWithDependents_DeveRemoverResgatesDoCupom()
    {
        // Arrange
        await using var context = TestDb.Create();
        context.AddRange(new Tutor { Id = 1, Name = "Ana", Email = "a@x.com", PasswordHash = "h" }, new Coupon { Id = 5, Code = "X" });
        await context.SaveChangesAsync();
        context.Add(new Redeem { TutorId = 1, CouponId = 5, PointsUsed = 10 });
        await context.SaveChangesAsync();
        var coupon = await context.Coupons.SingleAsync();

        // Act
        await new CouponRepository(context).RemoveWithDependentsAsync(coupon);
        await context.SaveChangesAsync();

        // Assert
        Assert.Empty(context.Coupons);
        Assert.Empty(context.Redeems);
    }

    [Fact]
    public async Task RewardRepository_GetPointsSummary_DeveSomarRecebidosEResgatados()
    {
        // Arrange
        await using var context = TestDb.Create();
        context.AddRange(new Tutor { Id = 1, Name = "Ana", Email = "a@x.com", PasswordHash = "h" },
            new Tutor { Id = 2, Name = "Bruno", Email = "b@x.com", PasswordHash = "h" },
            new RewardAction { Id = 1, Name = "A" }, new Coupon { Id = 1, Code = "C" });
        await context.SaveChangesAsync();
        context.AddRange(
            new RewardPoint { TutorId = 1, RewardActionId = 1, Points = 90 },
            new RewardPoint { TutorId = 1, RewardActionId = 1, Points = 45 },
            new RewardPoint { TutorId = 2, RewardActionId = 1, Points = 1000 },
            new Redeem { TutorId = 1, CouponId = 1, PointsUsed = 100 });
        await context.SaveChangesAsync();

        // Act
        var summary = await new RewardRepository(context).GetPointsSummaryAsync(1);

        // Assert
        Assert.Equal(new PointsSummary(135, 100), summary);
        Assert.Equal(35, summary.Balance);
    }

    [Fact]
    public async Task RewardRepository_GetPointsSummary_SemMovimentacao_DeveRetornarZeros()
    {
        // Arrange
        await using var context = TestDb.Create();

        // Act
        var summary = await new RewardRepository(context).GetPointsSummaryAsync(1);

        // Assert
        Assert.Equal(0, summary.Balance);
    }
}

public class UnitOfWorkTests
{
    [Theory]
    [InlineData("ORA-00001: unique constraint (PETFLOW.SYS_C001) violated")]
    [InlineData("Violation of UNIQUE KEY constraint 'UQ_Email'")]
    public void Translate_ViolacaoDeUnicidade_DeveGerarConflictException(string message)
    {
        // Arrange
        var exception = new DbUpdateException("erro", new Exception(message));

        // Act
        var translated = UnitOfWork.Translate(exception);

        // Assert
        var conflict = Assert.IsType<ConflictException>(translated);
        Assert.Contains("duplicado", conflict.Message);
        Assert.Same(exception, conflict.InnerException);
    }

    [Theory]
    [InlineData("ORA-02291: integrity constraint (FK_PET_TUTOR) violated - parent key not found")]
    [InlineData("ORA-02292: integrity constraint violated - child record found")]
    [InlineData("The DELETE statement conflicted with the REFERENCE constraint")]
    public void Translate_ViolacaoDeChaveEstrangeira_DeveGerarConflictException(string message)
    {
        // Arrange
        var exception = new DbUpdateException("erro", new Exception(message));

        // Act
        var translated = UnitOfWork.Translate(exception);

        // Assert
        var conflict = Assert.IsType<ConflictException>(translated);
        Assert.Contains("relacionados", conflict.Message);
    }

    [Fact]
    public void Translate_ErroDesconhecido_DeveManterAExcecaoOriginal()
    {
        // Arrange
        var exception = new DbUpdateException("timeout", new Exception("ORA-12170"));

        // Act
        var translated = UnitOfWork.Translate(exception);

        // Assert
        Assert.Same(exception, translated);
    }
}

public class SecurityTests
{
    [Fact]
    public void PasswordHasher_Hash_DeveGerarBcryptDiferenteACadaChamadaEVerificar()
    {
        // Arrange
        var sut = new PasswordHasher();

        // Act
        var first = sut.Hash("SenhaSegura123");
        var second = sut.Hash("SenhaSegura123");

        // Assert
        Assert.StartsWith("$2", first);
        Assert.NotEqual(first, second);
        Assert.True(sut.Verify("SenhaSegura123", first));
        Assert.False(sut.Verify("senha-errada", first));
    }

    [Fact]
    public void PasswordHasher_Verify_DeveAceitarHashPbkdf2LegadoDaSprint3()
    {
        // Arrange
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2("legado123", salt, 1000, HashAlgorithmName.SHA256, 32);
        var legacy = $"PBKDF2-SHA256$1000${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        var sut = new PasswordHasher();

        // Act
        var valid = sut.Verify("legado123", legacy);
        var invalid = sut.Verify("outra", legacy);

        // Assert
        Assert.True(valid);
        Assert.False(invalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hash_ana_123")]
    [InlineData("PBKDF2-SHA256$abc")]
    [InlineData("PBKDF2-SHA256$1000$@@@$@@@")]
    public void PasswordHasher_Verify_ComHashInvalido_DeveRetornarFalseSemLancar(string stored)
    {
        // Arrange
        var sut = new PasswordHasher();

        // Act
        var result = sut.Verify("qualquer", stored);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenService_CreateToken_DeveEmitirJwtComClaimsEExpiracao()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = Options.Create(new JwtOptions { Key = new string('k', 40), Issuer = "iss", Audience = "aud", ExpirationMinutes = 30 });
        var sut = new TokenService(options, new FakeTimeProvider(now));
        var tutor = new Tutor { Id = 12, Name = "Ana", Email = "ana@x.com", Role = "ADMIN" };

        // Act
        var token = sut.CreateToken(tutor);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        // Assert
        Assert.Equal(now.UtcDateTime.AddMinutes(30), token.ExpiresAt);
        Assert.Equal("iss", jwt.Issuer);
        Assert.Contains("aud", jwt.Audiences);
        Assert.Equal("12", jwt.Claims.First(c => c.Type is JwtRegisteredClaimNames.Sub or ClaimTypes.NameIdentifier).Value);
        Assert.Contains(jwt.Claims, c => c.Value == "ADMIN");
    }
}

public class MongoHealthCheckTests
{
    [Fact]
    public async Task CheckHealth_ComMongoInacessivel_DeveRetornarUnhealthy()
    {
        // Arrange
        var settings = Options.Create(new MongoSettings { ConnectionString = "mongodb://127.0.0.1:1", ServerSelectionTimeoutSeconds = 1 });
        var sut = new MongoHealthCheck(new MongoContext(settings));

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
