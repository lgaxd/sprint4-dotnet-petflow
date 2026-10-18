using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Infrastructure.Persistence.Repositories;

/// <summary>Implementação genérica do padrão Repository sobre o Entity Framework Core.</summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly PetFlowContext Context;

    public Repository(PetFlowContext context) => Context = context;

    protected DbSet<T> Set => Context.Set<T>();

    public async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await Set.FindAsync(new object[] { id }, cancellationToken);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(predicate, cancellationToken);

    // Count em vez de AnyAsync: evita a tradução booleana (TRUE/FALSE) que o provider Oracle não aceita em SQL.
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await Set.Where(predicate).Take(1).CountAsync(cancellationToken) > 0;

    public async Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>>? predicate, PageRequest page, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = Set.AsNoTracking();
        if (predicate is not null) query = query.Where(predicate);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await ApplyOrdering(query, page.SortBy, page.Descending)
            .Skip(page.Page * page.Size)
            .Take(page.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page.Page, page.Size, total);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);

    /// <summary>Ordenação dinâmica restrita a propriedades escalares públicas da entidade (sem SQL livre); desempata por Id.</summary>
    private static IQueryable<T> ApplyOrdering(IQueryable<T> query, string? sortBy, bool descending)
    {
        var property = ResolveSortProperty(sortBy);
        var idProperty = typeof(T).GetProperty("Id");

        var parameter = Expression.Parameter(typeof(T), "e");
        var ordered = OrderBy(query, parameter, property, descending, thenBy: false);
        if (idProperty is not null && idProperty != property)
            ordered = OrderBy(ordered, parameter, idProperty, descending, thenBy: true);
        return ordered;
    }

    private static PropertyInfo? ResolveSortProperty(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return typeof(T).GetProperty("Id");

        var property = typeof(T).GetProperty(sortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        var type = property is null ? null : Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var scalar = type is not null && (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime));
        return scalar ? property : typeof(T).GetProperty("Id");
    }

    private static IQueryable<T> OrderBy(IQueryable<T> query, ParameterExpression parameter, PropertyInfo? property, bool descending, bool thenBy)
    {
        if (property is null) return query;

        var body = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(body, parameter);
        var method = (thenBy, descending) switch
        {
            (false, false) => nameof(Queryable.OrderBy),
            (false, true) => nameof(Queryable.OrderByDescending),
            (true, false) => nameof(Queryable.ThenBy),
            (true, true) => nameof(Queryable.ThenByDescending)
        };

        var call = Expression.Call(typeof(Queryable), method, new[] { typeof(T), property.PropertyType }, query.Expression, Expression.Quote(lambda));
        return query.Provider.CreateQuery<T>(call);
    }
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PetFlowContext _context;

    public UnitOfWork(PetFlowContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("O registro foi alterado ou removido por outra operação. Recarregue e tente novamente.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw Translate(ex);
        }
    }

    /// <summary>Traduz erros de integridade do banco (Oracle/SQL Server) em <see cref="ConflictException"/>.</summary>
    internal static Exception Translate(DbUpdateException exception)
    {
        var message = exception.ToString();

        if (Contains(message, "ORA-00001") || Contains(message, "unique constraint") || Contains(message, "UNIQUE KEY") || Contains(message, "duplicate key"))
            return new ConflictException("Registro duplicado: já existe um registro com os mesmos dados únicos.", exception);

        if (Contains(message, "ORA-02291") || Contains(message, "ORA-02292") || Contains(message, "integrity constraint")
            || Contains(message, "FOREIGN KEY") || Contains(message, "REFERENCE constraint"))
            return new ConflictException("Operação bloqueada: existem registros relacionados.", exception);

        return exception;
    }

    private static bool Contains(string text, string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);
}
