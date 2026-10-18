using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Extensions;
using Serilog.Context;

namespace PetFlowAPI.Middleware;

/// <summary>Propaga/gera o X-Correlation-ID e o publica no contexto de log e na métrica de duração.</summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                PetFlowMetrics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("http.status_code", context.Response.StatusCode));
            }
        }
    }
}

/// <summary>
/// Tratamento global e consistente de exceções: converte exceções de domínio/aplicação em
/// <see cref="ProblemDetails"/> (RFC 7807) com o status HTTP adequado.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _problemDetails = problemDetails;
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Erro não tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning("{ExceptionType} em {Method} {Path}: {Message}", exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        var problem = exception is RequestValidationException validation
            ? new ValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value)) { Status = status, Title = title }
            : new ProblemDetails { Status = status, Title = title };

        problem.Detail = status >= StatusCodes.Status500InternalServerError
            ? (_environment.IsDevelopment() ? exception.ToString() : "Ocorreu um erro inesperado. Informe o correlationId ao suporte.")
            : exception.Message;

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static (int Status, string Title) Map(Exception exception) => exception switch
    {
        RequestValidationException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
        NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
        ConflictException => (StatusCodes.Status409Conflict, "Conflito com o estado atual do recurso."),
        BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada."),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Não autenticado."),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Acesso negado."),
        BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição malformada."),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno no servidor.")
    };
}
