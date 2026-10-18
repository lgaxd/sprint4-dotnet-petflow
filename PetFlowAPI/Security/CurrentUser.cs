using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Enums;

namespace PetFlowAPI.Security;

/// <summary>Usuário autenticado extraído das claims do JWT da requisição corrente.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsAdmin => string.Equals(Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
}
