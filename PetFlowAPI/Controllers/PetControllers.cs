using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Hateoas;

namespace PetFlowAPI.Controllers;

[Route("pets")]
public sealed class PetController(IPetService service) : PetFlowControllerBase
{
    /// <summary>Cadastra um novo pet.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PetResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponseDTO>> Create(PetRequestDTO request, CancellationToken ct)
    {
        var pet = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
    }

    /// <summary>Lista pets com paginação, ordenação (name, breed, birthDate, weight, createdAt) e filtros por nome, tutor e espécie.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PetResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PetResponseDTO>>> GetAll([FromQuery] PetFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), p => Decorate(p)));

    /// <summary>Busca um pet pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PetResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Atualiza os dados de um pet.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(PetResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponseDTO>> Update(long id, PetRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Remove um pet e seus registros dependentes (eventos, assinaturas, scores de risco e anotações no MongoDB).</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private PetResponseDTO Decorate(PetResponseDTO pet)
    {
        pet.Links = new List<Link>
        {
            LinkTo("self", "GET", nameof(GetById), new { id = pet.Id }),
            LinkTo("update", "PUT", nameof(Update), new { id = pet.Id }),
            LinkTo("delete", "DELETE", nameof(Delete), new { id = pet.Id }),
            LinkTo("tutor", "GET", "GetById", new { id = pet.TutorId }, "Tutor"),
            LinkTo("notes", "GET", "GetAll", new { petId = pet.Id }, "PetNote"),
            LinkTo("health-events", "GET", "GetAll", new { petId = pet.Id }, "HealthEvent"),
            LinkTo("subscriptions", "GET", "GetAll", new { petId = pet.Id }, "Subscription")
        };
        return pet;
    }
}

/// <summary>Anotações clínicas dos pets, persistidas em MongoDB (NoSQL).</summary>
[Route("pets/{petId:long}/notes")]
public sealed class PetNoteController(IPetNoteService service) : PetFlowControllerBase
{
    /// <summary>Registra uma anotação clínica para o pet.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PetNoteResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetNoteResponseDTO>> Create(long petId, PetNoteRequestDTO request, CancellationToken ct)
    {
        var note = Decorate(await service.CreateAsync(petId, request, ct));
        return CreatedAtAction(nameof(GetById), new { petId, noteId = note.Id }, note);
    }

    /// <summary>Lista as anotações do pet com paginação, ordenação (createdAt, title) e filtro por tag.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PetNoteResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<PetNoteResponseDTO>>> GetAll(long petId, [FromQuery] string? tag, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetByPetAsync(petId, tag, paging, ct), n => Decorate(n)));

    /// <summary>Busca uma anotação pelo ID.</summary>
    [HttpGet("{noteId}")]
    [ProducesResponseType(typeof(PetNoteResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetNoteResponseDTO>> GetById(long petId, string noteId, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(petId, noteId, ct)));

    /// <summary>Remove uma anotação.</summary>
    [HttpDelete("{noteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long petId, string noteId, CancellationToken ct)
    {
        await service.DeleteAsync(petId, noteId, ct);
        return NoContent();
    }

    private PetNoteResponseDTO Decorate(PetNoteResponseDTO note)
    {
        note.Links = new List<Link>
        {
            LinkTo("self", "GET", nameof(GetById), new { petId = note.PetId, noteId = note.Id }),
            LinkTo("delete", "DELETE", nameof(Delete), new { petId = note.PetId, noteId = note.Id }),
            LinkTo("pet", "GET", "GetById", new { id = note.PetId }, "Pet")
        };
        return note;
    }
}
