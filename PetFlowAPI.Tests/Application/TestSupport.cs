using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Tests.Application;

/// <summary>Usuário autenticado fake para os testes de serviços.</summary>
public sealed class FakeUser : ICurrentUser
{
    public long? UserId { get; init; }
    public string? Role { get; init; }
    public bool IsAdmin => Role == "ADMIN";

    public static FakeUser Tutor(long id) => new() { UserId = id, Role = "TUTOR" };
    public static FakeUser Admin(long id = 99) => new() { UserId = id, Role = "ADMIN" };
}

internal static class Mocks
{
    public static NullLogger<T> Logger<T>() => NullLogger<T>.Instance;

    public static Mock<IUnitOfWork> UnitOfWork()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return uow;
    }

    /// <summary>Repositório simulado onde <c>ExistsAsync</c> responde sempre com o valor indicado.</summary>
    public static Mock<IRepository<T>> RepositoryExists<T>(bool exists) where T : class
    {
        var repository = new Mock<IRepository<T>>();
        repository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(exists);
        return repository;
    }

    public static PagedResult<T> Page<T>(params T[] items) => new(items, 0, 10, items.Length);
}
