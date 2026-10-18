using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Hateoas;

namespace PetFlowAPI.Controllers;

[Route("clinics")]
public sealed class ClinicController(IClinicService service) : PetFlowControllerBase
{
    /// <summary>Cria uma nova clínica (somente ADMIN).</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(ClinicResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClinicResponseDTO>> Create(ClinicRequestDTO request, CancellationToken ct)
    {
        var clinic = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = clinic.Id }, clinic);
    }

    /// <summary>Lista clínicas com paginação, ordenação (name, createdAt) e filtro por nome.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ClinicResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ClinicResponseDTO>>> GetAll([FromQuery] ClinicFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), c => Decorate(c)));

    /// <summary>Busca uma clínica pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ClinicResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Atualiza uma clínica (somente ADMIN).</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(ClinicResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicResponseDTO>> Update(long id, ClinicRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Remove uma clínica e seus registros dependentes (somente ADMIN).</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private ClinicResponseDTO Decorate(ClinicResponseDTO clinic)
    {
        clinic.Links.Clear();
        clinic.Links.Add(LinkTo("self", "GET", nameof(GetById), new { id = clinic.Id }));
        clinic.Links.Add(LinkTo("plans", "GET", "GetAll", new { clinicId = clinic.Id }, "Plan"));
        if (IsAdmin)
        {
            clinic.Links.Add(LinkTo("update", "PUT", nameof(Update), new { id = clinic.Id }));
            clinic.Links.Add(LinkTo("delete", "DELETE", nameof(Delete), new { id = clinic.Id }));
        }
        return clinic;
    }
}

[Route("plans")]
public sealed class PlanController(IPlanService service) : PetFlowControllerBase
{
    /// <summary>Cria um plano de saúde pet (somente ADMIN).</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(PlanResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponseDTO>> Create(PlanRequestDTO request, CancellationToken ct)
    {
        var plan = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    /// <summary>Lista planos com paginação, ordenação (name, price, durationDays, pointsPerEvent) e filtros por nome e clínica.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PlanResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PlanResponseDTO>>> GetAll([FromQuery] PlanFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), p => Decorate(p)));

    /// <summary>Busca um plano pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PlanResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Atualiza um plano (somente ADMIN).</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(PlanResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponseDTO>> Update(long id, PlanRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Remove um plano sem assinaturas vinculadas (somente ADMIN).</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private PlanResponseDTO Decorate(PlanResponseDTO plan)
    {
        plan.Links.Clear();
        plan.Links.Add(LinkTo("self", "GET", nameof(GetById), new { id = plan.Id }));
        plan.Links.Add(LinkTo("clinic", "GET", "GetById", new { id = plan.ClinicId }, "Clinic"));
        if (IsAdmin)
        {
            plan.Links.Add(LinkTo("update", "PUT", nameof(Update), new { id = plan.Id }));
            plan.Links.Add(LinkTo("delete", "DELETE", nameof(Delete), new { id = plan.Id }));
        }
        return plan;
    }
}
