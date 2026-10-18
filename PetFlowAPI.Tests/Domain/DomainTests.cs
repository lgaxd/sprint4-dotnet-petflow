using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Domain.Services;

namespace PetFlowAPI.Tests.Domain;

public class RewardPointCalculatorTests
{
    private readonly IRewardPointCalculator _sut = new RewardPointCalculator();

    [Theory]
    [InlineData(10, 1, 10)]
    [InlineData(10, 2, 20)]
    [InlineData(25, 3, 75)]
    [InlineData(0, 5, 0)]
    public void Calculate_ComPontosBaseEMultiplicadorValidos_DeveRetornarPontosMultiplicados(int basePoints, int multiplier, int expected)
    {
        // Arrange — valores fornecidos por [InlineData]

        // Act
        var result = _sut.Calculate(basePoints, multiplier);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_ComPontosBaseNegativos_DeveLancarArgumentOutOfRangeException()
    {
        // Arrange
        const int basePoints = -5;

        // Act
        var exception = Record.Exception(() => _sut.Calculate(basePoints, 1));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_ComMultiplicadorZeroOuNegativo_DeveLancarArgumentOutOfRangeException(int multiplier)
    {
        // Arrange
        const int basePoints = 10;

        // Act
        var exception = Record.Exception(() => _sut.Calculate(basePoints, multiplier));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void Calculate_ComOverflow_DeveLancarOverflowException()
    {
        // Arrange
        const int basePoints = int.MaxValue;

        // Act
        var exception = Record.Exception(() => _sut.Calculate(basePoints, 2));

        // Assert
        Assert.IsType<OverflowException>(exception);
    }
}

public class CouponTests
{
    private static readonly DateTime Today = new(2026, 6, 15);

    [Fact]
    public void MarkAsRedeemed_CupomDisponivelESemExpiracao_DeveMarcarComoResgatado()
    {
        // Arrange
        var coupon = new Coupon { Code = "PET10", Status = CouponStatus.DISPONIVEL };

        // Act
        coupon.MarkAsRedeemed(Today);

        // Assert
        Assert.Equal(CouponStatus.RESGATADO, coupon.Status);
    }

    [Theory]
    [InlineData(CouponStatus.RESGATADO)]
    [InlineData(CouponStatus.UTILIZADO)]
    public void MarkAsRedeemed_CupomIndisponivel_DeveLancarBusinessRuleException(CouponStatus status)
    {
        // Arrange
        var coupon = new Coupon { Code = "PET10", Status = status };

        // Act
        var exception = Record.Exception(() => coupon.MarkAsRedeemed(Today));

        // Assert
        var businessRule = Assert.IsType<BusinessRuleException>(exception);
        Assert.Contains("PET10", businessRule.Message);
    }

    [Fact]
    public void MarkAsRedeemed_CupomExpirado_DeveLancarBusinessRuleException()
    {
        // Arrange
        var coupon = new Coupon { Code = "OLD", ExpirationDate = Today.AddDays(-1) };

        // Act
        var exception = Record.Exception(() => coupon.MarkAsRedeemed(Today));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
        Assert.Equal(CouponStatus.DISPONIVEL, coupon.Status);
    }

    [Fact]
    public void IsExpired_ExpiraHoje_NaoDeveEstarExpirado()
    {
        // Arrange
        var coupon = new Coupon { ExpirationDate = Today };

        // Act
        var expired = coupon.IsExpired(Today);

        // Assert
        Assert.False(expired);
    }

    [Fact]
    public void IsExpired_SemDataDeExpiracao_NaoDeveEstarExpirado()
    {
        // Arrange
        var coupon = new Coupon { ExpirationDate = null };

        // Act
        var expired = coupon.IsExpired(Today);

        // Assert
        Assert.False(expired);
    }
}

public class SubscriptionTests
{
    [Fact]
    public void EnsureValidPeriod_TerminoAnteriorAoInicio_DeveLancarBusinessRuleException()
    {
        // Arrange
        var subscription = new Subscription { StartDate = new DateTime(2026, 5, 10), EndDate = new DateTime(2026, 5, 9) };

        // Act
        var exception = Record.Exception(subscription.EnsureValidPeriod);

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
    }

    [Fact]
    public void EnsureValidPeriod_SemDataDeTermino_NaoDeveLancar()
    {
        // Arrange
        var subscription = new Subscription { StartDate = new DateTime(2026, 5, 10), EndDate = null };

        // Act
        var exception = Record.Exception(subscription.EnsureValidPeriod);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ChangeStatus_CanceladaParaAtiva_DeveLancarBusinessRuleException()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.CANCELADO };

        // Act
        var exception = Record.Exception(() => subscription.ChangeStatus(SubscriptionStatus.ATIVO));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
    }

    [Theory]
    [InlineData(SubscriptionStatus.ATIVO, SubscriptionStatus.ENCERRADO)]
    [InlineData(SubscriptionStatus.ATIVO, SubscriptionStatus.EXPIRADO)]
    [InlineData(SubscriptionStatus.ENCERRADO, SubscriptionStatus.ATIVO)]
    public void ChangeStatus_TransicaoPermitida_DeveAtualizarStatus(SubscriptionStatus from, SubscriptionStatus to)
    {
        // Arrange
        var subscription = new Subscription { Status = from };

        // Act
        subscription.ChangeStatus(to);

        // Assert
        Assert.Equal(to, subscription.Status);
    }
}

public class HealthEventTests
{
    [Theory]
    [InlineData(HealthEventStatus.AGENDADO, true)]
    [InlineData(HealthEventStatus.CANCELADO, true)]
    [InlineData(HealthEventStatus.REALIZADO, false)]
    public void WasJustCompleted_EventoRealizado_DeveDependerDoStatusAnterior(HealthEventStatus previous, bool expected)
    {
        // Arrange
        var healthEvent = new HealthEvent { Status = HealthEventStatus.REALIZADO };

        // Act
        var result = healthEvent.WasJustCompleted(previous);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WasJustCompleted_EventoAgendado_NuncaConcluiu()
    {
        // Arrange
        var healthEvent = new HealthEvent { Status = HealthEventStatus.AGENDADO };

        // Act
        var result = healthEvent.WasJustCompleted(HealthEventStatus.AGENDADO);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ChangeStatus_CanceladoParaRealizado_DeveLancarBusinessRuleException()
    {
        // Arrange
        var healthEvent = new HealthEvent { Status = HealthEventStatus.CANCELADO };

        // Act
        var exception = Record.Exception(() => healthEvent.ChangeStatus(HealthEventStatus.REALIZADO));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
    }
}

public class TutorAndPagingTests
{
    [Theory]
    [InlineData("ADMIN", true)]
    [InlineData("admin", true)]
    [InlineData("TUTOR", false)]
    public void IsAdmin_DeveConsiderarOPerfil(string role, bool expected)
    {
        // Arrange
        var tutor = new Tutor { Role = role };

        // Act
        var isAdmin = tutor.IsAdmin;

        // Assert
        Assert.Equal(expected, isAdmin);
    }

    [Theory]
    [InlineData("ADMIN", true)]
    [InlineData("TUTOR", true)]
    [InlineData("ROOT", false)]
    [InlineData(null, false)]
    public void UserRoles_IsValid_DeveAceitarApenasPerfisDoDdl(string? role, bool expected)
        => Assert.Equal(expected, UserRoles.IsValid(role));

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(95, 10, 10)]
    public void PagedResult_TotalPages_DeveArredondarParaCima(long total, int size, int expectedPages)
    {
        // Arrange
        var result = new PagedResult<int>(Array.Empty<int>(), 0, size, total);

        // Act
        var pages = result.TotalPages;

        // Assert
        Assert.Equal(expectedPages, pages);
    }

    [Fact]
    public void PagedResult_Map_DevePreservarMetadadosEConverterItens()
    {
        // Arrange
        var result = new PagedResult<int>(new[] { 1, 2, 3 }, 2, 3, 20);

        // Act
        var mapped = result.Map(i => i.ToString());

        // Assert
        Assert.Equal(new[] { "1", "2", "3" }, mapped.Items);
        Assert.Equal(2, mapped.Page);
        Assert.Equal(20, mapped.TotalItems);
    }
}
