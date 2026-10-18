using System.Linq.Expressions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Tests.Application;

public class RedeemServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IRepository<Redeem>> _redeems = new();
    private readonly Mock<IRepository<Tutor>> _tutors = Mocks.RepositoryExists<Tutor>(true);
    private readonly Mock<ICouponRepository> _coupons = new();
    private readonly Mock<IRepository<CouponTemplate>> _templates = new();
    private readonly Mock<IRewardRepository> _rewards = new();
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();
    private Redeem? _saved;

    public RedeemServiceTests()
    {
        _templates.Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new CouponTemplate { Id = 1, PointsRequired = 100 });
        _redeems.Setup(r => r.AddAsync(It.IsAny<Redeem>(), It.IsAny<CancellationToken>()))
            .Callback<Redeem, CancellationToken>((r, _) => { r.Id = 50; _saved = r; }).Returns(Task.CompletedTask);
    }

    private RedeemService CreateSut(FakeUser user) => new(_redeems.Object, _tutors.Object, _coupons.Object, _templates.Object, _rewards.Object,
        _uow.Object, user, new FakeTimeProvider(Now), Mocks.Logger<RedeemService>());

    private Coupon AvailableCoupon(DateTime? expiration = null)
    {
        var coupon = new Coupon { Id = 9, Code = "PET10", TemplateId = 1, Status = CouponStatus.DISPONIVEL, ExpirationDate = expiration };
        _coupons.Setup(c => c.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(coupon);
        return coupon;
    }

    private void Balance(int earned, int redeemed = 0)
        => _rewards.Setup(r => r.GetPointsSummaryAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new PointsSummary(earned, redeemed));

    [Fact]
    public async Task Create_ComSaldoSuficiente_DeveDebitarPontosDoModeloEMarcarCupomResgatado()
    {
        // Arrange
        var coupon = AvailableCoupon();
        Balance(earned: 250, redeemed: 100);

        // Act
        var response = await CreateSut(FakeUser.Tutor(7)).CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 });

        // Assert
        Assert.Equal(100, response.PointsUsed);
        Assert.Equal(100, _saved!.PointsUsed);
        Assert.Equal(CouponStatus.RESGATADO, coupon.Status);
        _coupons.Verify(c => c.Update(coupon), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ComSaldoExatamenteIgualAoNecessario_DevePermitir()
    {
        // Arrange
        AvailableCoupon();
        Balance(earned: 100);

        // Act
        var response = await CreateSut(FakeUser.Tutor(7)).CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 });

        // Assert
        Assert.Equal(50, response.Id);
    }

    [Fact]
    public async Task Create_ComSaldoInsuficiente_DeveLancarBusinessRuleExceptionSemPersistir()
    {
        // Arrange
        var coupon = AvailableCoupon();
        Balance(earned: 150, redeemed: 100);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 }));

        // Assert
        var businessRule = Assert.IsType<BusinessRuleException>(exception);
        Assert.Contains("insuficiente", businessRule.Message);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _redeems.Verify(r => r.AddAsync(It.IsAny<Redeem>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.NotNull(coupon);
    }

    [Fact]
    public async Task Create_ComCupomJaResgatado_DeveLancarBusinessRuleException()
    {
        // Arrange
        var coupon = AvailableCoupon();
        coupon.Status = CouponStatus.RESGATADO;
        Balance(earned: 500);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 }));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
    }

    [Fact]
    public async Task Create_ComCupomExpirado_DeveLancarBusinessRuleException()
    {
        // Arrange
        AvailableCoupon(expiration: Now.UtcDateTime.AddDays(-1));
        Balance(earned: 500);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 }));

        // Assert
        var businessRule = Assert.IsType<BusinessRuleException>(exception);
        Assert.Contains("expirado", businessRule.Message);
    }

    [Fact]
    public async Task Create_ComCupomInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _coupons.Setup(c => c.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Coupon?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 }));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task Create_ParaOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Tutor(8));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 }));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task Create_ComTutorInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var tutors = Mocks.RepositoryExists<Tutor>(false);
        var sut = new RedeemService(_redeems.Object, tutors.Object, _coupons.Object, _templates.Object, _rewards.Object,
            _uow.Object, FakeUser.Admin(), new FakeTimeProvider(Now), Mocks.Logger<RedeemService>());

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(new RedeemRequestDTO { TutorId = 7, CouponId = 9 }));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task GetById_DeResgateDeOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        _redeems.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Redeem { Id = 1, TutorId = 7 });

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(8)).GetByIdAsync(1));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task GetPaged_ComoTutor_DeveRestringirAosProprioResgates()
    {
        // Arrange
        Expression<Func<Redeem, bool>>? captured = null;
        _redeems.Setup(r => r.GetPagedAsync(It.IsAny<Expression<Func<Redeem, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Redeem, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page<Redeem>());

        // Act
        await CreateSut(FakeUser.Tutor(7)).GetPagedAsync(new RedeemFilter(), new PetFlowAPI.Application.Common.PagingParameters());

        // Assert
        var predicate = captured!.Compile();
        Assert.True(predicate(new Redeem { TutorId = 7 }));
        Assert.False(predicate(new Redeem { TutorId = 8 }));
    }

    [Fact]
    public async Task Delete_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _redeems.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Redeem?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Admin()).DeleteAsync(1));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }
}

