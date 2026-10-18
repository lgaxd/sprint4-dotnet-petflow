using Microsoft.Extensions.Options;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Infrastructure.Mongo;

namespace PetFlowAPI.Tests.Infrastructure;

/// <summary>Executa apenas quando PETFLOW_MONGO_TEST_CONNECTION aponta para um MongoDB (ex.: mongodb://localhost:27017).</summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public const string Variable = "PETFLOW_MONGO_TEST_CONNECTION";

    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
            Skip = "Defina " + Variable + " para executar os testes contra um MongoDB real.";
    }
}

public class MongoPetNoteRepositoryTests
{
    private static MongoPetNoteRepository CreateSut()
    {
        var settings = Options.Create(new MongoSettings
        {
            ConnectionString = Environment.GetEnvironmentVariable(MongoFactAttribute.Variable)!,
            Database = $"petflow_test_{Guid.NewGuid():N}",
            ServerSelectionTimeoutSeconds = 3
        });
        return new MongoPetNoteRepository(new MongoContext(settings));
    }

    [MongoFact]
    public async Task AddEGet_DeveGerarObjectIdEPersistirDocumento()
    {
        // Arrange
        var sut = CreateSut();
        var note = new PetNote { PetId = 1, AuthorTutorId = 2, Title = "Vacina", Content = "V10", Tags = new List<string> { "vacina" } };

        // Act
        await sut.AddAsync(note);
        var found = await sut.GetByIdAsync(note.Id);

        // Assert
        Assert.Equal(24, note.Id.Length);
        Assert.Equal("Vacina", found!.Title);
        Assert.Equal(new[] { "vacina" }, found.Tags);
    }

    [MongoFact]
    public async Task GetByPet_DeveFiltrarPorTagPaginarEOrdenar()
    {
        // Arrange
        var sut = CreateSut();
        for (var i = 0; i < 5; i++)
            await sut.AddAsync(new PetNote { PetId = 1, Title = $"Nota {i}", Content = "c", Tags = new List<string> { i % 2 == 0 ? "par" : "impar" }, CreatedAt = DateTime.UtcNow.AddMinutes(i) });
        await sut.AddAsync(new PetNote { PetId = 2, Title = "Outro pet", Content = "c" });

        // Act
        var all = await sut.GetByPetAsync(1, null, new PageRequest(0, 2, "CreatedAt", true));
        var even = await sut.GetByPetAsync(1, "par", new PageRequest(0, 10, "Title", false));

        // Assert
        Assert.Equal(5, all.TotalItems);
        Assert.Equal("Nota 4", all.Items[0].Title);
        Assert.Equal(new[] { "Nota 0", "Nota 2", "Nota 4" }, even.Items.Select(n => n.Title));
    }

    [MongoFact]
    public async Task Delete_EDeleteByPet_DevemRemoverDocumentos()
    {
        // Arrange
        var sut = CreateSut();
        var first = new PetNote { PetId = 1, Title = "a", Content = "c" };
        var second = new PetNote { PetId = 1, Title = "b", Content = "c" };
        await sut.AddAsync(first);
        await sut.AddAsync(second);

        // Act
        var deleted = await sut.DeleteAsync(first.Id);
        var invalidId = await sut.DeleteAsync("nao-e-object-id");
        await sut.DeleteByPetAsync(1);

        // Assert
        Assert.True(deleted);
        Assert.False(invalidId);
        Assert.Null(await sut.GetByIdAsync(second.Id));
    }
}
