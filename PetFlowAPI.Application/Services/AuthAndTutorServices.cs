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

public sealed class AuthService : IAuthService
{
    private readonly ITutorRepository _tutors;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ITutorRepository tutors, IPasswordHasher hasher, ITokenService tokens, ILogger<AuthService> logger)
    {
        _tutors = tutors;
        _hasher = hasher;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request, CancellationToken ct = default)
    {
        var tutor = await _tutors.GetByEmailAsync(request.Email.Trim(), ct);
        if (tutor is null || !_hasher.Verify(request.Password, tutor.PasswordHash))
        {
            _logger.LogWarning("Falha de login para o e-mail informado");
            throw new InvalidCredentialsException();
        }

        var token = _tokens.CreateToken(tutor);
        _logger.LogInformation("Login realizado com sucesso para o tutor {TutorId}", tutor.Id);
        return new LoginResponseDTO(token.Token, token.ExpiresAt, tutor.Id, tutor.Name, tutor.Email, tutor.Role);
    }
}

public sealed class TutorService : ITutorService
{
    private static readonly string[] SortableFields = { "Id", "Name", "Email", "CreatedAt" };

    private readonly ITutorRepository _tutors;
    private readonly IRewardRepository _rewards;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly ILogger<TutorService> _logger;

    public TutorService(ITutorRepository tutors, IRewardRepository rewards, IPasswordHasher hasher,
        IUnitOfWork uow, ICurrentUser user, ILogger<TutorService> logger)
    {
        _tutors = tutors;
        _rewards = rewards;
        _hasher = hasher;
        _uow = uow;
        _user = user;
        _logger = logger;
    }

    public async Task<TutorResponseDTO> CreateAsync(TutorRequestDTO request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        if (await _tutors.GetByEmailAsync(email, ct) is not null)
            throw new ConflictException($"Já existe um tutor cadastrado com o e-mail {email}.");

        var tutor = new Tutor
        {
            Name = request.Name,
            Email = email,
            Phone = request.Phone,
            Role = Domain.Enums.UserRoles.Tutor,
            PasswordHash = _hasher.Hash(request.Password)
        };

        await _tutors.AddAsync(tutor, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Tutor {TutorId} cadastrado", tutor.Id);
        return tutor.ToResponse();
    }

    public async Task<PagedResult<TutorResponseDTO>> GetPagedAsync(TutorFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Tutor, bool>>? predicate = null;
        var scoped = _user.ScopedTutorId();
        if (scoped.HasValue) predicate = predicate.AndAlso(t => t.Id == scoped.Value);
        if (!string.IsNullOrWhiteSpace(filter.Name)) predicate = predicate.AndAlso(t => t.Name.Contains(filter.Name));
        if (!string.IsNullOrWhiteSpace(filter.Email)) predicate = predicate.AndAlso(t => t.Email.Contains(filter.Email));

        var page = paging.ToPageRequest(SortableFields, "Id");
        return (await _tutors.GetPagedAsync(predicate, page, ct)).Map(t => t.ToResponse());
    }

    public async Task<TutorResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAuthorizedAsync(id, ct)).ToResponse();

    public async Task<TutorResponseDTO> UpdateAsync(long id, TutorUpdateRequestDTO request, CancellationToken ct = default)
    {
        var tutor = await GetAuthorizedAsync(id, ct);

        var email = request.Email.Trim();
        var other = await _tutors.GetByEmailAsync(email, ct);
        if (other is not null && other.Id != id)
            throw new ConflictException($"Já existe um tutor cadastrado com o e-mail {email}.");

        tutor.Name = request.Name;
        tutor.Email = email;
        tutor.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.Password))
            tutor.PasswordHash = _hasher.Hash(request.Password);

        _tutors.Update(tutor);
        await _uow.SaveChangesAsync(ct);
        return tutor.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var tutor = await GetAuthorizedAsync(id, ct);
        await _tutors.RemoveWithDependentsAsync(tutor, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Tutor {TutorId} removido", id);
    }

    public async Task<PointsBalanceDTO> GetPointsBalanceAsync(long id, CancellationToken ct = default)
    {
        await GetAuthorizedAsync(id, ct);
        var summary = await _rewards.GetPointsSummaryAsync(id, ct);
        return new PointsBalanceDTO(id, summary.Earned, summary.Redeemed, summary.Balance);
    }

    private async Task<Tutor> GetAuthorizedAsync(long id, CancellationToken ct)
    {
        _user.EnsureAccessToTutor(id);
        return await _tutors.GetByIdAsync(id, ct) ?? throw new NotFoundException("Tutor", id);
    }
}
