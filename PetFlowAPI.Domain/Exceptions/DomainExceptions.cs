namespace PetFlowAPI.Domain.Exceptions;

/// <summary>Violação de uma regra de negócio. Mapeada para HTTP 422 na camada de API.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Recurso inexistente. Mapeada para HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string resource, object key)
        : base($"{resource} com ID {key} não encontrado(a).") { }
}

/// <summary>Conflito com o estado atual (duplicidade, FK). Mapeada para HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Credenciais inválidas. Mapeada para HTTP 401.</summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message = "E-mail ou senha inválidos.") : base(message) { }
}

/// <summary>Usuário autenticado sem permissão sobre o recurso. Mapeada para HTTP 403.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Você não tem permissão para acessar este recurso.") : base(message) { }
}

/// <summary>Entrada inválida em campo específico. Mapeada para HTTP 400.</summary>
public class RequestValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public RequestValidationException(string field, string message) : base(message)
        => Errors = new Dictionary<string, string[]> { [field] = new[] { message } };
}
