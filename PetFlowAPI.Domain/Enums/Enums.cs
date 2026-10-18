namespace PetFlowAPI.Domain.Enums;

/// <summary>Status de um evento de saúde (alinhado ao DDL Oracle da Sprint 4).</summary>
public enum HealthEventStatus
{
    AGENDADO,
    REALIZADO,
    CANCELADO
}

public enum SubscriptionStatus
{
    ATIVO,
    ENCERRADO,
    CANCELADO,
    EXPIRADO
}

public enum CouponStatus
{
    DISPONIVEL,
    RESGATADO,
    UTILIZADO
}

/// <summary>Perfis aceitos na coluna TUTOR.ROLE.</summary>
public static class UserRoles
{
    public const string Admin = "ADMIN";
    public const string Tutor = "TUTOR";

    public static bool IsValid(string? role) => role is Admin or Tutor;
}
