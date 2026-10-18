using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Application.Mappings;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Domain.Services;

namespace PetFlowAPI.Application.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private static readonly string[] SortableFields = { "Id", "StartDate", "EndDate", "Status", "CreatedAt" };

    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Plan> _plans;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IRepository<Subscription> subscriptions, IRepository<Pet> pets, IRepository<Plan> plans,
        IUnitOfWork uow, ICurrentUser user, ILogger<SubscriptionService> logger)
    {
        _subscriptions = subscriptions;
        _pets = pets;
        _plans = plans;
        _uow = uow;
        _user = user;
        _logger = logger;
    }

    public async Task<SubscriptionResponseDTO> CreateAsync(SubscriptionRequestDTO request, CancellationToken ct = default)
    {
        await ValidateReferencesAsync(request, ct);

        var subscription = new Subscription
        {
            PetId = request.PetId,
            PlanId = request.PlanId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };
        subscription.EnsureValidPeriod();

        await _subscriptions.AddAsync(subscription, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Assinatura {SubscriptionId} criada para o pet {PetId}", subscription.Id, subscription.PetId);
        return subscription.ToResponse();
    }

    public async Task<PagedResult<SubscriptionResponseDTO>> GetPagedAsync(SubscriptionFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Subscription, bool>>? predicate = null;
        var scoped = _user.ScopedTutorId();
        if (scoped.HasValue) predicate = predicate.AndAlso(s => s.Pet!.TutorId == scoped.Value);
        if (filter.PetId.HasValue) predicate = predicate.AndAlso(s => s.PetId == filter.PetId.Value);
        if (filter.PlanId.HasValue) predicate = predicate.AndAlso(s => s.PlanId == filter.PlanId.Value);
        if (filter.Status.HasValue) predicate = predicate.AndAlso(s => s.Status == filter.Status.Value);

        var page = paging.ToPageRequest(SortableFields, "Id");
        return (await _subscriptions.GetPagedAsync(predicate, page, ct)).Map(s => s.ToResponse());
    }

    public async Task<SubscriptionResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAuthorizedAsync(id, ct)).ToResponse();

    public async Task<SubscriptionResponseDTO> UpdateAsync(long id, SubscriptionRequestDTO request, CancellationToken ct = default)
    {
        var subscription = await GetAuthorizedAsync(id, ct);
        await ValidateReferencesAsync(request, ct);

        subscription.PetId = request.PetId;
        subscription.PlanId = request.PlanId;
        subscription.StartDate = request.StartDate;
        subscription.EndDate = request.EndDate;
        subscription.EnsureValidPeriod();
        subscription.ChangeStatus(request.Status);

        _subscriptions.Update(subscription);
        await _uow.SaveChangesAsync(ct);
        return subscription.ToResponse();
    }

    public async Task<SubscriptionResponseDTO> UpdateStatusAsync(long id, SubscriptionStatus status, CancellationToken ct = default)
    {
        var subscription = await GetAuthorizedAsync(id, ct);
        subscription.ChangeStatus(status);
        _subscriptions.Update(subscription);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Assinatura {SubscriptionId} alterada para {Status}", id, status);
        return subscription.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var subscription = await GetAuthorizedAsync(id, ct);
        _subscriptions.Remove(subscription);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<Subscription> GetAuthorizedAsync(long id, CancellationToken ct)
    {
        var subscription = await _subscriptions.GetByIdAsync(id, ct) ?? throw new NotFoundException("Assinatura", id);
        var pet = await _pets.GetByIdAsync(subscription.PetId, ct) ?? throw new NotFoundException("Pet", subscription.PetId);
        _user.EnsureAccessToTutor(pet.TutorId);
        return subscription;
    }

    private async Task ValidateReferencesAsync(SubscriptionRequestDTO request, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(request.PetId, ct) ?? throw new NotFoundException("Pet", request.PetId);
        _user.EnsureAccessToTutor(pet.TutorId);

        if (!await _plans.ExistsAsync(p => p.Id == request.PlanId, ct))
            throw new NotFoundException("Plano", request.PlanId);
    }
}

public sealed class HealthEventService : IHealthEventService
{
    private static readonly string[] SortableFields = { "Id", "EventDate", "Status", "CreatedAt" };

    private readonly IRepository<HealthEvent> _events;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<EventType> _eventTypes;
    private readonly IRepository<Clinic> _clinics;
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<RewardAction> _rewardActions;
    private readonly IRepository<RewardPoint> _rewardPoints;
    private readonly IRewardPointCalculator _calculator;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly ILogger<HealthEventService> _logger;

    public HealthEventService(IRepository<HealthEvent> events, IRepository<Pet> pets, IRepository<EventType> eventTypes,
        IRepository<Clinic> clinics, IRepository<Subscription> subscriptions, IRepository<Plan> plans,
        IRepository<RewardAction> rewardActions, IRepository<RewardPoint> rewardPoints,
        IRewardPointCalculator calculator, IUnitOfWork uow, ICurrentUser user, ILogger<HealthEventService> logger)
    {
        _events = events;
        _pets = pets;
        _eventTypes = eventTypes;
        _clinics = clinics;
        _subscriptions = subscriptions;
        _plans = plans;
        _rewardActions = rewardActions;
        _rewardPoints = rewardPoints;
        _calculator = calculator;
        _uow = uow;
        _user = user;
        _logger = logger;
    }

