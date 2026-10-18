using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Domain.Enums;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Hateoas;

/// <summary>Base dos controllers: exige autenticação JWT e oferece utilitários HATEOAS.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class PetFlowControllerBase : ControllerBase
{
    protected bool IsAdmin => User.IsInRole(UserRoles.Admin);

    /// <summary>Cria um link absoluto para uma action (do controller atual, ou de <paramref name="controller"/>).</summary>
    protected Link LinkTo(string rel, string method, string action, object? values = null, string? controller = null)
        => new(rel, Url.ActionLink(action, controller ?? ControllerContext.ActionDescriptor.ControllerName, values) ?? string.Empty, method);

    /// <summary>Monta o envelope paginado, decorando cada item e adicionando links self/first/prev/next/last.</summary>
    protected PagedResponse<T> Paginate<T>(PagedResult<T> result, Action<T> decorate) where T : ResourceBase
    {
        foreach (var item in result.Items) decorate(item);

        var response = PagedResponse<T>.From(result);
        response.Links.Add(PageLink("self", result.Page));
        if (result.TotalPages > 0)
        {
            response.Links.Add(PageLink("first", 0));
            response.Links.Add(PageLink("last", result.TotalPages - 1));
        }
        if (result.Page > 0) response.Links.Add(PageLink("prev", Math.Min(result.Page - 1, Math.Max(result.TotalPages - 1, 0))));
        if (result.Page + 1 < result.TotalPages) response.Links.Add(PageLink("next", result.Page + 1));
        return response;
    }

    private Link PageLink(string rel, int page)
    {
        var query = new QueryBuilder();
        foreach (var (key, values) in Request.Query)
        {
            if (key.Equals("page", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var value in values) query.Add(key, value ?? string.Empty);
        }
        query.Add("page", page.ToString());

        var href = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{query.ToQueryString()}";
        return new Link(rel, href, "GET");
    }
}
