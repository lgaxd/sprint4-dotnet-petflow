using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Hateoas;

namespace PetFlowAPI.Controllers;

[Route("subscriptions")]
public sealed class SubscriptionController(ISubscriptionService service) : PetFlowControllerBase
{
    /// <summary>Cria uma assinatura de plano para um pet.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubscriptionResponseDTO>> Create(SubscriptionRequestDTO request, CancellationToken ct)
    {
        var subscription = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
    }

    /// <summary>Lista assinaturas com paginação, ordenação (startDate, endDate, status, createdAt) e filtros por pet, plano e status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SubscriptionResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SubscriptionResponseDTO>>> GetAll([FromQuery] SubscriptionFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), s => Decorate(s)));

    /// <summary>Busca uma assinatura pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(SubscriptionResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Atualiza uma assinatura.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(SubscriptionResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionResponseDTO>> Update(long id, SubscriptionRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Altera apenas o status da assinatura (ATIVO, ENCERRADO, CANCELADO, EXPIRADO).</summary>
    [HttpPut("{id:long}/status")]
    [ProducesResponseType(typeof(SubscriptionResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubscriptionResponseDTO>> UpdateStatus(long id, [FromQuery] SubscriptionStatus status, CancellationToken ct)
        => Ok(Decorate(await service.UpdateStatusAsync(id, status, ct)));

    /// <summary>Remove uma assinatura.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private SubscriptionResponseDTO Decorate(SubscriptionResponseDTO subscription)
    {
        subscription.Links = new List<Link>
        {
            LinkTo("self", "GET", nameof(GetById), new { id = subscription.Id }),
            LinkTo("update", "PUT", nameof(Update), new { id = subscription.Id }),
            LinkTo("status", "PUT", nameof(UpdateStatus), new { id = subscription.Id }),
            LinkTo("delete", "DELETE", nameof(Delete), new { id = subscription.Id }),
            LinkTo("pet", "GET", "GetById", new { id = subscription.PetId }, "Pet"),
            LinkTo("plan", "GET", "GetById", new { id = subscription.PlanId }, "Plan")
        };
        return subscription;
    }
}

[Route("health-events")]
public sealed class HealthEventController(IHealthEventService service) : PetFlowControllerBase
{
    /// <summary>
    /// Registra um evento de saúde. Se já for criado como REALIZADO, os pontos de recompensa
    /// (pontos do tipo de evento × multiplicador do plano ativo) são concedidos ao tutor.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(HealthEventResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthEventResponseDTO>> Create(HealthEventRequestDTO request, CancellationToken ct)
    {
        var healthEvent = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = healthEvent.Id }, healthEvent);
    }

    /// <summary>Lista eventos com paginação, ordenação (eventDate, status, createdAt) e filtros por pet, status e período (from/to).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<HealthEventResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<HealthEventResponseDTO>>> GetAll([FromQuery] HealthEventFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), e => Decorate(e)));

    /// <summary>Busca um evento de saúde pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(HealthEventResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthEventResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Atualiza um evento de saúde. Ao passar para REALIZADO, concede os pontos ao tutor (uma única vez).</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(HealthEventResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HealthEventResponseDTO>> Update(long id, HealthEventRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Remove um evento de saúde.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private HealthEventResponseDTO Decorate(HealthEventResponseDTO healthEvent)
    {
        healthEvent.Links = new List<Link>
        {
            LinkTo("self", "GET", nameof(GetById), new { id = healthEvent.Id }),
            LinkTo("update", "PUT", nameof(Update), new { id = healthEvent.Id }),
            LinkTo("delete", "DELETE", nameof(Delete), new { id = healthEvent.Id }),
            LinkTo("pet", "GET", "GetById", new { id = healthEvent.PetId }, "Pet")
        };
        if (healthEvent.ClinicId.HasValue)
            healthEvent.Links.Add(LinkTo("clinic", "GET", "GetById", new { id = healthEvent.ClinicId.Value }, "Clinic"));
        return healthEvent;
    }
}
