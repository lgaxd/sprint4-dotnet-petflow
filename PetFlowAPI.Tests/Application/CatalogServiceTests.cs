using System.Linq.Expressions;
using Moq;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Mappings;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Tests.Application;

public class SubscriptionServiceTests
{
    private readonly Mock<IRepository<Subscription>> _subscriptions = new();
    private readonly Mock<IRepository<Pet>> _pets = new();
    private readonly Mock<IRepository<Plan>> _plans = Mocks.RepositoryExists<Plan>(true);
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();

    public SubscriptionServiceTests()
        => _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { Id = 1, TutorId = 7 });

    private SubscriptionService CreateSut(FakeUser user, Mock<IRepository<Plan>>? plans = null)
        => new(_subscriptions.Object, _pets.Object, (plans ?? _plans).Object, _uow.Object, user, Mocks.Logger<SubscriptionService>());

    private static SubscriptionRequestDTO Request() => new()
    {
        PetId = 1, PlanId = 2, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31), Status = SubscriptionStatus.ATIVO
    };

    [Fact]
    public async Task Create_ComDadosValidos_DevePersistirAssinatura()
    {
        // Arrange
        Subscription? saved = null;
        _subscriptions.Setup(s => s.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((s, _) => { s.Id = 3; saved = s; }).Returns(Task.CompletedTask);

        // Act
        var response = await CreateSut(FakeUser.Tutor(7)).CreateAsync(Request());

        // Assert
        Assert.Equal(1, saved!.PetId);
        Assert.Equal(3, response.Id);
        Assert.Equal(SubscriptionStatus.ATIVO, response.Status);
    }

    [Fact]
    public async Task Create_ComTerminoAnteriorAoInicio_DeveLancarBusinessRuleException()
    {
        // Arrange
        var request = Request();
        request.EndDate = new DateTime(2025, 12, 31);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).CreateAsync(request));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ParaPetDeOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Tutor(8));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(Request()));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task Create_ComPlanoInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Tutor(7), Mocks.RepositoryExists<Plan>(false));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(Request()));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task UpdateStatus_DeCanceladaParaAtiva_DeveLancarBusinessRuleException()
    {
        // Arrange
        _subscriptions.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription { Id = 3, PetId = 1, Status = SubscriptionStatus.CANCELADO });

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).UpdateStatusAsync(3, SubscriptionStatus.ATIVO));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
    }

    [Fact]
    public async Task UpdateStatus_DeAtivaParaCancelada_DevePersistir()
    {
        // Arrange
        var subscription = new Subscription { Id = 3, PetId = 1, Status = SubscriptionStatus.ATIVO };
        _subscriptions.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        // Act
        var response = await CreateSut(FakeUser.Tutor(7)).UpdateStatusAsync(3, SubscriptionStatus.CANCELADO);

        // Assert
        Assert.Equal(SubscriptionStatus.CANCELADO, response.Status);
        _subscriptions.Verify(s => s.Update(subscription), Times.Once);
    }

    [Fact]
    public async Task GetPaged_DeveAplicarFiltrosDeStatusEPlanoComEscopoDoTutor()
    {
        // Arrange
        Expression<Func<Subscription, bool>>? captured = null;
        _subscriptions.Setup(s => s.GetPagedAsync(It.IsAny<Expression<Func<Subscription, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Subscription, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page<Subscription>());

        // Act
        await CreateSut(FakeUser.Tutor(7)).GetPagedAsync(new SubscriptionFilter { Status = SubscriptionStatus.ATIVO, PlanId = 2 }, new PagingParameters());

        // Assert
        var predicate = captured!.Compile();
        Assert.True(predicate(new Subscription { PlanId = 2, Status = SubscriptionStatus.ATIVO, Pet = new Pet { TutorId = 7 } }));
        Assert.False(predicate(new Subscription { PlanId = 3, Status = SubscriptionStatus.ATIVO, Pet = new Pet { TutorId = 7 } }));
        Assert.False(predicate(new Subscription { PlanId = 2, Status = SubscriptionStatus.ATIVO, Pet = new Pet { TutorId = 8 } }));
    }
}

public class ClinicAndPlanServiceTests
{
    private readonly Mock<IClinicRepository> _clinics = new();
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();

    private ClinicService CreateClinicSut() => new(_clinics.Object, _uow.Object, Mocks.Logger<ClinicService>());

