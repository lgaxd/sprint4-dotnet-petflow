using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Hateoas;

namespace PetFlowAPI.Controllers;

[Route("coupons")]
public sealed class CouponController(ICouponService service) : PetFlowControllerBase
{
    /// <summary>Cria um cupom de desconto (somente ADMIN).</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(CouponResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CouponResponseDTO>> Create(CouponRequestDTO request, CancellationToken ct)
    {
        var coupon = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = coupon.Id }, coupon);
    }

    /// <summary>Lista cupons com paginação, ordenação (code, status, expirationDate, createdAt) e filtros por código e status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CouponResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CouponResponseDTO>>> GetAll([FromQuery] CouponFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), c => Decorate(c)));

    /// <summary>Busca um cupom pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CouponResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CouponResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Atualiza um cupom (somente ADMIN).</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(CouponResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CouponResponseDTO>> Update(long id, CouponRequestDTO request, CancellationToken ct)
        => Ok(Decorate(await service.UpdateAsync(id, request, ct)));

    /// <summary>Altera o status do cupom: DISPONIVEL, RESGATADO ou UTILIZADO (somente ADMIN).</summary>
    [HttpPut("{id:long}/status")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(CouponResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CouponResponseDTO>> UpdateStatus(long id, [FromQuery] CouponStatus status, CancellationToken ct)
        => Ok(Decorate(await service.UpdateStatusAsync(id, status, ct)));

    /// <summary>Remove um cupom e seus resgates (somente ADMIN).</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private CouponResponseDTO Decorate(CouponResponseDTO coupon)
    {
        coupon.Links.Clear();
        coupon.Links.Add(LinkTo("self", "GET", nameof(GetById), new { id = coupon.Id }));
        if (coupon.Status == CouponStatus.DISPONIVEL)
            coupon.Links.Add(LinkTo("redeem", "POST", "Create", null, "Redeem"));
        if (IsAdmin)
        {
            coupon.Links.Add(LinkTo("update", "PUT", nameof(Update), new { id = coupon.Id }));
            coupon.Links.Add(LinkTo("status", "PUT", nameof(UpdateStatus), new { id = coupon.Id }));
            coupon.Links.Add(LinkTo("delete", "DELETE", nameof(Delete), new { id = coupon.Id }));
        }
        return coupon;
    }
}

[Route("redeems")]
public sealed class RedeemController(IRedeemService service) : PetFlowControllerBase
{
    /// <summary>Resgata um cupom debitando os pontos exigidos pelo modelo do cupom do saldo do tutor.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RedeemResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RedeemResponseDTO>> Create(RedeemRequestDTO request, CancellationToken ct)
    {
        var redeem = Decorate(await service.CreateAsync(request, ct));
        return CreatedAtAction(nameof(GetById), new { id = redeem.Id }, redeem);
    }

    /// <summary>Lista resgates com paginação, ordenação (pointsUsed, createdAt) e filtros por tutor e cupom.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RedeemResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RedeemResponseDTO>>> GetAll([FromQuery] RedeemFilter filter, [FromQuery] PagingParameters paging, CancellationToken ct)
        => Ok(Paginate(await service.GetPagedAsync(filter, paging, ct), r => Decorate(r)));

    /// <summary>Busca um resgate pelo ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(RedeemResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RedeemResponseDTO>> GetById(long id, CancellationToken ct)
        => Ok(Decorate(await service.GetByIdAsync(id, ct)));

    /// <summary>Remove um resgate (recurso imutável por design; somente ADMIN, para correções).</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    private RedeemResponseDTO Decorate(RedeemResponseDTO redeem)
    {
        redeem.Links.Clear();
        redeem.Links.Add(LinkTo("self", "GET", nameof(GetById), new { id = redeem.Id }));
        redeem.Links.Add(LinkTo("tutor", "GET", "GetById", new { id = redeem.TutorId }, "Tutor"));
        redeem.Links.Add(LinkTo("coupon", "GET", "GetById", new { id = redeem.CouponId }, "Coupon"));
        if (IsAdmin) redeem.Links.Add(LinkTo("delete", "DELETE", nameof(Delete), new { id = redeem.Id }));
        return redeem;
    }
}
