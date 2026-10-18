using System.Net.Http.Json;
using System.Text.Json;

namespace PetFlowAPI.Tests.Integration;

public class InfrastructureEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public InfrastructureEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task HealthLive_DeveRetornar200ESemDependerDoBanco()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task HealthReady_ComBancoDisponivel_DeveRetornar200ListandoOsChecks()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health/ready");
        var body = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var names = body.GetProperty("checks").EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        Assert.Contains("oracle-database", names);
        Assert.Contains("external-services", names);
    }

    [Fact]
    public async Task Health_SemToken_DevePermitirAcessoAnonimo()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Requisicao_DevePreservarCorrelationIdRecebido()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "teste-sprint4-001");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal("teste-sprint4-001", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task Requisicao_SemCorrelationId_DeveGerarUm()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health/live");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues("X-Correlation-ID").Single()));
    }

    [Fact]
    public async Task RotaInexistente_DeveRetornar404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/rota-que-nao-existe");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_DevePublicarOpenApiComEsquemaBearerETodosOsRecursos()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        var document = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"bearer\"", document);
        foreach (var path in new[] { "/auth/login", "/tutors", "/pets", "/pets/{petId}/notes", "/clinics", "/plans", "/subscriptions", "/health-events", "/coupons", "/redeems" })
            Assert.Contains($"\"{path}\"", document);
    }
}

