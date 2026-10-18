using System.Linq.Expressions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Domain.Services;

namespace PetFlowAPI.Tests.Application;

/// <summary>Cenários de leitura, listagem e atualização dos serviços de catálogo e assinatura.</summary>
public class ServiceReadAndUpdateTests
{
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();

    // ───────────── Clínica ─────────────
    [Fact]
    public async Task ClinicGetById_DeveRetornarClinica()
    {
        // Arrange
        var clinics = new Mock<IClinicRepository>();
        clinics.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Clinic { Id = 1, Name = "VetVida" });
        var sut = new ClinicService(clinics.Object, _uow.Object, Mocks.Logger<ClinicService>());

        // Act
        var clinic = await sut.GetByIdAsync(1);

        // Assert
        Assert.Equal("VetVida", clinic.Name);
    }

    [Fact]
    public async Task ClinicGetPaged_ComFiltroDeNome_DeveComporPredicado()
    {
        // Arrange
        Expression<Func<Clinic, bool>>? captured = null;
        var clinics = new Mock<IClinicRepository>();
        clinics.Setup(c => c.GetPagedAsync(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Clinic, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page(new Clinic { Id = 1, Name = "VetVida" }));
        var sut = new ClinicService(clinics.Object, _uow.Object, Mocks.Logger<ClinicService>());

        // Act
        var page = await sut.GetPagedAsync(new ClinicFilter { Name = "Vet" }, new PagingParameters());

        // Assert
        Assert.True(captured!.Compile()(new Clinic { Name = "VetVida" }));
        Assert.False(captured.Compile()(new Clinic { Name = "Outra" }));
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task ClinicUpdate_DeveAplicarDadosEPersistir()
    {
        // Arrange
        var clinic = new Clinic { Id = 1, Name = "Antigo" };
        var clinics = new Mock<IClinicRepository>();
        clinics.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(clinic);
        var sut = new ClinicService(clinics.Object, _uow.Object, Mocks.Logger<ClinicService>());

        // Act
        var response = await sut.UpdateAsync(1, new ClinicRequestDTO { Name = "Novo", Phone = "1199" });

        // Assert
        Assert.Equal("Novo", clinic.Name);
        Assert.Equal("Novo", response.Name);
        clinics.Verify(c => c.Update(clinic), Times.Once);
    }

    // ───────────── Plano ─────────────
    private PlanService PlanSut(Mock<IRepository<Plan>> plans)
        => new(plans.Object, Mocks.RepositoryExists<Clinic>(true).Object, Mocks.RepositoryExists<Subscription>(false).Object, _uow.Object, Mocks.Logger<PlanService>());

    [Fact]
    public async Task PlanGetPaged_DeveFiltrarPorNomeEClinica()
    {
        // Arrange
        Expression<Func<Plan, bool>>? captured = null;
        var plans = new Mock<IRepository<Plan>>();
        plans.Setup(p => p.GetPagedAsync(It.IsAny<Expression<Func<Plan, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Plan, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page(new Plan { Id = 1, Name = "Premium" }));

        // Act
        await PlanSut(plans).GetPagedAsync(new PlanFilter { Name = "Prem", ClinicId = 2 }, new PagingParameters { SortBy = "price", Direction = "desc" });

        // Assert
        var predicate = captured!.Compile();
        Assert.True(predicate(new Plan { Name = "Premium", ClinicId = 2 }));
        Assert.False(predicate(new Plan { Name = "Premium", ClinicId = 3 }));
    }

    [Fact]
    public async Task PlanUpdate_DeveAplicarDados()
    {
        // Arrange
        var plan = new Plan { Id = 1, Name = "Antigo", PointsPerEvent = 1 };
        var plans = new Mock<IRepository<Plan>>();
        plans.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        // Act
        var response = await PlanSut(plans).UpdateAsync(1, new PlanRequestDTO { Name = "Novo", Price = 10, DurationDays = 30, PointsPerEvent = 3, ClinicId = 1 });

        // Assert
        Assert.Equal(3, plan.PointsPerEvent);
        Assert.Equal("Novo", response.Name);
    }

    [Fact]
    public async Task PlanGetById_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var plans = new Mock<IRepository<Plan>>();

        // Act
        var exception = await Record.ExceptionAsync(() => PlanSut(plans).GetByIdAsync(1));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    // ───────────── Cupom ─────────────
    private CouponService CouponSut(Mock<ICouponRepository> coupons)
        => new(coupons.Object, Mocks.RepositoryExists<CouponTemplate>(true).Object, _uow.Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero)), Mocks.Logger<CouponService>());

    [Fact]
    public async Task CouponGetPaged_DeveFiltrarPorCodigoEStatus()
    {
        // Arrange
        Expression<Func<Coupon, bool>>? captured = null;
        var coupons = new Mock<ICouponRepository>();
        coupons.Setup(c => c.GetPagedAsync(It.IsAny<Expression<Func<Coupon, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Coupon, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page<Coupon>());

        // Act
        await CouponSut(coupons).GetPagedAsync(new CouponFilter { Code = "PET", Status = CouponStatus.DISPONIVEL }, new PagingParameters());

        // Assert
        var predicate = captured!.Compile();
        Assert.True(predicate(new Coupon { Code = "PET10", Status = CouponStatus.DISPONIVEL }));
        Assert.False(predicate(new Coupon { Code = "PET10", Status = CouponStatus.UTILIZADO }));
    }

    [Fact]
    public async Task CouponUpdate_DeveAplicarDados()
    {
        // Arrange
        var coupon = new Coupon { Id = 1, Code = "VELHO", TemplateId = 1 };
        var coupons = new Mock<ICouponRepository>();
        coupons.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(coupon);

        // Act
        var response = await CouponSut(coupons).UpdateAsync(1, new CouponRequestDTO { Code = "NOVO", TemplateId = 1, Status = CouponStatus.UTILIZADO });

        // Assert
        Assert.Equal("NOVO", coupon.Code);
        Assert.Equal(CouponStatus.UTILIZADO, response.Status);
    }

    // ───────────── Assinatura ─────────────
    private SubscriptionService SubscriptionSut(Mock<IRepository<Subscription>> subscriptions, FakeUser user)
    {
        var pets = new Mock<IRepository<Pet>>();
        pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { Id = 1, TutorId = 7 });
        return new SubscriptionService(subscriptions.Object, pets.Object, Mocks.RepositoryExists<Plan>(true).Object, _uow.Object, user, Mocks.Logger<SubscriptionService>());
    }

    [Fact]
    public async Task SubscriptionGetById_DeveRetornarAssinaturaDoTutor()
    {
        // Arrange
        var subscriptions = new Mock<IRepository<Subscription>>();
        subscriptions.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new Subscription { Id = 3, PetId = 1, PlanId = 2 });

        // Act
        var response = await SubscriptionSut(subscriptions, FakeUser.Tutor(7)).GetByIdAsync(3);

        // Assert
        Assert.Equal(2, response.PlanId);
    }

    [Fact]
    public async Task SubscriptionGetById_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var subscriptions = new Mock<IRepository<Subscription>>();

        // Act
        var exception = await Record.ExceptionAsync(() => SubscriptionSut(subscriptions, FakeUser.Tutor(7)).GetByIdAsync(3));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task SubscriptionUpdate_DeveAplicarPeriodoEStatus()
    {
        // Arrange
        var subscription = new Subscription { Id = 3, PetId = 1, PlanId = 2, Status = SubscriptionStatus.ATIVO, StartDate = new DateTime(2026, 1, 1) };
        var subscriptions = new Mock<IRepository<Subscription>>();
        subscriptions.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        var request = new SubscriptionRequestDTO { PetId = 1, PlanId = 4, StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 8, 1), Status = SubscriptionStatus.ENCERRADO };

        // Act
        var response = await SubscriptionSut(subscriptions, FakeUser.Tutor(7)).UpdateAsync(3, request);

        // Assert
        Assert.Equal(4, subscription.PlanId);
        Assert.Equal(SubscriptionStatus.ENCERRADO, response.Status);
    }

    [Fact]
    public async Task SubscriptionDelete_DeveRemover()
    {
        // Arrange
        var subscription = new Subscription { Id = 3, PetId = 1 };
        var subscriptions = new Mock<IRepository<Subscription>>();
        subscriptions.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        // Act
        await SubscriptionSut(subscriptions, FakeUser.Tutor(7)).DeleteAsync(3);

        // Assert
        subscriptions.Verify(s => s.Remove(subscription), Times.Once);
    }

    // ───────────── Evento de saúde / Resgate ─────────────
    [Fact]
    public async Task HealthEventGetById_DeAssinanteNaoDono_DeveLancarForbiddenException()
    {
        // Arrange
        var events = new Mock<IRepository<HealthEvent>>();
        events.Setup(e => e.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new HealthEvent { Id = 1, PetId = 1 });
        var pets = new Mock<IRepository<Pet>>();
        pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { Id = 1, TutorId = 7 });
        var sut = new HealthEventService(events.Object, pets.Object, new Mock<IRepository<EventType>>().Object, new Mock<IRepository<Clinic>>().Object,
            new Mock<IRepository<Subscription>>().Object, new Mock<IRepository<Plan>>().Object, new Mock<IRepository<RewardAction>>().Object,
            new Mock<IRepository<RewardPoint>>().Object, new RewardPointCalculator(), _uow.Object, FakeUser.Tutor(8), Mocks.Logger<HealthEventService>());

        // Act
        var exception = await Record.ExceptionAsync(() => sut.GetByIdAsync(1));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task RedeemGetById_DoProprioTutor_DeveRetornar()
    {
        // Arrange
        var redeems = new Mock<IRepository<Redeem>>();
        redeems.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Redeem { Id = 1, TutorId = 7, PointsUsed = 100 });
        var sut = new RedeemService(redeems.Object, new Mock<IRepository<Tutor>>().Object, new Mock<ICouponRepository>().Object,
            new Mock<IRepository<CouponTemplate>>().Object, new Mock<IRewardRepository>().Object, _uow.Object, FakeUser.Tutor(7),
            TimeProvider.System, Mocks.Logger<RedeemService>());

        // Act
        var response = await sut.GetByIdAsync(1);

        // Assert
        Assert.Equal(100, response.PointsUsed);
    }

    [Fact]
    public async Task RedeemDelete_DeveRemoverEPersistir()
    {
        // Arrange
        var redeem = new Redeem { Id = 1, TutorId = 7 };
        var redeems = new Mock<IRepository<Redeem>>();
        redeems.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(redeem);
        var sut = new RedeemService(redeems.Object, new Mock<IRepository<Tutor>>().Object, new Mock<ICouponRepository>().Object,
            new Mock<IRepository<CouponTemplate>>().Object, new Mock<IRewardRepository>().Object, _uow.Object, FakeUser.Admin(),
            TimeProvider.System, Mocks.Logger<RedeemService>());

        // Act
        await sut.DeleteAsync(1);

        // Assert
        redeems.Verify(r => r.Remove(redeem), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
