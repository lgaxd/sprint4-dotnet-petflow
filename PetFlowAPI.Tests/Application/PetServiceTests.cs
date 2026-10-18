using Moq;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Tests.Application;

public class PetServiceTests
{
    private readonly Mock<IPetRepository> _pets = new();
    private readonly Mock<IRepository<Tutor>> _tutors = Mocks.RepositoryExists<Tutor>(true);
    private readonly Mock<IRepository<Species>> _species = Mocks.RepositoryExists<Species>(true);
    private readonly Mock<IPetNoteRepository> _notes = new();
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();

    private PetService CreateSut(FakeUser user)
        => new(_pets.Object, _tutors.Object, _species.Object, _notes.Object, _uow.Object, user, Mocks.Logger<PetService>());

    private static PetRequestDTO ValidRequest(long tutorId = 1) => new() { Name = "Rex", Breed = "Labrador", SpeciesId = 1, TutorId = tutorId, Weight = 28.5m };

    [Fact]
    public async Task Create_ComDadosValidos_DevePersistirEPopularResposta()
    {
        // Arrange
        Pet? saved = null;
        _pets.Setup(p => p.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()))
            .Callback<Pet, CancellationToken>((p, _) => { p.Id = 10; saved = p; }).Returns(Task.CompletedTask);

        // Act
        var response = await CreateSut(FakeUser.Tutor(1)).CreateAsync(ValidRequest());