    public async Task<HealthEventResponseDTO> CreateAsync(HealthEventRequestDTO request, CancellationToken ct = default)
    {
        var pet = await ValidateReferencesAsync(request, ct);

        var healthEvent = new HealthEvent
        {
            PetId = request.PetId,
            EventTypeId = request.EventTypeId,
            ClinicId = request.ClinicId,
            Description = request.Description,
            EventDate = request.EventDate,
            Status = request.Status
        };

        await _events.AddAsync(healthEvent, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Evento de saúde {HealthEventId} registrado para o pet {PetId}", healthEvent.Id, pet.Id);

        var response = healthEvent.ToResponse();
        if (healthEvent.WasJustCompleted(HealthEventStatus.AGENDADO))
            response.PointsAwarded = await AwardPointsAsync(healthEvent, pet, ct);
        return response;
    }

    public async Task<PagedResult<HealthEventResponseDTO>> GetPagedAsync(HealthEventFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<HealthEvent, bool>>? predicate = null;
        var scoped = _user.ScopedTutorId();
        if (scoped.HasValue) predicate = predicate.AndAlso(e => e.Pet!.TutorId == scoped.Value);
        if (filter.PetId.HasValue) predicate = predicate.AndAlso(e => e.PetId == filter.PetId.Value);
        if (filter.Status.HasValue) predicate = predicate.AndAlso(e => e.Status == filter.Status.Value);
        if (filter.From.HasValue) predicate = predicate.AndAlso(e => e.EventDate >= filter.From.Value);
        if (filter.To.HasValue) predicate = predicate.AndAlso(e => e.EventDate <= filter.To.Value);

        var page = paging.ToPageRequest(SortableFields, "EventDate");
        return (await _events.GetPagedAsync(predicate, page, ct)).Map(e => e.ToResponse());
    }

    public async Task<HealthEventResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAuthorizedAsync(id, ct)).Event.ToResponse();

    public async Task<HealthEventResponseDTO> UpdateAsync(long id, HealthEventRequestDTO request, CancellationToken ct = default)
    {
        var (healthEvent, _) = await GetAuthorizedAsync(id, ct);
        var pet = await ValidateReferencesAsync(request, ct);

        var previousStatus = healthEvent.Status;
        healthEvent.PetId = request.PetId;
        healthEvent.EventTypeId = request.EventTypeId;
        healthEvent.ClinicId = request.ClinicId;
        healthEvent.Description = request.Description;
        healthEvent.EventDate = request.EventDate;
        healthEvent.ChangeStatus(request.Status);

        _events.Update(healthEvent);
        await _uow.SaveChangesAsync(ct);

        var response = healthEvent.ToResponse();
        if (healthEvent.WasJustCompleted(previousStatus))
            response.PointsAwarded = await AwardPointsAsync(healthEvent, pet, ct);
        return response;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var (healthEvent, _) = await GetAuthorizedAsync(id, ct);
        _events.Remove(healthEvent);
        await _uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Concede ao tutor os pontos do evento concluído: pontos base do tipo de evento × multiplicador
    /// do plano ativo do pet (1 quando não há assinatura ativa). Idempotente por evento.
    /// </summary>
    private async Task<int?> AwardPointsAsync(HealthEvent healthEvent, Pet pet, CancellationToken ct)
    {
        if (await _rewardPoints.ExistsAsync(p => p.ReferenceType == RewardPoint.HealthEventReference && p.ReferenceId == healthEvent.Id, ct))
            return null;

        var action = await _rewardActions.FirstOrDefaultAsync(a => a.Name == RewardAction.HealthEventActionName, ct);
        if (action is null)
        {
            _logger.LogWarning("Ação de recompensa '{ActionName}' não cadastrada; pontos do evento {HealthEventId} não concedidos",
                RewardAction.HealthEventActionName, healthEvent.Id);
            return null;
        }

        var eventType = await _eventTypes.GetByIdAsync(healthEvent.EventTypeId, ct);
        if (eventType is null) return null;

        var multiplier = 1;
        var subscription = await _subscriptions.FirstOrDefaultAsync(s => s.PetId == pet.Id && s.Status == SubscriptionStatus.ATIVO, ct);
        if (subscription is not null)
        {
            var plan = await _plans.GetByIdAsync(subscription.PlanId, ct);
            if (plan is not null) multiplier = plan.PointsPerEvent;
        }

        var points = _calculator.Calculate(eventType.PointsReward, multiplier);
        await _rewardPoints.AddAsync(new RewardPoint
        {
            TutorId = pet.TutorId,
            RewardActionId = action.Id,
            Points = points,
            ReferenceType = RewardPoint.HealthEventReference,
            ReferenceId = healthEvent.Id
        }, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("{Points} pontos concedidos ao tutor {TutorId} pelo evento {HealthEventId}", points, pet.TutorId, healthEvent.Id);
        return points;
    }

    private async Task<(HealthEvent Event, Pet Pet)> GetAuthorizedAsync(long id, CancellationToken ct)
    {
        var healthEvent = await _events.GetByIdAsync(id, ct) ?? throw new NotFoundException("Evento de saúde", id);
        var pet = await _pets.GetByIdAsync(healthEvent.PetId, ct) ?? throw new NotFoundException("Pet", healthEvent.PetId);
        _user.EnsureAccessToTutor(pet.TutorId);
        return (healthEvent, pet);
    }

    private async Task<Pet> ValidateReferencesAsync(HealthEventRequestDTO request, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(request.PetId, ct) ?? throw new NotFoundException("Pet", request.PetId);
        _user.EnsureAccessToTutor(pet.TutorId);

        if (!await _eventTypes.ExistsAsync(t => t.Id == request.EventTypeId, ct))
            throw new NotFoundException("Tipo de evento", request.EventTypeId);

        if (request.ClinicId.HasValue && !await _clinics.ExistsAsync(c => c.Id == request.ClinicId.Value, ct))
            throw new NotFoundException("Clínica", request.ClinicId.Value);

        return pet;
    }
}
