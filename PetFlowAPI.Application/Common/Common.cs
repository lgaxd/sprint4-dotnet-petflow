using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Application.Common;

/// <summary>Link HATEOAS (RFC 8288 simplificado).</summary>
public sealed record Link(string Rel, string Href, string Method);

/// <summary>Base dos DTOs de resposta que expõem links HATEOAS.</summary>
public abstract class ResourceBase
{
    public List<Link> Links { get; set; } = new();
}

/// <summary>Envelope de listagem paginada com links de navegação.</summary>
public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int Size { get; init; }
    public long TotalItems { get; init; }
    public int TotalPages { get; init; }
    public List<Link> Links { get; init; } = new();

    public static PagedResponse<T> From(PagedResult<T> result) => new()
    {
        Items = result.Items,
        Page = result.Page,
        Size = result.Size,
        TotalItems = result.TotalItems,
        TotalPages = result.TotalPages
    };
}

/// <summary>Parâmetros de paginação e ordenação recebidos via query string.</summary>
public class PagingParameters
{
    /// <summary>Página (base 0).</summary>
    [Range(0, int.MaxValue)]
    public int Page { get; set; }

    /// <summary>Itens por página (1 a 100).</summary>
    [Range(1, PageRequest.MaxSize)]
    public int Size { get; set; } = 10;

    /// <summary>Campo de ordenação (ex.: name, createdAt).</summary>
    public string? SortBy { get; set; }

    /// <summary>Direção da ordenação: asc ou desc.</summary>
    public string? Direction { get; set; }

    /// <summary>Converte para <see cref="PageRequest"/> validando o campo de ordenação contra uma lista permitida.</summary>
    public PageRequest ToPageRequest(IReadOnlyCollection<string> sortableFields, string defaultSort)
    {
        var size = Math.Clamp(Size, 1, PageRequest.MaxSize);
        var page = Math.Max(0, Page);

        var sortBy = string.IsNullOrWhiteSpace(SortBy) ? defaultSort : SortBy.Trim();
        var canonical = sortableFields.FirstOrDefault(f => f.Equals(sortBy, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
            throw new RequestValidationException("sortBy", $"Campo de ordenação inválido. Valores aceitos: {string.Join(", ", sortableFields)}.");

        var direction = Direction?.Trim().ToLowerInvariant();
        if (direction is not (null or "" or "asc" or "desc"))
            throw new RequestValidationException("direction", "Direção inválida. Use 'asc' ou 'desc'.");

        return new PageRequest(page, size, canonical, direction == "desc");
    }
}

/// <summary>Composição de predicados <see cref="Expression"/> (AND) preservando a tradução para SQL.</summary>
public static class PredicateExtensions
{
    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>>? left, Expression<Func<T, bool>> right)
    {
        if (left is null) return right;

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = Expression.AndAlso(
            new ReplaceVisitor(left.Parameters[0], parameter).Visit(left.Body)!,
            new ReplaceVisitor(right.Parameters[0], parameter).Visit(right.Body)!);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private sealed class ReplaceVisitor(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
