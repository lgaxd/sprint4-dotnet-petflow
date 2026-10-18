using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Exceptions;

namespace PetFlowAPI.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(Tutor tutor);
}

/// <summary>Usuário autenticado na requisição atual (implementado pela camada de API).</summary>
public interface ICurrentUser
{
    long? UserId { get; }
    string? Role { get; }
    bool IsAdmin { get; }
}

public static class CurrentUserExtensions
{
    /// <summary>Id do tutor ao qual os dados devem ser restritos; <c>null</c> para administradores (sem restrição).</summary>
    public static long? ScopedTutorId(this ICurrentUser user)
        => user.IsAdmin ? null : user.UserId ?? throw new ForbiddenException("Usuário não identificado.");

    /// <summary>Garante que o usuário é administrador ou o próprio tutor informado.</summary>
    public static void EnsureAccessToTutor(this ICurrentUser user, long tutorId)
    {
        if (user.IsAdmin) return;
        if (user.UserId != tutorId) throw new ForbiddenException();
    }
}
