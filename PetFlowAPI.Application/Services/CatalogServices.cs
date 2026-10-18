using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Application.Mappings;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Application.Services;

public sealed class ClinicService : IClinicService
{
    private static readonly string[] SortableFields = { "Id", "Name", "CreatedAt" };

    private readonly IClinicRepository _clinics;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(IClinicRepository clinics, IUnitOfWork uow, ILogger<ClinicService> logger)
    {
        _clinics = clinics;
        _uow = uow;
        _logger = logger;
    }

    public async Task<ClinicResponseDTO> CreateAsync(ClinicRequestDTO request, CancellationToken ct = default)
    {
        await EnsureCnpjAvailableAsync(request.Cnpj, null, ct);

        var clinic = new Clinic();
        clinic.Apply(request);
        await _clinics.AddAsync(clinic, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Clínica {ClinicId} cadastrada", clinic.Id);
        return clinic.ToResponse();
    }

    public async Task<PagedResult<ClinicResponseDTO>> GetPagedAsync(ClinicFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Clinic, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(filter.Name)) predicate = predicate.AndAlso(c => c.Name.Contains(filter.Name));

        var page = paging.ToPageRequest(SortableFields, "Id");
        return (await _clinics.GetPagedAsync(predicate, page, ct)).Map(c => c.ToResponse());
    }

    public async Task<ClinicResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAsync(id, ct)).ToResponse();

    public async Task<ClinicResponseDTO> UpdateAsync(long id, ClinicRequestDTO request, CancellationToken ct = default)
    {
        var clinic = await GetAsync(id, ct);
        await EnsureCnpjAvailableAsync(request.Cnpj, id, ct);

        clinic.Apply(request);
        _clinics.Update(clinic);
        await _uow.SaveChangesAsync(ct);
        return clinic.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var clinic = await GetAsync(id, ct);
        await _clinics.RemoveWithDependentsAsync(clinic, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Clínica {ClinicId} removida", id);
    }

    private async Task<Clinic> GetAsync(long id, CancellationToken ct)
        => await _clinics.GetByIdAsync(id, ct) ?? throw new NotFoundException("Clínica", id);

    private async Task EnsureCnpjAvailableAsync(string? cnpj, long? currentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return;
        if (await _clinics.ExistsAsync(c => c.Cnpj == cnpj && c.Id != (currentId ?? 0), ct))
            throw new ConflictException($"Já existe uma clínica cadastrada com o CNPJ {cnpj}.");
    }
}

public sealed class PlanService : IPlanService
{
    private static readonly string[] SortableFields = { "Id", "Name", "Price", "DurationDays", "PointsPerEvent" };

    private readonly IRepository<Plan> _plans;
    private readonly IRepository<Clinic> _clinics;
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PlanService> _logger;

    public PlanService(IRepository<Plan> plans, IRepository<Clinic> clinics, IRepository<Subscription> subscriptions,
        IUnitOfWork uow, ILogger<PlanService> logger)
    {
        _plans = plans;
        _clinics = clinics;
        _subscriptions = subscriptions;
        _uow = uow;
        _logger = logger;
    }

    public async Task<PlanResponseDTO> CreateAsync(PlanRequestDTO request, CancellationToken ct = default)
    {
        await EnsureClinicExistsAsync(request.ClinicId, ct);

        var plan = new Plan();
        plan.Apply(request);
        await _plans.AddAsync(plan, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Plano {PlanId} cadastrado", plan.Id);
        return plan.ToResponse();
    }

    public async Task<PagedResult<PlanResponseDTO>> GetPagedAsync(PlanFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Plan, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(filter.Name)) predicate = predicate.AndAlso(p => p.Name.Contains(filter.Name));
        if (filter.ClinicId.HasValue) predicate = predicate.AndAlso(p => p.ClinicId == filter.ClinicId.Value);

        var page = paging.ToPageRequest(SortableFields, "Id");
        return (await _plans.GetPagedAsync(predicate, page, ct)).Map(p => p.ToResponse());
    }

    public async Task<PlanResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAsync(id, ct)).ToResponse();

    public async Task<PlanResponseDTO> UpdateAsync(long id, PlanRequestDTO request, CancellationToken ct = default)
    {
        var plan = await GetAsync(id, ct);
        await EnsureClinicExistsAsync(request.ClinicId, ct);

        plan.Apply(request);
        _plans.Update(plan);
        await _uow.SaveChangesAsync(ct);
        return plan.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var plan = await GetAsync(id, ct);
        if (await _subscriptions.ExistsAsync(s => s.PlanId == id, ct))
            throw new ConflictException("O plano possui assinaturas vinculadas e não pode ser removido.");

        _plans.Remove(plan);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<Plan> GetAsync(long id, CancellationToken ct)
        => await _plans.GetByIdAsync(id, ct) ?? throw new NotFoundException("Plano", id);

    private async Task EnsureClinicExistsAsync(long clinicId, CancellationToken ct)
    {
        if (!await _clinics.ExistsAsync(c => c.Id == clinicId, ct))
            throw new NotFoundException("Clínica", clinicId);
    }
}