    [Fact]
    public async Task ClinicCreate_ComCnpjDuplicado_DeveLancarConflictException()
    {
        // Arrange
        _clinics.Setup(c => c.ExistsAsync(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateClinicSut().CreateAsync(new ClinicRequestDTO { Name = "VetVida", Cnpj = "12.345.678/0001-90" }));

        // Assert
        Assert.IsType<ConflictException>(exception);
    }

    [Fact]
    public async Task ClinicCreate_SemCnpj_NaoDeveConsultarDuplicidade()
    {
        // Arrange
        var request = new ClinicRequestDTO { Name = "VetVida", Cnpj = "  " };

        // Act
        var response = await CreateClinicSut().CreateAsync(request);

        // Assert
        Assert.Null(response.Cnpj);
        _clinics.Verify(c => c.ExistsAsync(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClinicDelete_DeveRemoverComDependentesEConfirmar()
    {
        // Arrange
        var clinic = new Clinic { Id = 2 };
        _clinics.Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(clinic);

        // Act
        await CreateClinicSut().DeleteAsync(2);

        // Assert
        _clinics.Verify(c => c.RemoveWithDependentsAsync(clinic, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClinicUpdate_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _clinics.Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((Clinic?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateClinicSut().UpdateAsync(2, new ClinicRequestDTO { Name = "X" }));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    private PlanService CreatePlanSut(Mock<IRepository<Plan>> plans, bool clinicExists = true, bool hasSubscriptions = false)
    {
        var subscriptions = Mocks.RepositoryExists<Subscription>(hasSubscriptions);
        return new PlanService(plans.Object, Mocks.RepositoryExists<Clinic>(clinicExists).Object, subscriptions.Object, _uow.Object, Mocks.Logger<PlanService>());
    }

    private static PlanRequestDTO PlanRequest() => new() { Name = "Premium", Price = 199.9m, DurationDays = 30, PointsPerEvent = 2, ClinicId = 1 };

    [Fact]
    public async Task PlanCreate_ComClinicaExistente_DevePersistir()
    {
        // Arrange
        var plans = new Mock<IRepository<Plan>>();
        Plan? saved = null;
        plans.Setup(p => p.AddAsync(It.IsAny<Plan>(), It.IsAny<CancellationToken>()))
            .Callback<Plan, CancellationToken>((p, _) => saved = p).Returns(Task.CompletedTask);

        // Act
        var response = await CreatePlanSut(plans).CreateAsync(PlanRequest());

        // Assert
        Assert.Equal(2, saved!.PointsPerEvent);
        Assert.Equal(199.9m, response.Price);
    }

    [Fact]
    public async Task PlanCreate_ComClinicaInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var plans = new Mock<IRepository<Plan>>();

        // Act
        var exception = await Record.ExceptionAsync(() => CreatePlanSut(plans, clinicExists: false).CreateAsync(PlanRequest()));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task PlanDelete_ComAssinaturasVinculadas_DeveLancarConflictException()
    {
        // Arrange
        var plans = new Mock<IRepository<Plan>>();
        plans.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Plan { Id = 1 });

        // Act
        var exception = await Record.ExceptionAsync(() => CreatePlanSut(plans, hasSubscriptions: true).DeleteAsync(1));

        // Assert
        Assert.IsType<ConflictException>(exception);
        plans.Verify(p => p.Remove(It.IsAny<Plan>()), Times.Never);
    }

    [Fact]
    public async Task PlanDelete_SemAssinaturas_DeveRemover()
    {
        // Arrange
        var plan = new Plan { Id = 1 };
        var plans = new Mock<IRepository<Plan>>();
        plans.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        // Act
        await CreatePlanSut(plans).DeleteAsync(1);

        // Assert
        plans.Verify(p => p.Remove(plan), Times.Once);
    }
}

public class PagingParametersTests
{
    private static readonly string[] Sortable = { "Id", "Name", "CreatedAt" };

    [Fact]
    public void ToPageRequest_ComPadroes_DeveUsarPagina0Tamanho10EOrdenacaoPadrao()
    {
        // Arrange
        var paging = new PagingParameters();

        // Act
        var request = paging.ToPageRequest(Sortable, "Id");

        // Assert
        Assert.Equal(new PageRequest(0, 10, "Id", false), request);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1000, PageRequest.MaxSize)]
    public void ToPageRequest_DeveLimitarOTamanhoDaPagina(int size, int expected)
    {
        // Arrange
        var paging = new PagingParameters { Size = size };

        // Act
        var request = paging.ToPageRequest(Sortable, "Id");

        // Assert
        Assert.Equal(expected, request.Size);
    }

    [Fact]
    public void ToPageRequest_ComPaginaNegativa_DeveUsarPaginaZero()
    {
        // Arrange
        var paging = new PagingParameters { Page = -3 };

        // Act
        var request = paging.ToPageRequest(Sortable, "Id");

        // Assert
        Assert.Equal(0, request.Page);
    }

    [Fact]
    public void ToPageRequest_DeveNormalizarCapitalizacaoEDirecaoDescendente()
    {
        // Arrange
        var paging = new PagingParameters { SortBy = "createdat", Direction = "DESC" };

        // Act
        var request = paging.ToPageRequest(Sortable, "Id");

        // Assert
        Assert.Equal("CreatedAt", request.SortBy);
        Assert.True(request.Descending);
    }

    [Fact]
    public void ToPageRequest_ComCampoNaoPermitido_DeveLancarRequestValidationException()
    {
        // Arrange
        var paging = new PagingParameters { SortBy = "PasswordHash" };

        // Act
        var exception = Record.Exception(() => paging.ToPageRequest(Sortable, "Id"));

        // Assert
        var validation = Assert.IsType<RequestValidationException>(exception);
        Assert.Contains("sortBy", validation.Errors.Keys);
    }

    [Fact]
    public void ToPageRequest_ComDirecaoInvalida_DeveLancarRequestValidationException()
    {
        // Arrange
        var paging = new PagingParameters { Direction = "sideways" };

        // Act
        var exception = Record.Exception(() => paging.ToPageRequest(Sortable, "Id"));

        // Assert
        Assert.IsType<RequestValidationException>(exception);
    }
}

public class PredicateExtensionsTests
{
    [Fact]
    public void AndAlso_ComPredicadoEsquerdoNulo_DeveRetornarODireito()
    {
        // Arrange
        Expression<Func<Pet, bool>>? left = null;
        Expression<Func<Pet, bool>> right = p => p.TutorId == 1;

        // Act
        var combined = left.AndAlso(right);

        // Assert
        Assert.Same(right, combined);
    }

    [Theory]
    [InlineData(1, "Rex", true)]
    [InlineData(1, "Mimi", false)]
    [InlineData(2, "Rex", false)]
    public void AndAlso_DeveCombinarAmbosPredicados(long tutorId, string name, bool expected)
    {
        // Arrange
        Expression<Func<Pet, bool>> left = p => p.TutorId == 1;
        Expression<Func<Pet, bool>> right = p => p.Name == "Rex";

        // Act
        var combined = left.AndAlso(right).Compile();

        // Assert
        Assert.Equal(expected, combined(new Pet { TutorId = tutorId, Name = name }));
    }
}

public class MappersTests
{
    [Fact]
    public void TutorToResponse_NaoDeveExporHashDeSenha()
    {
        // Arrange
        var tutor = new Tutor { Id = 1, Name = "Ana", Email = "ana@x.com", PasswordHash = "segredo", Role = "ADMIN" };

        // Act
        var dto = tutor.ToResponse();

        // Assert
        Assert.Equal("ADMIN", dto.Role);
        Assert.DoesNotContain("PasswordHash", dto.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void PetApply_DeveCopiarTodosOsCamposDoRequest()
    {
        // Arrange
        var pet = new Pet();
        var request = new PetRequestDTO { Name = "Rex", Breed = "Lab", BirthDate = new DateTime(2020, 1, 1), Weight = 10.5m, SpeciesId = 2, TutorId = 3 };

        // Act
        pet.Apply(request);

        // Assert
        Assert.Equal(("Rex", "Lab", 10.5m, 2L, 3L), (pet.Name, pet.Breed, pet.Weight, pet.SpeciesId, pet.TutorId));
    }

    [Fact]
    public void ClinicApply_CnpjEmBranco_DeveVirarNulo()
    {
        // Arrange
        var clinic = new Clinic { Cnpj = "antigo" };

        // Act
        clinic.Apply(new ClinicRequestDTO { Name = "X", Cnpj = " " });

        // Assert
        Assert.Null(clinic.Cnpj);
    }

    [Fact]
    public void CouponToResponse_DeveMapearStatusETemplate()
    {
        // Arrange
        var coupon = new Coupon { Id = 1, Code = "A", TemplateId = 9, Status = CouponStatus.UTILIZADO };

        // Act
        var dto = coupon.ToResponse();

        // Assert
        Assert.Equal(CouponStatus.UTILIZADO, dto.Status);
        Assert.Equal(9, dto.TemplateId);
    }
}