        // Assert
        Assert.Equal("Rex", saved!.Name);
        Assert.Equal(10, response.Id);
        Assert.Equal(1, response.TutorId);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ParaOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Tutor(1));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(ValidRequest(tutorId: 2)));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task Create_ComoAdministradorParaQualquerTutor_DevePermitir()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Admin());

        // Act
        var response = await sut.CreateAsync(ValidRequest(tutorId: 2));

        // Assert
        Assert.Equal(2, response.TutorId);
    }

    [Fact]
    public async Task Create_ComEspecieInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var species = Mocks.RepositoryExists<Species>(false);
        var sut = new PetService(_pets.Object, _tutors.Object, species.Object, _notes.Object, _uow.Object, FakeUser.Tutor(1), Mocks.Logger<PetService>());

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(ValidRequest()));

        // Assert
        var notFound = Assert.IsType<NotFoundException>(exception);
        Assert.Contains("Espécie", notFound.Message);
    }

    [Fact]
    public async Task Create_ComTutorInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        var tutors = Mocks.RepositoryExists<Tutor>(false);
        var sut = new PetService(_pets.Object, tutors.Object, _species.Object, _notes.Object, _uow.Object, FakeUser.Admin(), Mocks.Logger<PetService>());

        // Act
        var exception = await Record.ExceptionAsync(() => sut.CreateAsync(ValidRequest()));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task Create_ComDataDeNascimentoFutura_DeveLancarRequestValidationException()
    {
        // Arrange
        var request = ValidRequest();
        request.BirthDate = DateTime.UtcNow.AddDays(5);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(1)).CreateAsync(request));

        // Assert
        var validation = Assert.IsType<RequestValidationException>(exception);
        Assert.Contains("BirthDate", validation.Errors.Keys);
    }

    [Fact]
    public async Task GetById_PetDeOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { Id = 1, TutorId = 2 });

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(1)).GetByIdAsync(1));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
    }

    [Fact]
    public async Task GetById_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Admin()).GetByIdAsync(1));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task Update_DeveAplicarNovosDados()
    {
        // Arrange
        var pet = new Pet { Id = 1, TutorId = 1, Name = "Rex", SpeciesId = 1 };
        _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        var request = ValidRequest();
        request.Name = "Rex II";

        // Act
        var response = await CreateSut(FakeUser.Tutor(1)).UpdateAsync(1, request);

        // Assert
        Assert.Equal("Rex II", pet.Name);
        Assert.Equal("Rex II", response.Name);
        _pets.Verify(p => p.Update(pet), Times.Once);
    }

    [Fact]
    public async Task Delete_DeveRemoverPetComDependentesEAnotacoesDoMongo()
    {
        // Arrange
        var pet = new Pet { Id = 8, TutorId = 1 };
        _pets.Setup(p => p.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(pet);

        // Act
        await CreateSut(FakeUser.Tutor(1)).DeleteAsync(8);

        // Assert
        _pets.Verify(p => p.RemoveWithDependentsAsync(pet, It.IsAny<CancellationToken>()), Times.Once);
        _notes.Verify(n => n.DeleteByPetAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaged_ComCampoDeOrdenacaoInvalido_DeveLancarRequestValidationException()
    {
        // Arrange
        var paging = new PagingParameters { SortBy = "senha" };

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Admin()).GetPagedAsync(new PetFilter(), paging));

        // Assert
        Assert.IsType<RequestValidationException>(exception);
        _pets.Verify(p => p.GetPagedAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Pet, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPaged_DeveRepassarPaginacaoEOrdenacaoAoRepositorio()
    {
        // Arrange
        PageRequest? captured = null;
        _pets.Setup(p => p.GetPagedAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Pet, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<System.Linq.Expressions.Expression<Func<Pet, bool>>?, PageRequest, CancellationToken>((_, page, _) => captured = page)
            .ReturnsAsync(Mocks.Page(new Pet { Id = 1, Name = "Rex" }));
        var paging = new PagingParameters { Page = 2, Size = 5, SortBy = "name", Direction = "desc" };

        // Act
        var result = await CreateSut(FakeUser.Admin()).GetPagedAsync(new PetFilter { Name = "Re" }, paging);

        // Assert
        Assert.Equal(new PageRequest(2, 5, "Name", true), captured);
        Assert.Equal("Rex", result.Items[0].Name);
    }
}

public class PetNoteServiceTests
{
    private readonly Mock<IPetNoteRepository> _notes = new();
    private readonly Mock<IRepository<Pet>> _pets = new();

    private PetNoteService CreateSut(FakeUser user) => new(_notes.Object, _pets.Object, user, Mocks.Logger<PetNoteService>());

    private void PetOwnedBy(long tutorId) => _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { Id = 1, TutorId = tutorId });

    [Fact]
    public async Task Create_DeveNormalizarTagsEDefinirAutor()
    {
        // Arrange
        PetNote? saved = null;
        PetOwnedBy(7);
        _notes.Setup(n => n.AddAsync(It.IsAny<PetNote>(), It.IsAny<CancellationToken>()))
            .Callback<PetNote, CancellationToken>((n, _) => { n.Id = "abc"; saved = n; }).Returns(Task.CompletedTask);
        var request = new PetNoteRequestDTO { Title = "Consulta", Content = "Tudo bem", Tags = new List<string> { " Vacina ", "vacina", "", "Peso" } };

        // Act
        var response = await CreateSut(FakeUser.Tutor(7)).CreateAsync(1, request);

        // Assert
        Assert.Equal(7, saved!.AuthorTutorId);
        Assert.Equal(new[] { "vacina", "peso" }, saved.Tags);
        Assert.Equal("abc", response.Id);
    }

    [Fact]
    public async Task Create_ParaPetDeOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        PetOwnedBy(2);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            CreateSut(FakeUser.Tutor(7)).CreateAsync(1, new PetNoteRequestDTO { Title = "t", Content = "c" }));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
        _notes.Verify(n => n.AddAsync(It.IsAny<PetNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_PetInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _pets.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            CreateSut(FakeUser.Admin()).CreateAsync(1, new PetNoteRequestDTO { Title = "t", Content = "c" }));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task GetByPet_DeveNormalizarTagEOrdenarPorDataDecrescentePorPadrao()
    {
        // Arrange
        PetOwnedBy(7);
        string? tag = null;
        PageRequest? page = null;
        _notes.Setup(n => n.GetByPetAsync(1, It.IsAny<string?>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<long, string?, PageRequest, CancellationToken>((_, t, p, _) => { tag = t; page = p; })
            .ReturnsAsync(Mocks.Page(new PetNote { Id = "1", PetId = 1, Title = "x" }));

        // Act
        var result = await CreateSut(FakeUser.Tutor(7)).GetByPetAsync(1, " VACINA ", new PagingParameters());

        // Assert
        Assert.Equal("vacina", tag);
        Assert.Equal("CreatedAt", page!.SortBy);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Delete_AnotacaoDeOutroPet_DeveLancarNotFoundException()
    {
        // Arrange
        PetOwnedBy(7);
        _notes.Setup(n => n.GetByIdAsync("n1", It.IsAny<CancellationToken>())).ReturnsAsync(new PetNote { Id = "n1", PetId = 99 });

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).DeleteAsync(1, "n1"));

        // Assert
        Assert.IsType<NotFoundException>(exception);
        _notes.Verify(n => n.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_AnotacaoDoPet_DeveRemover()
    {
        // Arrange
        PetOwnedBy(7);
        _notes.Setup(n => n.GetByIdAsync("n1", It.IsAny<CancellationToken>())).ReturnsAsync(new PetNote { Id = "n1", PetId = 1 });

        // Act
        await CreateSut(FakeUser.Tutor(7)).DeleteAsync(1, "n1");

        // Assert
        _notes.Verify(n => n.DeleteAsync("n1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_DeveRetornarAnotacaoDoPet()
    {
        // Arrange
        PetOwnedBy(7);
        _notes.Setup(n => n.GetByIdAsync("n1", It.IsAny<CancellationToken>())).ReturnsAsync(new PetNote { Id = "n1", PetId = 1, Title = "Retorno" });

        // Act
        var note = await CreateSut(FakeUser.Tutor(7)).GetByIdAsync(1, "n1");

        // Assert
        Assert.Equal("Retorno", note.Title);
    }
}
