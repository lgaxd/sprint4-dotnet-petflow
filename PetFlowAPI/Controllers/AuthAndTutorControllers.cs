using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Hateoas;

namespace PetFlowAPI.Controllers;

[Route("auth")]
public sealed class AuthController(IAuthService service) : PetFlowControllerBase
{
    /// <summary>Autentica o tutor e emite um token JWT (Bearer).</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDTO>> Login(LoginRequestDTO request, CancellationToken ct)
        => Ok(await service.LoginAsync(request, ct));
}

[Route("tutors")]
public sealed class TutorController(ITutorService service) : PetFlowControllerBase
{
    /// <summary>Cadastra um novo tutor (endpoint público de auto-cadastro; o perfil é sempre TUTOR).</summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(TutorResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TutorResponseDTO>> Create(TutorRequestDTO request, CancellationToken ct)
    {
        var tutor = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = tutor.Id }, tutor);
    }

    /// <summary>Lista tutores com paginação, ordenação e filtros (administradores veem todos; tutores veem apenas a si).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TutorResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TutorResponseDTO>>> GetAll([FromQuery] TutorFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), t => Decorate(t)));

    /// <summary>Busca um tutor pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TutorResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TutorResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Saldo de pontos de recompensa do tutor.</summary>
    [HttpGet("{id:long}/points")]
    [ProducesResponseType(typeof(PointsBalanceDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsBalanceDTO>> GetPoints(long id, CancellationToken ct)
        => Ok(await service.GetPointsBalanceAsync(id, ct));

    /// <summary>Atualiza os dados de um tutor (a senha só é alterada se informada).</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(TutorResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TutorResponseDTO>> Update(long id, TutorUpdateRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Remove um tutor e seus registros dependentes.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private TutorResponseDTO Decorate(TutorResponseDTO tutor)
    {
        tutor.Links = new List<Link>
        {
            LinkTo("self", "GET", nameof(GetById), new { id = tutor.Id }),
            LinkTo("update", "PUT", nameof(Update), new { id = tutor.Id }),
            LinkTo("delete", "DELETE", nameof(Delete), new { id = tutor.Id }),
            LinkTo("points", "GET", nameof(GetPoints), new { id = tutor.Id }),
            LinkTo("pets", "GET", "GetAll", new { tutorId = tutor.Id }, "Pet")
        };
        return tutor;
    }
}