public class AuthenticationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthenticationTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/tutors")]
    [InlineData("/pets")]
    [InlineData("/clinics")]
    [InlineData("/plans")]
    [InlineData("/subscriptions")]
    [InlineData("/health-events")]
    [InlineData("/coupons")]
    [InlineData("/redeems")]
    public async Task EndpointsProtegidos_SemToken_DevemRetornar401(string path)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync(path);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cadastro_Login_DeveEmitirTokenEPermitirRotaProtegida()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await client.GetAsync($"/tutors/{tutorId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cadastro_ComEmailDuplicado_DeveRetornar409ComProblemDetails()
    {
        // Arrange
        var (_, _, email) = await _factory.RegisterTutorAsync();
        var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/tutors", new { name = "Outro", email, password = "SenhaSegura123" });
        var problem = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task Cadastro_ComPayloadInvalido_DeveRetornar400ComErrosPorCampo()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/tutors", new { name = "", email = "nao-e-email", password = "123" });
        var problem = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Cadastro_DeveSempreCriarPerfilTutor()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/tutors", new { name = "Espertinho", email = $"{Guid.NewGuid():N}@x.com", password = "SenhaSegura123", role = "ADMIN" });
        var body = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal("TUTOR", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_DeveRetornar401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/auth/login", new { email = SeedData.AdminEmail, password = "errada-123" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenAdulterado_DeveRetornar401()
    {
        // Arrange
        var client = await _factory.AdminClientAsync();
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");

        // Act
        using var response = await client.GetAsync("/tutors");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TutorComum_NaoPodeCriarClinica_ApenasAdmin()
    {
        // Arrange
        var (tutor, _, _) = await _factory.RegisterTutorAsync();
        var admin = await _factory.AdminClientAsync();
        var payload = new { name = "Nova Clínica", cnpj = $"{Random.Shared.Next(10000000, 99999999)}/0001-00" };

        // Act
        using var forbidden = await tutor.PostAsJsonAsync("/clinics", payload);
        using var created = await admin.PostAsJsonAsync("/clinics", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task Tutor_NaoPodeLerDadosDeOutroTutor_MasListagemMostraApenasASiMesmo()
    {
        // Arrange
        var (tutorA, idA, _) = await _factory.RegisterTutorAsync();
        var (_, idB, _) = await _factory.RegisterTutorAsync();

        // Act
        using var other = await tutorA.GetAsync($"/tutors/{idB}");
        using var list = await tutorA.GetAsync("/tutors");
        var page = await ApiFactory.ReadJsonAsync(list);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
        var ids = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToList();
        Assert.Equal(new[] { idA }, ids);
    }

    [Fact]
    public async Task Admin_ListaTodosOsTutores()
    {
        // Arrange
        await _factory.RegisterTutorAsync();
        await _factory.RegisterTutorAsync();
        var admin = await _factory.AdminClientAsync();

        // Act
        using var response = await admin.GetAsync("/tutors?size=100");
        var page = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.True(page.GetProperty("totalItems").GetInt64() >= 3);
    }
}

public class PetEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PetEndpointsTests(ApiFactory factory) => _factory = factory;

    private async Task<long> CreatePetAsync(HttpClient client, long tutorId, string name, decimal weight = 10m)
    {
        using var response = await client.PostAsJsonAsync("/pets", new { name, breed = "SRD", weight, speciesId = _factory.Seed.SpeciesId, tutorId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ApiFactory.ReadJsonAsync(response)).GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task CrudCompleto_DeveCriarConsultarAtualizarERemover()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();

        // Act
        var petId = await CreatePetAsync(client, tutorId, "Rex");
        using var get = await client.GetAsync($"/pets/{petId}");
        using var update = await client.PutAsJsonAsync($"/pets/{petId}", new { name = "Rex II", breed = "Lab", weight = 30.5m, speciesId = _factory.Seed.SpeciesId, tutorId });
        using var delete = await client.DeleteAsync($"/pets/{petId}");
        using var afterDelete = await client.GetAsync($"/pets/{petId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Rex II", (await ApiFactory.ReadJsonAsync(update)).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Create_DeveRetornar201ComLocationELinksHateoas()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await client.PostAsJsonAsync("/pets", new { name = "Thor", speciesId = _factory.Seed.SpeciesId, tutorId });
        var body = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var rels = body.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()).ToList();
        Assert.Contains("self", rels);
        Assert.Contains("update", rels);
        Assert.Contains("delete", rels);
        Assert.Contains("notes", rels);
        Assert.Contains("tutor", rels);
    }

    [Fact]
    public async Task GetAll_DevePaginarOrdenarFiltrarEExporLinksDeNavegacao()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        foreach (var (name, weight) in new[] { ("Thor", 32m), ("Rex", 28m), ("Bolt", 14m), ("Nala", 5m), ("Luna", 3m) })
            await CreatePetAsync(client, tutorId, name, weight);

        // Act
        using var response = await client.GetAsync("/pets?page=1&size=2&sortBy=name&direction=asc");
        var page = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(new[] { "Nala", "Rex" }, page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString()));
        Assert.Equal(5, page.GetProperty("totalItems").GetInt64());
        Assert.Equal(3, page.GetProperty("totalPages").GetInt32());
        var links = page.GetProperty("links").EnumerateArray().ToDictionary(l => l.GetProperty("rel").GetString()!, l => l.GetProperty("href").GetString()!);
        Assert.Contains("page=2", links["next"]);
        Assert.Contains("page=0", links["prev"]);
        Assert.Contains("page=0", links["first"]);
        Assert.Contains("page=2", links["last"]);
        Assert.Contains("sortBy=name", links["next"]);
    }

    [Fact]
    public async Task GetAll_ComFiltroDeNome_DeveRetornarSomenteCorrespondentes()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        await CreatePetAsync(client, tutorId, "Mimi");
        await CreatePetAsync(client, tutorId, "Mimosa");
        await CreatePetAsync(client, tutorId, "Thor");

        // Act
        using var response = await client.GetAsync("/pets?name=Mim");
        var page = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(2, page.GetProperty("totalItems").GetInt64());
    }

    [Fact]
    public async Task GetAll_ComOrdenacaoInvalida_DeveRetornar400()
    {
        // Arrange
        var (client, _, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await client.GetAsync("/pets?sortBy=tutor.passwordHash");
        var problem = await ApiFactory.ReadJsonAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("sortBy", out _));
    }

    [Fact]
    public async Task GetAll_TutorEnxergaApenasOsProprioPets_AdminEnxergaTodos()
    {
        // Arrange
        var (clientA, idA, _) = await _factory.RegisterTutorAsync();
        var (clientB, idB, _) = await _factory.RegisterTutorAsync();
        await CreatePetAsync(clientA, idA, "PetDeA");
        await CreatePetAsync(clientB, idB, "PetDeB");
        var admin = await _factory.AdminClientAsync();

        // Act
        var forA = await ApiFactory.ReadJsonAsync(await clientA.GetAsync("/pets?size=100"));
        var forAdmin = await ApiFactory.ReadJsonAsync(await admin.GetAsync("/pets?size=100"));

        // Assert
        Assert.All(forA.GetProperty("items").EnumerateArray(), item => Assert.Equal(idA, item.GetProperty("tutorId").GetInt64()));
        Assert.True(forAdmin.GetProperty("totalItems").GetInt64() >= 2);
    }

    [Fact]
    public async Task Create_ParaOutroTutor_DeveRetornar403()
    {
        // Arrange
        var (clientA, _, _) = await _factory.RegisterTutorAsync();
        var (_, idB, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await clientA.PostAsJsonAsync("/pets", new { name = "Intruso", speciesId = _factory.Seed.SpeciesId, tutorId = idB });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComEspecieInexistente_DeveRetornar404()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await client.PostAsJsonAsync("/pets", new { name = "Rex", speciesId = 9999, tutorId });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComCamposObrigatoriosAusentes_DeveRetornar400()
    {
        // Arrange
        var (client, _, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await client.PostAsJsonAsync("/pets", new { breed = "SRD" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_PetDeOutroTutor_DeveRetornar403()
    {
        // Arrange
        var (clientA, idA, _) = await _factory.RegisterTutorAsync();
        var (clientB, _, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(clientA, idA, "Privado");

        // Act
        using var response = await clientB.GetAsync($"/pets/{petId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anotacoes_MongoDb_DevemSerCriadasListadasFiltradasPorTagERemovidas()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(client, tutorId, "Rex");

        // Act
        using var created = await client.PostAsJsonAsync($"/pets/{petId}/notes", new { title = "Vacina", content = "V10 aplicada", tags = new[] { "Vacina", "preventivo" } });
        using var other = await client.PostAsJsonAsync($"/pets/{petId}/notes", new { title = "Peso", content = "28kg", tags = new[] { "peso" } });
        var note = await ApiFactory.ReadJsonAsync(created);
        var noteId = note.GetProperty("id").GetString();
        var filtered = await ApiFactory.ReadJsonAsync(await client.GetAsync($"/pets/{petId}/notes?tag=VACINA"));
        using var delete = await client.DeleteAsync($"/pets/{petId}/notes/{noteId}");
        using var afterDelete = await client.GetAsync($"/pets/{petId}/notes/{noteId}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
        Assert.Equal(1, filtered.GetProperty("totalItems").GetInt64());
        Assert.Equal("vacina", note.GetProperty("tags")[0].GetString());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Anotacoes_DePetDeOutroTutor_DevemRetornar403()
    {
        // Arrange
        var (clientA, idA, _) = await _factory.RegisterTutorAsync();
        var (clientB, _, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(clientA, idA, "Rex");

        // Act
        using var response = await clientB.PostAsJsonAsync($"/pets/{petId}/notes", new { title = "x", content = "y" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoverPet_DeveRemoverTambemAsAnotacoesDoMongo()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(client, tutorId, "Rex");
        await client.PostAsJsonAsync($"/pets/{petId}/notes", new { title = "x", content = "y" });

        // Act
        using var delete = await client.DeleteAsync($"/pets/{petId}");
        var remaining = await _factory.Notes.GetByPetAsync(petId, null, new PetFlowAPI.Domain.Repositories.PageRequest(0, 10, "CreatedAt", false));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, remaining.TotalItems);
    }
}

public class RewardFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RewardFlowTests(ApiFactory factory) => _factory = factory;

    private async Task<long> CreatePetAsync(HttpClient client, long tutorId)
    {
        using var response = await client.PostAsJsonAsync("/pets", new { name = "Rex", speciesId = _factory.Seed.SpeciesId, tutorId });
        return (await ApiFactory.ReadJsonAsync(response)).GetProperty("id").GetInt64();
    }

    private async Task<long> CreateCouponAsync(HttpClient admin)
    {
        using var response = await admin.PostAsJsonAsync("/coupons", new { code = $"CUPOM-{Guid.NewGuid():N}"[..14], templateId = _factory.Seed.CouponTemplateId, expirationDate = DateTime.UtcNow.AddDays(30) });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ApiFactory.ReadJsonAsync(response)).GetProperty("id").GetInt64();
    }

    private async Task<JsonElement> CompleteEventAsync(HttpClient client, long petId)
    {
        using var response = await client.PostAsJsonAsync("/health-events", new
        {
            petId, eventTypeId = _factory.Seed.EventTypeId, clinicId = _factory.Seed.ClinicId,
            eventDate = DateTime.UtcNow.Date, status = "REALIZADO", description = "Vacina anual"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ApiFactory.ReadJsonAsync(response);
    }

    [Fact]
    public async Task EventoRealizado_ComPlanoPremium_DeveConcederPontosEAcumularSaldo()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(client, tutorId);
        using var subscription = await client.PostAsJsonAsync("/subscriptions", new { petId, planId = _factory.Seed.PlanId, startDate = DateTime.UtcNow.Date, status = "ATIVO" });

        // Act
        var healthEvent = await CompleteEventAsync(client, petId);
        var balance = await ApiFactory.ReadJsonAsync(await client.GetAsync($"/tutors/{tutorId}/points"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, subscription.StatusCode);
        Assert.Equal(90, healthEvent.GetProperty("pointsAwarded").GetInt32());
        Assert.Equal(90, balance.GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task EventoAgendadoAtualizadoParaRealizado_DeveConcederPontosUmaUnicaVez()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(client, tutorId);
        var payload = new { petId, eventTypeId = _factory.Seed.EventTypeId, eventDate = DateTime.UtcNow.Date, status = "AGENDADO" };
        var created = await ApiFactory.ReadJsonAsync(await client.PostAsJsonAsync("/health-events", payload));
        var id = created.GetProperty("id").GetInt64();
        var done = new { payload.petId, payload.eventTypeId, payload.eventDate, status = "REALIZADO" };

        // Act
        var first = await ApiFactory.ReadJsonAsync(await client.PutAsJsonAsync($"/health-events/{id}", done));
        var second = await ApiFactory.ReadJsonAsync(await client.PutAsJsonAsync($"/health-events/{id}", done));
        var balance = await ApiFactory.ReadJsonAsync(await client.GetAsync($"/tutors/{tutorId}/points"));

        // Assert
        Assert.Equal(45, first.GetProperty("pointsAwarded").GetInt32());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("pointsAwarded").ValueKind);
        Assert.Equal(45, balance.GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task ResgateDeCupom_DeveExigirSaldoDebitarPontosEImpedirReuso()
    {
        // Arrange
        var admin = await _factory.AdminClientAsync();
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(client, tutorId);
        var couponId = await CreateCouponAsync(admin);

        // Act — 45 pontos: insuficiente (cupom exige 100)
        await CompleteEventAsync(client, petId);
        using var insufficient = await client.PostAsJsonAsync("/redeems", new { tutorId, couponId });

        // Act — 90 pontos: ainda insuficiente; 135: suficiente
        await CompleteEventAsync(client, petId);
        await CompleteEventAsync(client, petId);
        using var redeemed = await client.PostAsJsonAsync("/redeems", new { tutorId, couponId });
        var redeem = await ApiFactory.ReadJsonAsync(redeemed);
        var balance = await ApiFactory.ReadJsonAsync(await client.GetAsync($"/tutors/{tutorId}/points"));
        var coupon = await ApiFactory.ReadJsonAsync(await client.GetAsync($"/coupons/{couponId}"));
        using var again = await client.PostAsJsonAsync("/redeems", new { tutorId, couponId });

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, insufficient.StatusCode);
        Assert.Equal(HttpStatusCode.Created, redeemed.StatusCode);
        Assert.Equal(100, redeem.GetProperty("pointsUsed").GetInt32());
        Assert.Equal(35, balance.GetProperty("balance").GetInt32());
        Assert.Equal(100, balance.GetProperty("totalRedeemed").GetInt32());
        Assert.Equal("RESGATADO", coupon.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
    }

    [Fact]
    public async Task Resgate_ParaOutroTutor_DeveRetornar403()
    {
        // Arrange
        var admin = await _factory.AdminClientAsync();
        var couponId = await CreateCouponAsync(admin);
        var (clientA, _, _) = await _factory.RegisterTutorAsync();
        var (_, idB, _) = await _factory.RegisterTutorAsync();

        // Act
        using var response = await clientA.PostAsJsonAsync("/redeems", new { tutorId = idB, couponId });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cupom_ComExpiracaoNoPassado_DeveRetornar400()
    {
        // Arrange
        var admin = await _factory.AdminClientAsync();

        // Act
        using var response = await admin.PostAsJsonAsync("/coupons", new { code = "VENCIDO", templateId = _factory.Seed.CouponTemplateId, expirationDate = DateTime.UtcNow.AddDays(-2) });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cupom_ComStatusInvalidoNaQuery_DeveRetornar400()
    {
        // Arrange
        var admin = await _factory.AdminClientAsync();
        var couponId = await CreateCouponAsync(admin);

        // Act
        using var response = await admin.PutAsync($"/coupons/{couponId}/status?status=ACTIVE", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cupom_DisponivelDeveExporLinkDeResgate_ETutorNaoVeLinksAdministrativos()
    {
        // Arrange
        var admin = await _factory.AdminClientAsync();
        var couponId = await CreateCouponAsync(admin);
        var (client, _, _) = await _factory.RegisterTutorAsync();

        // Act
        var asTutor = await ApiFactory.ReadJsonAsync(await client.GetAsync($"/coupons/{couponId}"));
        var asAdmin = await ApiFactory.ReadJsonAsync(await admin.GetAsync($"/coupons/{couponId}"));

        // Assert
        var tutorRels = asTutor.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()).ToList();
        var adminRels = asAdmin.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()).ToList();
        Assert.Contains("redeem", tutorRels);
        Assert.DoesNotContain("delete", tutorRels);
        Assert.Contains("delete", adminRels);
    }

    [Fact]
    public async Task Assinatura_StatusCanceladaParaAtiva_DeveRetornar422()
    {
        // Arrange
        var (client, tutorId, _) = await _factory.RegisterTutorAsync();
        var petId = await CreatePetAsync(client, tutorId);
        var created = await ApiFactory.ReadJsonAsync(await client.PostAsJsonAsync("/subscriptions",
            new { petId, planId = _factory.Seed.PlanId, startDate = DateTime.UtcNow.Date, status = "ATIVO" }));
        var id = created.GetProperty("id").GetInt64();

        // Act
        using var cancel = await client.PutAsync($"/subscriptions/{id}/status?status=CANCELADO", null);
        using var reactivate = await client.PutAsync($"/subscriptions/{id}/status?status=ATIVO", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reactivate.StatusCode);
    }

    [Fact]
    public async Task Clinica_ExclusaoPeloAdmin_DeveRemoverPlanosEAssinaturasRelacionados()
    {
        // Arrange
        var admin = await _factory.AdminClientAsync();
        var clinic = await ApiFactory.ReadJsonAsync(await admin.PostAsJsonAsync("/clinics", new { name = "Temporária" }));
        var clinicId = clinic.GetProperty("id").GetInt64();
        var plan = await ApiFactory.ReadJsonAsync(await admin.PostAsJsonAsync("/plans", new { name = "Plano X", price = 50, durationDays = 30, pointsPerEvent = 1, clinicId }));

        // Act
        using var delete = await admin.DeleteAsync($"/clinics/{clinicId}");
        using var planAfter = await admin.GetAsync($"/plans/{plan.GetProperty("id").GetInt64()}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, planAfter.StatusCode);
    }
}
