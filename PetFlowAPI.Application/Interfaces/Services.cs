using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request, CancellationToken ct = default);
}

public interface ITutorService
{
    Task<TutorResponseDTO> CreateAsync(TutorRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<TutorResponseDTO>> GetPagedAsync(TutorFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<TutorResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<TutorResponseDTO> UpdateAsync(long id, TutorUpdateRequestDTO request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<PointsBalanceDTO> GetPointsBalanceAsync(long id, CancellationToken ct = default);
}

public interface IPetService
{
    Task<PetResponseDTO> CreateAsync(PetRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<PetResponseDTO>> GetPagedAsync(PetFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<PetResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PetResponseDTO> UpdateAsync(long id, PetRequestDTO request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IPetNoteService
{
    Task<PetNoteResponseDTO> CreateAsync(long petId, PetNoteRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<PetNoteResponseDTO>> GetByPetAsync(long petId, string? tag, PagingParameters paging, CancellationToken ct = default);
    Task<PetNoteResponseDTO> GetByIdAsync(long petId, string noteId, CancellationToken ct = default);
    Task DeleteAsync(long petId, string noteId, CancellationToken ct = default);
}

public interface IClinicService
{
    Task<ClinicResponseDTO> CreateAsync(ClinicRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<ClinicResponseDTO>> GetPagedAsync(ClinicFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<ClinicResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ClinicResponseDTO> UpdateAsync(long id, ClinicRequestDTO request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IPlanService
{
    Task<PlanResponseDTO> CreateAsync(PlanRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<PlanResponseDTO>> GetPagedAsync(PlanFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<PlanResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PlanResponseDTO> UpdateAsync(long id, PlanRequestDTO request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ISubscriptionService
{
    Task<SubscriptionResponseDTO> CreateAsync(SubscriptionRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<SubscriptionResponseDTO>> GetPagedAsync(SubscriptionFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<SubscriptionResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SubscriptionResponseDTO> UpdateAsync(long id, SubscriptionRequestDTO request, CancellationToken ct = default);
    Task<SubscriptionResponseDTO> UpdateStatusAsync(long id, SubscriptionStatus status, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IHealthEventService
{
    Task<HealthEventResponseDTO> CreateAsync(HealthEventRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<HealthEventResponseDTO>> GetPagedAsync(HealthEventFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<HealthEventResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<HealthEventResponseDTO> UpdateAsync(long id, HealthEventRequestDTO request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ICouponService
{
    Task<CouponResponseDTO> CreateAsync(CouponRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<CouponResponseDTO>> GetPagedAsync(CouponFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<CouponResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CouponResponseDTO> UpdateAsync(long id, CouponRequestDTO request, CancellationToken ct = default);
    Task<CouponResponseDTO> UpdateStatusAsync(long id, CouponStatus status, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IRedeemService
{
    Task<RedeemResponseDTO> CreateAsync(RedeemRequestDTO request, CancellationToken ct = default);
    Task<PagedResult<RedeemResponseDTO>> GetPagedAsync(RedeemFilter filter, PagingParameters paging, CancellationToken ct = default);
    Task<RedeemResponseDTO> GetByIdAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
