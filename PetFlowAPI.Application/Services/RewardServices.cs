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

namespace PetFlowAPI.Application.Services;

public sealed class CouponService : ICouponService
{
    private static readonly string[] SortableFields = { "Id", "Code", "Status", "ExpirationDate", "CreatedAt" };

    private readonly ICouponRepository _coupons;
    private readonly IRepository<CouponTemplate> _templates;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly ILogger<CouponService> _logger;

    public CouponService(ICouponRepository coupons, IRepository<CouponTemplate> templates, IUnitOfWork uow,
        TimeProvider time, ILogger<CouponService> logger)
    {
        _coupons = coupons;
        _templates = templates;
        _uow = uow;
        _time = time;
        _logger = logger;
    }

    public async Task<CouponResponseDTO> CreateAsync(CouponRequestDTO request, CancellationToken ct = default)
    {
        await ValidateAsync(request, null, ct);

        var coupon = new Coupon
        {
            TemplateId = request.TemplateId,
            Code = request.Code.Trim(),
            Status = request.Status,
            ExpirationDate = request.ExpirationDate
        };

        await _coupons.AddAsync(coupon, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Cupom {CouponId} criado", coupon.Id);
        return coupon.ToResponse();
    }

    public async Task<PagedResult<CouponResponseDTO>> GetPagedAsync(CouponFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Coupon, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(filter.Code)) predicate = predicate.AndAlso(c => c.Code.Contains(filter.Code));
        if (filter.Status.HasValue) predicate = predicate.AndAlso(c => c.Status == filter.Status.Value);

        var page = paging.ToPageRequest(SortableFields, "Id");
        return (await _coupons.GetPagedAsync(predicate, page, ct)).Map(c => c.ToResponse());
    }

    public async Task<CouponResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAsync(id, ct)).ToResponse();

    public async Task<CouponResponseDTO> UpdateAsync(long id, CouponRequestDTO request, CancellationToken ct = default)
    {
        var coupon = await GetAsync(id, ct);
        await ValidateAsync(request, id, ct);

        coupon.TemplateId = request.TemplateId;
        coupon.Code = request.Code.Trim();
        coupon.Status = request.Status;
        coupon.ExpirationDate = request.ExpirationDate;

        _coupons.Update(coupon);
        await _uow.SaveChangesAsync(ct);
        return coupon.ToResponse();
    }

    public async Task<CouponResponseDTO> UpdateStatusAsync(long id, CouponStatus status, CancellationToken ct = default)
    {
        var coupon = await GetAsync(id, ct);
        coupon.Status = status;
        _coupons.Update(coupon);
        await _uow.SaveChangesAsync(ct);
        return coupon.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var coupon = await GetAsync(id, ct);
        await _coupons.RemoveWithDependentsAsync(coupon, ct);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<Coupon> GetAsync(long id, CancellationToken ct)
        => await _coupons.GetByIdAsync(id, ct) ?? throw new NotFoundException("Cupom", id);

    private async Task ValidateAsync(CouponRequestDTO request, long? currentId, CancellationToken ct)
    {
        var today = _time.GetUtcNow().UtcDateTime.Date;
        if (request.ExpirationDate.HasValue && request.ExpirationDate.Value.Date < today)
            throw new RequestValidationException(nameof(request.ExpirationDate), "A data de expiração não pode ser uma data no passado.");

        if (!await _templates.ExistsAsync(t => t.Id == request.TemplateId, ct))
            throw new NotFoundException("Modelo de cupom", request.TemplateId);

        var code = request.Code.Trim();
        if (await _coupons.ExistsAsync(c => c.Code == code && c.Id != (currentId ?? 0), ct))
            throw new ConflictException($"Já existe um cupom com o código {code}.");
    }
}

public sealed class RedeemService : IRedeemService
{
    private static readonly string[] SortableFields = { "Id", "PointsUsed", "CreatedAt" };

    private readonly IRepository<Redeem> _redeems;
    private readonly IRepository<Tutor> _tutors;
    private readonly ICouponRepository _coupons;
    private readonly IRepository<CouponTemplate> _templates;
    private readonly IRewardRepository _rewards;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;
    private readonly ILogger<RedeemService> _logger;

    public RedeemService(IRepository<Redeem> redeems, IRepository<Tutor> tutors, ICouponRepository coupons,
        IRepository<CouponTemplate> templates, IRewardRepository rewards, IUnitOfWork uow, ICurrentUser user,
        TimeProvider time, ILogger<RedeemService> logger)
    {
        _redeems = redeems;
        _tutors = tutors;
        _coupons = coupons;
        _templates = templates;
        _rewards = rewards;
        _uow = uow;
        _user = user;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// Resgata um cupom: valida disponibilidade/validade, confere o saldo de pontos do tutor,
    /// debita os pontos exigidos pelo modelo do cupom e marca o cupom como RESGATADO.
    /// </summary>
    public async Task<RedeemResponseDTO> CreateAsync(RedeemRequestDTO request, CancellationToken ct = default)
    {
        _user.EnsureAccessToTutor(request.TutorId);

        if (!await _tutors.ExistsAsync(t => t.Id == request.TutorId, ct))
            throw new NotFoundException("Tutor", request.TutorId);

        var coupon = await _coupons.GetByIdAsync(request.CouponId, ct) ?? throw new NotFoundException("Cupom", request.CouponId);
        var template = await _templates.GetByIdAsync(coupon.TemplateId, ct) ?? throw new NotFoundException("Modelo de cupom", coupon.TemplateId);

        coupon.MarkAsRedeemed(_time.GetUtcNow().UtcDateTime);

        var summary = await _rewards.GetPointsSummaryAsync(request.TutorId, ct);
        if (summary.Balance < template.PointsRequired)
            throw new BusinessRuleException(
                $"Saldo de pontos insuficiente: o cupom exige {template.PointsRequired} pontos e o tutor possui {summary.Balance}.");

        var redeem = new Redeem { TutorId = request.TutorId, CouponId = coupon.Id, PointsUsed = template.PointsRequired };
        await _redeems.AddAsync(redeem, ct);
        _coupons.Update(coupon);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Tutor {TutorId} resgatou o cupom {CouponId} usando {Points} pontos", request.TutorId, coupon.Id, redeem.PointsUsed);
        return redeem.ToResponse();
    }

    public async Task<PagedResult<RedeemResponseDTO>> GetPagedAsync(RedeemFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Redeem, bool>>? predicate = null;
        var scoped = _user.ScopedTutorId();
        if (scoped.HasValue) predicate = predicate.AndAlso(r => r.TutorId == scoped.Value);
        if (filter.TutorId.HasValue) predicate = predicate.AndAlso(r => r.TutorId == filter.TutorId.Value);
        if (filter.CouponId.HasValue) predicate = predicate.AndAlso(r => r.CouponId == filter.CouponId.Value);

        var page = paging.ToPageRequest(SortableFields, "CreatedAt");
        return (await _redeems.GetPagedAsync(predicate, page, ct)).Map(r => r.ToResponse());
    }

    public async Task<RedeemResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var redeem = await _redeems.GetByIdAsync(id, ct) ?? throw new NotFoundException("Resgate", id);
        _user.EnsureAccessToTutor(redeem.TutorId);
        return redeem.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var redeem = await _redeems.GetByIdAsync(id, ct) ?? throw new NotFoundException("Resgate", id);
        _redeems.Remove(redeem);
        await _uow.SaveChangesAsync(ct);
    }
}
