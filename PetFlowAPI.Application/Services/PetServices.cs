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

public sealed class PetService : IPetService
{
    private static readonly string[] SortableFields = { "Id", "Name", "Breed", "BirthDate", "Weight", "CreatedAt" };

    private readonly IPetRepository _pets;
    private readonly IRepository<Tutor> _tutors;
    private readonly IRepository<Species> _species;
    private readonly IPetNoteRepository _notes;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;
    private readonly ILogger<PetService> _logger;

    public PetService(IPetRepository pets, IRepository<Tutor> tutors, IRepository<Species> species,
        IPetNoteRepository notes, IUnitOfWork uow, ICurrentUser user, ILogger<PetService> logger)
    {
        _pets = pets;
        _tutors = tutors;
        _species = species;
        _notes = notes;
        _uow = uow;
        _user = user;
        _logger = logger;
    }

    public async Task<PetResponseDTO> CreateAsync(PetRequestDTO request, CancellationToken ct = default)
    {
        await ValidateReferencesAsync(request, ct);

        var pet = new Pet();
        pet.Apply(request);
        await _pets.AddAsync(pet, ct);
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Pet {PetId} cadastrado para o tutor {TutorId}", pet.Id, pet.TutorId);
        return pet.ToResponse();
    }

    public async Task<PagedResult<PetResponseDTO>> GetPagedAsync(PetFilter filter, PagingParameters paging, CancellationToken ct = default)
    {
        Expression<Func<Pet, bool>>? predicate = null;
        var scoped = _user.ScopedTutorId();
        if (scoped.HasValue) predicate = predicate.AndAlso(p => p.TutorId == scoped.Value);
        if (!string.IsNullOrWhiteSpace(filter.Name)) predicate = predicate.AndAlso(p => p.Name.Contains(filter.Name));
        if (filter.TutorId.HasValue) predicate = predicate.AndAlso(p => p.TutorId == filter.TutorId.Value);
        if (filter.SpeciesId.HasValue) predicate = predicate.AndAlso(p => p.SpeciesId == filter.SpeciesId.Value);

        var page = paging.ToPageRequest(SortableFields, "Id");
        return (await _pets.GetPagedAsync(predicate, page, ct)).Map(p => p.ToResponse());
    }

    public async Task<PetResponseDTO> GetByIdAsync(long id, CancellationToken ct = default)
        => (await GetAuthorizedAsync(id, ct)).ToResponse();

    public async Task<PetResponseDTO> UpdateAsync(long id, PetRequestDTO request, CancellationToken ct = default)
    {
        var pet = await GetAuthorizedAsync(id, ct);
        await ValidateReferencesAsync(request, ct);

        pet.Apply(request);
        _pets.Update(pet);
        await _uow.SaveChangesAsync(ct);
        return pet.ToResponse();
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var pet = await GetAuthorizedAsync(id, ct);
        await _pets.RemoveWithDependentsAsync(pet, ct);
        await _uow.SaveChangesAsync(ct);
        await _notes.DeleteByPetAsync(id, ct);
        _logger.LogInformation("Pet {PetId} removido", id);
    }

    private async Task<Pet> GetAuthorizedAsync(long id, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Pet", id);
        _user.EnsureAccessToTutor(pet.TutorId);
        return pet;
    }

    private async Task ValidateReferencesAsync(PetRequestDTO request, CancellationToken ct)
    {
        _user.EnsureAccessToTutor(request.TutorId);

        if (!await _tutors.ExistsAsync(t => t.Id == request.TutorId, ct))
            throw new NotFoundException("Tutor", request.TutorId);

        if (!await _species.ExistsAsync(s => s.Id == request.SpeciesId, ct))
            throw new NotFoundException("Espécie", request.SpeciesId);

        if (request.BirthDate.HasValue && request.BirthDate.Value.Date > DateTime.UtcNow.Date)
            throw new RequestValidationException(nameof(request.BirthDate), "A data de nascimento não pode estar no futuro.");
    }
}

public sealed class PetNoteService : IPetNoteService
{
    private static readonly string[] SortableFields = { "CreatedAt", "Title" };

    private readonly IPetNoteRepository _notes;
    private readonly IRepository<Pet> _pets;
    private readonly ICurrentUser _user;
    private readonly ILogger<PetNoteService> _logger;

    public PetNoteService(IPetNoteRepository notes, IRepository<Pet> pets, ICurrentUser user, ILogger<PetNoteService> logger)
    {
        _notes = notes;
        _pets = pets;
        _user = user;
        _logger = logger;
    }

    public async Task<PetNoteResponseDTO> CreateAsync(long petId, PetNoteRequestDTO request, CancellationToken ct = default)
    {
        await EnsurePetAccessAsync(petId, ct);

        var note = new PetNote
        {
            PetId = petId,
            AuthorTutorId = _user.UserId ?? throw new ForbiddenException("Usuário não identificado."),
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList(),
            CreatedAt = DateTime.UtcNow
        };

        await _notes.AddAsync(note, ct);
        _logger.LogInformation("Anotação {NoteId} registrada para o pet {PetId}", note.Id, petId);
        return note.ToResponse();
    }

    public async Task<PagedResult<PetNoteResponseDTO>> GetByPetAsync(long petId, string? tag, PagingParameters paging, CancellationToken ct = default)
    {
        await EnsurePetAccessAsync(petId, ct);
        var page = paging.ToPageRequest(SortableFields, "CreatedAt");
        var normalizedTag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToLowerInvariant();
        return (await _notes.GetByPetAsync(petId, normalizedTag, page, ct)).Map(n => n.ToResponse());
    }

    public async Task<PetNoteResponseDTO> GetByIdAsync(long petId, string noteId, CancellationToken ct = default)
    {
        await EnsurePetAccessAsync(petId, ct);
        return (await GetNoteAsync(petId, noteId, ct)).ToResponse();
    }

    public async Task DeleteAsync(long petId, string noteId, CancellationToken ct = default)
    {
        await EnsurePetAccessAsync(petId, ct);
        await GetNoteAsync(petId, noteId, ct);
        await _notes.DeleteAsync(noteId, ct);
    }

    private async Task<PetNote> GetNoteAsync(long petId, string noteId, CancellationToken ct)
    {
        var note = await _notes.GetByIdAsync(noteId, ct);
        return note is null || note.PetId != petId ? throw new NotFoundException("Anotação", noteId) : note;
    }

    private async Task EnsurePetAccessAsync(long petId, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(petId, ct) ?? throw new NotFoundException("Pet", petId);
        _user.EnsureAccessToTutor(pet.TutorId);
    }
}
