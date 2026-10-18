using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Repositories;
using PetFlowAPI.Infrastructure.Mongo;
using PetFlowAPI.Infrastructure.Persistence;

namespace PetFlowAPI.Tests.Integration;

public sealed record SeedData(long SpeciesId, long EventTypeId, long ClinicId, long PlanId, long CouponTemplateId)
{
    public const string AdminEmail = "admin@petflow.test";
    public const string AdminPassword = "Admin@12345";
}

/// <summary>MongoDB simulado em memória (o teste de integração não depende de um servidor Mongo).</summary>
public sealed class FakePetNoteRepository : IPetNoteRepository
{
    private readonly List<PetNote> _notes = new();
    private int _sequence;

    public Task AddAsync(PetNote note, CancellationToken cancellationToken = default)
    {
        note.Id = (++_sequence).ToString("x24");
        lock (_notes) _notes.Add(note);
        return Task.CompletedTask;
    }

    public Task<PetNote?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_notes) return Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));
    }

    public Task<PagedResult<PetNote>> GetByPetAsync(long petId, string? tag, PageRequest page, CancellationToken cancellationToken = default)
    {
        lock (_notes)
        {
            var query = _notes.Where(n => n.PetId == petId && (tag is null || n.Tags.Contains(tag)));
            query = page.SortBy == "Title"
                ? (page.Descending ? query.OrderByDescending(n => n.Title) : query.OrderBy(n => n.Title))
                : (page.Descending ? query.OrderByDescending(n => n.CreatedAt) : query.OrderBy(n => n.CreatedAt));
            var list = query.ToList();
            return Task.FromResult(new PagedResult<PetNote>(list.Skip(page.Page * page.Size).Take(page.Size).ToList(), page.Page, page.Size, list.Count));
        }
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_notes) return Task.FromResult(_notes.RemoveAll(n => n.Id == id) > 0);
    }

    public Task DeleteByPetAsync(long petId, CancellationToken cancellationToken = default)
    {
        lock (_notes) _notes.RemoveAll(n => n.PetId == petId);
        return Task.CompletedTask;
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"PetFlowIntegration-{Guid.NewGuid():N}";

    public FakePetNoteRepository Notes { get; } = new();
    public SeedData Seed { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PetFlowContext>();
            services.RemoveAll<DbContextOptions<PetFlowContext>>();
            services.AddDbContext<PetFlowContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IPetNoteRepository>();
            services.AddSingleton<IPetNoteRepository>(Notes);

            var indexInitializer = services.Single(d => d.ImplementationType == typeof(MongoIndexInitializer));
            services.Remove(indexInitializer);

            // Sem MongoDB nos testes: o health check "mongodb" é coberto por teste unitário próprio.
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                var mongo = options.Registrations.Single(r => r.Name == "mongodb");
                options.Registrations.Remove(mongo);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        Seed = SeedDatabase(scope.ServiceProvider);
        return host;
    }

    private static SeedData SeedDatabase(IServiceProvider services)
    {
        var context = services.GetRequiredService<PetFlowContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();

        var species = new Species { Name = "Cachorro" };
        var eventType = new EventType { Name = "Vacina", PointsReward = 45, Category = "PREVENTIVO" };
        var clinic = new Clinic { Name = "Clínica VetVida", Cnpj = "12.345.678/0001-90" };
        context.AddRange(
            species, eventType, clinic,
            new RewardAction { Name = RewardAction.HealthEventActionName, PointsValue = 50 },
            new Tutor { Name = "Administrador", Email = SeedData.AdminEmail, Role = "ADMIN", PasswordHash = hasher.Hash(SeedData.AdminPassword) });
        context.SaveChanges();

        var plan = new Plan { ClinicId = clinic.Id, Name = "Premium", Price = 199.9m, DurationDays = 30, PointsPerEvent = 2 };
        var partner = new PartnerDiscount { ClinicId = clinic.Id, PartnerName = "PetShop", Category = "RACAO", DiscountPercent = 10 };
        context.AddRange(plan, partner);
        context.SaveChanges();

        var template = new CouponTemplate { PartnerDiscountId = partner.Id, Title = "10% em ração", DiscountType = "PERCENTUAL", DiscountValue = 10, PointsRequired = 100 };
        context.Add(template);
        context.SaveChanges();

        return new SeedData(species.Id, eventType.Id, clinic.Id, plan.Id, template.Id);
    }

    // ───────────── Helpers usados pelos testes ─────────────

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    public Task<HttpClient> AdminClientAsync() => LoginAsync(SeedData.AdminEmail, SeedData.AdminPassword);

    /// <summary>Cadastra um tutor novo (endpoint público) e devolve um cliente autenticado como ele.</summary>
    public async Task<(HttpClient Client, long TutorId, string Email)> RegisterTutorAsync()
    {
        var email = $"tutor-{Guid.NewGuid():N}@petflow.test";
        var anonymous = CreateClient();
        var response = await anonymous.PostAsJsonAsync("/tutors", new { name = "Tutor Teste", email, phone = "11999999999", password = "SenhaSegura123" });
        response.EnsureSuccessStatusCode();
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        return (await LoginAsync(email, "SenhaSegura123"), id, email);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();
}