public class CouponServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICouponRepository> _coupons = new();
    private readonly Mock<IRepository<CouponTemplate>> _templates = Mocks.RepositoryExists<CouponTemplate>(true);
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();

    private CouponService CreateSut(Mock<IRepository<CouponTemplate>>? templates = null)
        => new(_coupons.Object, (templates ?? _templates).Object, _uow.Object, new FakeTimeProvider(Now), Mocks.Logger<CouponService>());

    private static CouponRequestDTO Request() => new() { Code = " PET10 ", TemplateId = 1, ExpirationDate = new DateTime(2026, 12, 31) };

    [Fact]
    public async Task Create_ComDadosValidos_DeveNormalizarCodigoEPersistir()
    {
        // Arrange
        Coupon? saved = null;
        _coupons.Setup(c => c.AddAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .Callback<Coupon, CancellationToken>((c, _) => saved = c).Returns(Task.CompletedTask);

        // Act
        var response = await CreateSut().CreateAsync(Request());

        // Assert
        Assert.Equal("PET10", saved!.Code);
        Assert.Equal("PET10", response.Code);
        Assert.Equal(CouponStatus.DISPONIVEL, saved.Status);
    }

    [Fact]
    public async Task Create_ComExpiracaoNoPassado_DeveLancarRequestValidationException()
    {
        // Arrange
        var request = Request();
        request.ExpirationDate = new DateTime(2026, 6, 14);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().CreateAsync(request));

        // Assert
        var validation = Assert.IsType<RequestValidationException>(exception);
        Assert.Contains("ExpirationDate", validation.Errors.Keys);
    }

    [Fact]
    public async Task Create_ComExpiracaoHoje_DevePermitir()
    {
        // Arrange
        var request = Request();
        request.ExpirationDate = Now.UtcDateTime.Date;

        // Act
        var response = await CreateSut().CreateAsync(request);

        // Assert
        Assert.Equal("PET10", response.Code);
    }

    [Fact]
    public async Task Create_ComCodigoDuplicado_DeveLancarConflictException()
    {
        // Arrange
        _coupons.Setup(c => c.ExistsAsync(It.IsAny<Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().CreateAsync(Request()));

        // Assert
        Assert.IsType<ConflictException>(exception);
    }

    [Fact]
    public async Task Create_ComModeloInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var templates = Mocks.RepositoryExists<CouponTemplate>(false);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(templates).CreateAsync(Request()));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task UpdateStatus_DeveAlterarStatusEPersistir()
    {
        // Arrange
        var coupon = new Coupon { Id = 4, Status = CouponStatus.DISPONIVEL };
        _coupons.Setup(c => c.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(coupon);

        // Act
        var response = await CreateSut().UpdateStatusAsync(4, CouponStatus.UTILIZADO);

        // Assert
        Assert.Equal(CouponStatus.UTILIZADO, response.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_DeveRemoverCupomComResgates()
    {
        // Arrange
        var coupon = new Coupon { Id = 4 };
        _coupons.Setup(c => c.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(coupon);

        // Act
        await CreateSut().DeleteAsync(4);

        // Assert
        _coupons.Verify(c => c.RemoveWithDependentsAsync(coupon, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _coupons.Setup(c => c.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync((Coupon?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().GetByIdAsync(4));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }
}
