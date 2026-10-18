using System.Linq.Expressions;
using PetFlowAPI.Domain.Entities;

namespace PetFlowAPI.Domain.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>>? predicate, PageRequest page, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

/// <summary>Unidade de trabalho: confirma todas as alterações pendentes em uma única transação.</summary>
public interface IUnitOfWork
{
    /// <exception cref="Exceptions.ConflictException">Violação de unicidade ou de chave estrangeira.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITutorRepository : IRepository<Tutor>
{
    Task<Tutor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Marca para remoção o tutor e os registros dependentes (o DDL Oracle não usa ON DELETE CASCADE).</summary>
    Task RemoveWithDependentsAsync(Tutor tutor, CancellationToken cancellationToken = default);
}

public interface IPetRepository : IRepository<Pet>
{
    Task RemoveWithDependentsAsync(Pet pet, CancellationToken cancellationToken = default);
}

public interface IClinicRepository : IRepository<Clinic>
{
    Task RemoveWithDependentsAsync(Clinic clinic, CancellationToken cancellationToken = default);
}

public interface ICouponRepository : IRepository<Coupon>
{
    Task RemoveWithDependentsAsync(Coupon coupon, CancellationToken cancellationToken = default);
}

/// <summary>Pontos recebidos e resgatados por um tutor.</summary>
public sealed record PointsSummary(int Earned, int Redeemed)
{
    /// <summary>Saldo = pontos recebidos − pontos já resgatados.</summary>
    public int Balance => Earned - Redeemed;
}

public interface IRewardRepository
{
    Task<PointsSummary> GetPointsSummaryAsync(long tutorId, CancellationToken cancellationToken = default);
}

/// <summary>Repositório de documentos MongoDB (NoSQL).</summary>
public interface IPetNoteRepository
{
    Task AddAsync(PetNote note, CancellationToken cancellationToken = default);
    Task<PetNote?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PagedResult<PetNote>> GetByPetAsync(long petId, string? tag, PageRequest page, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task DeleteByPetAsync(long petId, CancellationToken cancellationToken = default);
}
