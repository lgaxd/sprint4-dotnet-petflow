using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PetFlowAPI.Domain.Entities;
using PetFlowAPI.Domain.Repositories;

namespace PetFlowAPI.Infrastructure.Mongo;

public sealed class MongoSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "petflow";
    public string PetNotesCollection { get; set; } = "pet_notes";
    public int ServerSelectionTimeoutSeconds { get; set; } = 5;
}

/// <summary>Acesso ao banco MongoDB (cliente thread-safe, registrado como singleton).</summary>
public sealed class MongoContext
{
    static MongoContext()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(PetNote))) return;

        BsonClassMap.RegisterClassMap<PetNote>(map =>
        {
            map.AutoMap();
            map.MapIdMember(n => n.Id)
                .SetSerializer(new StringSerializer(BsonType.ObjectId))
                .SetIdGenerator(StringObjectIdGenerator.Instance);
            map.SetIgnoreExtraElements(true);
        });
    }

    public MongoContext(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(settings.ServerSelectionTimeoutSeconds);

        Client = new MongoClient(clientSettings);
        Database = Client.GetDatabase(settings.Database);
        PetNotes = Database.GetCollection<PetNote>(settings.PetNotesCollection);
    }

    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }
    public IMongoCollection<PetNote> PetNotes { get; }
}

public sealed class MongoPetNoteRepository : IPetNoteRepository
{
    private readonly IMongoCollection<PetNote> _collection;

    public MongoPetNoteRepository(MongoContext context) => _collection = context.PetNotes;

    public Task AddAsync(PetNote note, CancellationToken cancellationToken = default)
        => _collection.InsertOneAsync(note, cancellationToken: cancellationToken);

    public async Task<PetNote?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        return await _collection.Find(n => n.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<PetNote>> GetByPetAsync(long petId, string? tag, PageRequest page, CancellationToken cancellationToken = default)
    {
        var builder = Builders<PetNote>.Filter;
        var filter = builder.Eq(n => n.PetId, petId);
        if (!string.IsNullOrWhiteSpace(tag)) filter &= builder.AnyEq(n => n.Tags, tag);

        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var sort = string.Equals(page.SortBy, "Title", StringComparison.OrdinalIgnoreCase)
            ? (page.Descending ? Builders<PetNote>.Sort.Descending(n => n.Title) : Builders<PetNote>.Sort.Ascending(n => n.Title))
            : (page.Descending ? Builders<PetNote>.Sort.Descending(n => n.CreatedAt) : Builders<PetNote>.Sort.Ascending(n => n.CreatedAt));

        var items = await _collection.Find(filter).Sort(sort).Skip(page.Page * page.Size).Limit(page.Size).ToListAsync(cancellationToken);
        return new PagedResult<PetNote>(items, page.Page, page.Size, total);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return false;
        var result = await _collection.DeleteOneAsync(n => n.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task DeleteByPetAsync(long petId, CancellationToken cancellationToken = default)
        => await _collection.DeleteManyAsync(n => n.PetId == petId, cancellationToken);
}

/// <summary>Cria os índices da coleção na inicialização (falha de conexão não impede a API de subir).</summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly MongoContext _context;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(MongoContext context, ILogger<MongoIndexInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Em segundo plano: a API não deve aguardar (nem falhar) por causa do MongoDB na inicialização.
        _ = Task.Run(() => EnsureIndexesAsync(cancellationToken), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var indexes = _context.PetNotes.Indexes;
            await indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<PetNote>(Builders<PetNote>.IndexKeys.Ascending(n => n.PetId).Descending(n => n.CreatedAt)),
                new CreateIndexModel<PetNote>(Builders<PetNote>.IndexKeys.Ascending(n => n.Tags))
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível criar os índices do MongoDB na inicialização");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class MongoHealthCheck : IHealthCheck
{
    private readonly MongoContext _context;

    public MongoHealthCheck(MongoContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB respondeu ao ping.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB indisponível.", ex);
        }
    }
}
