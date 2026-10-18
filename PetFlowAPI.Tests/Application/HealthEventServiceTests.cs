using System.Linq.Expressions;
using Moq;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Domain.Services;

namespace PetFlowAPI.Tests.Application;

public class HealthEventServiceTests
{
    private readonly Mock<IRepository<HealthEvent>> _events = new();
    private readonly Mock<IRepository<Pet>> _pets = new();
    private readonly Mock<IRepository<EventType>> _eventTypes = new();
    private readonly Mock<IRepository<Clinic>> _clinics = Mocks.RepositoryExists<Clinic>(true);
    private readonly Mock<IRepository<Subscription>> _subscriptions = new();
    private readonly Mock<IRepository<Plan>> _plans = new();
    private readonly Mock<IRepository<RewardAction>> _rewardActions = new();
    private readonly Mock<IRepository<RewardPoint>> _rewardPoints = new();
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();
    private RewardPoint? _awarded;

    public HealthEventServiceTests()
    {
        _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { Id = 1, TutorId = 7 });
        _eventTypes.Setup(t => t.ExistsAsync(It.IsAny<Expression<Func<EventType, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _eventTypes.Setup(t => t.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new EventType { Id = 3, PointsReward = 45 });
        _rewardActions.Setup(a => a.FirstOrDefaultAsync(It.IsAny<Expression<Func<RewardAction, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RewardAction { Id = 20, Name = RewardAction.HealthEventActionName });
        _rewardPoints.Setup(r => r.AddAsync(It.IsAny<RewardPoint>(), It.IsAny<CancellationToken>()))
            .Callback<RewardPoint, CancellationToken>((p, _) => _awarded = p).Returns(Task.CompletedTask);
        _events.Setup(e => e.AddAsync(It.IsAny<HealthEvent>(), It.IsAny<CancellationToken>()))
            .Callback<HealthEvent, CancellationToken>((e, _) => e.Id = 100).Returns(Task.CompletedTask);
    }

    private HealthEventService CreateSut(FakeUser? user = null) => new(
        _events.Object, _pets.Object, _eventTypes.Object, _clinics.Object, _subscriptions.Object, _plans.Object,
        _rewardActions.Object, _rewardPoints.Object, new RewardPointCalculator(), _uow.Object,
        user ?? FakeUser.Tutor(7), Mocks.Logger<HealthEventService>());

    private static HealthEventRequestDTO Request(HealthEventStatus status) => new()
    {
        PetId = 1, EventTypeId = 3, EventDate = new DateTime(2026, 5, 1), Status = status, Description = "Vacina anual"
    };

    private void ActivePlanWithMultiplier(int multiplier)
    {
        _subscriptions.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subscription, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription { PetId = 1, PlanId = 5, Status = SubscriptionStatus.ATIVO });
        _plans.Setup(p => p.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Plan { Id = 5, PointsPerEvent = multiplier });
    }

    [Fact]
    public async Task Create_RealizadoComPlanoAtivo_DeveConcederPontosBaseVezesMultiplicador()
    {
        // Arrange
        ActivePlanWithMultiplier(2);

        // Act
        var response = await CreateSut().CreateAsync(Request(HealthEventStatus.REALIZADO));

        // Assert
        Assert.Equal(90, response.PointsAwarded);
        Assert.Equal(7, _awarded!.TutorId);
        Assert.Equal(20, _awarded.RewardActionId);
        Assert.Equal(100, _awarded.ReferenceId);
        Assert.Equal(RewardPoint.HealthEventReference, _awarded.ReferenceType);
    }

    [Fact]
    public async Task Create_RealizadoSemAssinaturaAtiva_DeveUsarMultiplicadorUm()
    {
        // Arrange — nenhuma assinatura configurada (FirstOrDefaultAsync retorna null)

        // Act
        var response = await CreateSut().CreateAsync(Request(HealthEventStatus.REALIZADO));

        // Assert
        Assert.Equal(45, response.PointsAwarded);
    }

    [Fact]
    public async Task Create_Agendado_NaoDeveConcederPontos()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var response = await sut.CreateAsync(Request(HealthEventStatus.AGENDADO));

        // Assert
        Assert.Null(response.PointsAwarded);
        Assert.Null(_awarded);
    }

