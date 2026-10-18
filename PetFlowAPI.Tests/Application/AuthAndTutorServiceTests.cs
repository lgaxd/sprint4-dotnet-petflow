using System.Linq.Expressions;
using Moq;
using PetFlowAPI.Application.Common;
using PetFlowAPI.Application.DTOs;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Application.Services;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Exceptions;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<ITutorRepository> _tutors = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();

    private AuthService CreateSut() => new(_tutors.Object, _hasher.Object, _tokens.Object, Mocks.Logger<AuthService>());

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        // Arrange
        var tutor = new Tutor { Id = 5, Name = "Ana", Email = "ana@petflow.test", PasswordHash = "hash", Role = "TUTOR" };
        var expires = DateTime.UtcNow.AddHours(1);
        _tutors.Setup(t => t.GetByEmailAsync("ana@petflow.test", It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        _hasher.Setup(h => h.Verify("segredo123", "hash")).Returns(true);
        _tokens.Setup(t => t.CreateToken(tutor)).Returns(new AccessToken("jwt-token", expires));

        // Act
        var response = await CreateSut().LoginAsync(new LoginRequestDTO { Email = " ana@petflow.test ", Password = "segredo123" });

        // Assert
        Assert.Equal("jwt-token", response.AccessToken);
        Assert.Equal(expires, response.ExpiresAt);
        Assert.Equal(5, response.TutorId);
        Assert.Equal("TUTOR", response.Role);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_DeveLancarInvalidCredentialsException()
    {
        // Arrange
        _tutors.Setup(t => t.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tutor?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().LoginAsync(new LoginRequestDTO { Email = "x@x.com", Password = "123" }));

        // Assert
        Assert.IsType<InvalidCredentialsException>(exception);
        _tokens.Verify(t => t.CreateToken(It.IsAny<Tutor>()), Times.Never);
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_DeveLancarInvalidCredentialsException()
    {
        // Arrange
        var tutor = new Tutor { Email = "ana@petflow.test", PasswordHash = "hash" };
        _tutors.Setup(t => t.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "hash")).Returns(false);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut().LoginAsync(new LoginRequestDTO { Email = "ana@petflow.test", Password = "errada" }));

        // Assert
        Assert.IsType<InvalidCredentialsException>(exception);
    }
}

public class TutorServiceTests
{
    private readonly Mock<ITutorRepository> _tutors = new();
    private readonly Mock<IRewardRepository> _rewards = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IUnitOfWork> _uow = Mocks.UnitOfWork();

    public TutorServiceTests() => _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

    private TutorService CreateSut(ICurrentUser user)
        => new(_tutors.Object, _rewards.Object, _hasher.Object, _uow.Object, user, Mocks.Logger<TutorService>());

