using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;

namespace PetFlowAPI.Tests.Domain;

public class EntityDefaultsTests
{
    [Fact]
    public void NovasEntidades_DevemNascerComValoresPadraoCoerentesComODdl()
    {
        // Arrange & Act
        var tutor = new Tutor();
        var plan = new Plan();
        var subscription = new Subscription();
        var healthEvent = new HealthEvent();
        var coupon = new Coupon();

        // Assert
        Assert.Equal(UserRoles.Tutor, tutor.Role);
        Assert.Equal(1, plan.PointsPerEvent);
        Assert.Equal(SubscriptionStatus.ATIVO, subscription.Status);
        Assert.Equal(HealthEventStatus.AGENDADO, healthEvent.Status);
        Assert.Equal(CouponStatus.DISPONIVEL, coupon.Status);
        Assert.True((DateTime.UtcNow - tutor.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void Entidades_DevemPermitirNavegacaoEPopularPropriedades()
    {
        // Arrange
        var tutor = new Tutor { Id = 1 };
        var pet = new Pet { Id = 2, Tutor = tutor, TutorId = 1, Species = new Species { Id = 3, Name = "Gato", Description = "Felino" } };
        tutor.Pets.Add(pet);
        tutor.Addresses.Add(new Address { Id = 4, Tutor = tutor, TutorId = 1, Street = "Rua", City = "SP", State = "SP", ZipCode = "01310100" });
        pet.RiskScores.Add(new RiskScore { Id = 5, Pet = pet, PetId = 2, Score = 10, RiskLevel = new RiskLevel { Id = 6, Name = "Baixo", MinScore = 0, MaxScore = 30 } });
        var point = new RewardPoint { Id = 7, Tutor = tutor, TutorId = 1, RewardAction = new RewardAction { Id = 8, Name = "A", PointsValue = 5 }, Points = 5, ReferenceId = 2 };
        var redeem = new Redeem { Id = 9, Tutor = tutor, Coupon = new Coupon { Id = 10, Template = new CouponTemplate { Id = 11, PartnerDiscount = new PartnerDiscount { Id = 12, Clinic = new Clinic { Id = 13 } } } } };
        var log = new LogErro { Id = 1, ProcedureName = "sp", Usuario = "u", CodigoErro = -1, MensagemErro = "x" };
        var audit = new LogAuditoriaHealthEvent { Id = 1, NomeUsuario = "u", TipoOperacao = "INSERT", ValoresNovos = "{}" };

        // Act & Assert
        Assert.Same(tutor, pet.Tutor);
        Assert.Single(tutor.Addresses);
        Assert.Equal("Baixo", pet.RiskScores.Single().RiskLevel!.Name);
        Assert.Equal(5, point.RewardAction!.PointsValue);
        Assert.Equal(13, redeem.Coupon!.Template!.PartnerDiscount!.Clinic!.Id);
        Assert.Equal("sp", log.ProcedureName);
        Assert.Equal("INSERT", audit.TipoOperacao);
    }

    [Fact]
    public void NotFoundException_ComRecursoEChave_DeveMontarMensagemPadrao()
    {
        // Arrange & Act
        var exception = new NotFoundException("Pet", 42);

        // Assert
        Assert.Contains("Pet", exception.Message);
        Assert.Contains("42", exception.Message);
    }

    [Fact]
    public void RequestValidationException_DeveExporErrosPorCampo()
    {
        // Arrange & Act
        var exception = new RequestValidationException("email", "inválido");

        // Assert
        Assert.Equal(new[] { "inválido" }, exception.Errors["email"]);
    }
}