    [Fact]
    public async Task Create_RealizadoSemAcaoDeRecompensaCadastrada_NaoDeveConcederPontos()
    {
        // Arrange
        _rewardActions.Setup(a => a.FirstOrDefaultAsync(It.IsAny<Expression<Func<RewardAction, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RewardAction?)null);

        // Act
        var response = await CreateSut().CreateAsync(Request(HealthEventStatus.REALIZADO));

        // Assert
        Assert.Null(response.PointsAwarded);
        Assert.Null(_awarded);
    }

    [Fact]
    public async Task Create_RealizadoComPontosJaConcedidos_NaoDeveDuplicar()
    {
        // Arrange
        _rewardPoints.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<RewardPoint, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var response = await CreateSut().CreateAsync(Request(HealthEventStatus.REALIZADO));

        // Assert
        Assert.Null(response.PointsAwarded);
        Assert.Null(_awarded);
    }

    [Fact]
    public async Task Update_AgendadoParaRealizado_DeveConcederPontos()
    {
        // Arrange
        var existing = new HealthEvent { Id = 100, PetId = 1, EventTypeId = 3, Status = HealthEventStatus.AGENDADO };
        _events.Setup(e => e.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Act
        var response = await CreateSut().UpdateAsync(100, Request(HealthEventStatus.REALIZADO));

        // Assert
        Assert.Equal(HealthEventStatus.REALIZADO, existing.Status);
        Assert.Equal(45, response.PointsAwarded);
    }

    [Fact]
    public async Task Update_RealizadoParaRealizado_NaoDeveConcederPontosNovamente()
    {
        // Arrange
        var existing = new HealthEvent { Id = 100, PetId = 1, EventTypeId = 3, Status = HealthEventStatus.REALIZADO };
        _events.Setup(e => e.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Act
        var response = await CreateSut().UpdateAsync(100, Request(HealthEventStatus.REALIZADO));

        // Assert
        Assert.Null(response.PointsAwarded);
        Assert.Null(_awarded);
    }

    [Fact]
    public async Task Update_CanceladoParaRealizado_DeveLancarBusinessRuleException()
    {
        // Arrange
        var existing = new HealthEvent { Id = 100, PetId = 1, EventTypeId = 3, Status = HealthEventStatus.CANCELADO };
        _events.Setup(e => e.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().UpdateAsync(100, Request(HealthEventStatus.REALIZADO)));

        // Assert
        Assert.IsType<BusinessRuleException>(exception);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ComPetInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var request = Request(HealthEventStatus.AGENDADO);
        request.PetId = 404;

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().CreateAsync(request));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task Create_ComPetDeOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Tutor(8));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(Request(HealthEventStatus.AGENDADO)));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task Create_ComClinicaInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var clinics = Mocks.RepositoryExists<Clinic>(false);
        var sut = new HealthEventService(_events.Object, _pets.Object, _eventTypes.Object, clinics.Object, _subscriptions.Object, _plans.Object,
            _rewardActions.Object, _rewardPoints.Object, new RewardPointCalculator(), _uow.Object, FakeUser.Tutor(7), Mocks.Logger<HealthEventService>());
        var request = Request(HealthEventStatus.AGENDADO);
        request.ClinicId = 55;

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(request));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task Delete_DeveRemoverEvento()
    {
        // Arrange
        var existing = new HealthEvent { Id = 100, PetId = 1 };
        _events.Setup(e => e.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Act
        await CreateSut().DeleteAsync(100);

        // Assert
        _events.Verify(e => e.Remove(existing), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_ComoTutorComFiltros_DeveComporPredicadoDeEscopoEStatus()
    {
        // Arrange
        Expression<Func<HealthEvent, bool>>? captured = null;
        _events.Setup(e => e.GetPagedAsync(It.IsAny<Expression<Func<HealthEvent, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<HealthEvent, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page<HealthEvent>());
        var filter = new HealthEventFilter { Status = HealthEventStatus.REALIZADO, PetId = 1 };

        // Act
        await CreateSut().GetPagedAsync(filter, new PetFlowAPI.Application.Common.PagingParameters());

        // Assert
        var predicate = captured!.Compile();
        var mine = new HealthEvent { PetId = 1, Status = HealthEventStatus.REALIZADO, Pet = new Pet { TutorId = 7 } };
        var someoneElses = new HealthEvent { PetId = 1, Status = HealthEventStatus.REALIZADO, Pet = new Pet { TutorId = 8 } };
        var wrongStatus = new HealthEvent { PetId = 1, Status = HealthEventStatus.AGENDADO, Pet = new Pet { TutorId = 7 } };
        Assert.True(predicate(mine));
        Assert.False(predicate(someoneElses));
        Assert.False(predicate(wrongStatus));
    }
}