    [Fact]
    public async Task Create_ComEmailNovo_DeveGravarTutorComSenhaHasheadaEPerfilTutor()
    {
        // Arrange
        Tutor? saved = null;
        _tutors.Setup(t => t.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tutor?)null);
        _tutors.Setup(t => t.AddAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()))
            .Callback<Tutor, CancellationToken>((t, _) => saved = t).Returns(Task.CompletedTask);
        var request = new TutorRequestDTO { Name = "Ana", Email = "ana@petflow.test", Password = "SenhaSegura1", Phone = "11999999999" };

        // Act
        var response = await CreateSut(new FakeUser()).CreateAsync(request);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("hashed", saved!.PasswordHash);
        Assert.Equal("TUTOR", saved.Role);
        Assert.Equal("ana@petflow.test", response.Email);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ComEmailJaCadastrado_DeveLancarConflictException()
    {
        // Arrange
        _tutors.Setup(t => t.GetByEmailAsync("ana@petflow.test", It.IsAny<CancellationToken>())).ReturnsAsync(new Tutor { Id = 1 });
        var request = new TutorRequestDTO { Name = "Ana", Email = "ana@petflow.test", Password = "SenhaSegura1" };

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(new FakeUser()).CreateAsync(request));

        // Assert
        Assert.IsType<ConflictException>(exception);
        _tutors.Verify(t => t.AddAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_DeOutroTutor_DeveLancarForbiddenException()
    {
        // Arrange
        var sut = CreateSut(FakeUser.Tutor(1));

        // Act
        var exception = await Record.ExceptionAsync(() => sut.GetByIdAsync(2));

        // Assert
        Assert.IsType<ForbiddenException>(exception);
        _tutors.Verify(t => t.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_ComoAdministrador_DeveRetornarQualquerTutor()
    {
        // Arrange
        _tutors.Setup(t => t.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Tutor { Id = 2, Name = "Bruno" });

        // Act
        var response = await CreateSut(FakeUser.Admin()).GetByIdAsync(2);

        // Assert
        Assert.Equal("Bruno", response.Name);
    }

    [Fact]
    public async Task GetById_Inexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _tutors.Setup(t => t.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((Tutor?)null);

        // Act
        var exception = await Record.ExceptionAsync(() => CreateSut(FakeUser.Tutor(7)).GetByIdAsync(7));

        // Assert
        Assert.IsType<NotFoundException>(exception);
    }

    [Fact]
    public async Task Update_SemSenha_DeveManterHashAtual()
    {
        // Arrange
        var tutor = new Tutor { Id = 1, Name = "Ana", Email = "ana@petflow.test", PasswordHash = "hash-antigo" };
        _tutors.Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        _tutors.Setup(t => t.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(tutor);

        // Act
        var response = await CreateSut(FakeUser.Tutor(1)).UpdateAsync(1, new TutorUpdateRequestDTO { Name = "Ana Maria", Email = "ana@petflow.test" });

        // Assert
        Assert.Equal("hash-antigo", tutor.PasswordHash);
        Assert.Equal("Ana Maria", response.Name);
        _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Update_ComNovaSenha_DeveGerarNovoHash()
    {
        // Arrange
        var tutor = new Tutor { Id = 1, Email = "ana@petflow.test", PasswordHash = "hash-antigo" };
        _tutors.Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        _tutors.Setup(t => t.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(tutor);

        // Act
        await CreateSut(FakeUser.Tutor(1)).UpdateAsync(1, new TutorUpdateRequestDTO { Name = "Ana", Email = "ana@petflow.test", Password = "NovaSenha123" });

        // Assert
        Assert.Equal("hashed", tutor.PasswordHash);
    }

    [Fact]
    public async Task Update_ComEmailDeOutroTutor_DeveLancarConflictException()
    {
        // Arrange
        _tutors.Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Tutor { Id = 1 });
        _tutors.Setup(t => t.GetByEmailAsync("outro@petflow.test", It.IsAny<CancellationToken>())).ReturnsAsync(new Tutor { Id = 2 });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            CreateSut(FakeUser.Tutor(1)).UpdateAsync(1, new TutorUpdateRequestDTO { Name = "Ana", Email = "outro@petflow.test" }));

        // Assert
        Assert.IsType<ConflictException>(exception);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_DoProprioTutor_DeveRemoverComDependentesEConfirmar()
    {
        // Arrange
        var tutor = new Tutor { Id = 3 };
        _tutors.Setup(t => t.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);

        // Act
        await CreateSut(FakeUser.Tutor(3)).DeleteAsync(3);

        // Assert
        _tutors.Verify(t => t.RemoveWithDependentsAsync(tutor, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPointsBalance_DeveRetornarResumoDeRecompensas()
    {
        // Arrange
        _tutors.Setup(t => t.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Tutor { Id = 1 });
        _rewards.Setup(r => r.GetPointsSummaryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new PointsSummary(300, 120));

        // Act
        var balance = await CreateSut(FakeUser.Tutor(1)).GetPointsBalanceAsync(1);

        // Assert
        Assert.Equal(300, balance.TotalEarned);
        Assert.Equal(120, balance.TotalRedeemed);
        Assert.Equal(180, balance.Balance);
    }

    [Fact]
    public async Task GetPaged_ComoTutor_DeveRestringirConsultaAoProprioRegistro()
    {
        // Arrange
        Expression<Func<Tutor, bool>>? captured = null;
        _tutors.Setup(t => t.GetPagedAsync(It.IsAny<Expression<Func<Tutor, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Tutor, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page<Tutor>());

        // Act
        await CreateSut(FakeUser.Tutor(4)).GetPagedAsync(new TutorFilter(), new PagingParameters());

        // Assert
        var predicate = captured!.Compile();
        Assert.True(predicate(new Tutor { Id = 4 }));
        Assert.False(predicate(new Tutor { Id = 5 }));
    }

    [Fact]
    public async Task GetPaged_ComoAdminComFiltroDeNome_DeveAplicarApenasFiltro()
    {
        // Arrange
        Expression<Func<Tutor, bool>>? captured = null;
        _tutors.Setup(t => t.GetPagedAsync(It.IsAny<Expression<Func<Tutor, bool>>>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Tutor, bool>>?, PageRequest, CancellationToken>((p, _, _) => captured = p)
            .ReturnsAsync(Mocks.Page(new Tutor { Id = 1, Name = "Ana Lima" }));

        // Act
        var result = await CreateSut(FakeUser.Admin()).GetPagedAsync(new TutorFilter { Name = "Ana" }, new PagingParameters());

        // Assert
        var predicate = captured!.Compile();
        Assert.True(predicate(new Tutor { Id = 50, Name = "Ana Lima" }));
        Assert.False(predicate(new Tutor { Id = 50, Name = "Bruno" }));
        Assert.Single(result.Items);
    }
}
