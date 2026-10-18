using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PetFlowAPI.Application.Interfaces;
using PetFlowAPI.HealthChecks;
using PetFlowAPI.Infrastructure.Mongo;
using PetFlowAPI.Infrastructure.Persistence;
using PetFlowAPI.Infrastructure.Security;
using PetFlowAPI.Middleware;
using PetFlowAPI.Security;

namespace PetFlowAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ServiceName = "PetFlowAPI";

    /// <summary>Controllers, serialização JSON, ProblemDetails e tratamento global de exceções.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var http = context.HttpContext;
            context.ProblemDetails.Instance ??= $"{http.Request.Method} {http.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] = http.TraceIdentifier;
            if (http.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId))
                context.ProblemDetails.Extensions["correlationId"] = correlationId;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    /// <summary>Autenticação JWT Bearer e autorização por perfil (ADMIN/TUTOR).</summary>
    public static IServiceCollection AddJwtAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configura o JwtBearer a partir de JwtOptions (resolvido tardiamente: permite sobrescrever em testes).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var options = jwt.Value;
                if (string.IsNullOrWhiteSpace(options.Key) || Encoding.UTF8.GetByteCount(options.Key) < 32)
                    throw new InvalidOperationException("Configure 'Jwt:Key' com pelo menos 32 caracteres (variável de ambiente Jwt__Key).");

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>CORS opcional: ativo apenas quando Cors:AllowedOrigins (lista separada por vírgula) estiver preenchido.</summary>
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = (configuration["Cors:AllowedOrigins"] ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (origins.Length == 0) return services;

        services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("X-Correlation-ID")));
        return services;
    }

    /// <summary>Swagger/OpenAPI com esquema de segurança Bearer e comentários XML.</summary>
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = ServiceName,
                Version = "v1",
                Description = "API REST de gerenciamento de saúde de pets — FIAP Challenge 2026 (Sprint 4) | Clyvo Vet. " +
                              "Autentique-se em POST /auth/login e informe o token no botão Authorize."
            });

            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml)) c.IncludeXmlComments(xml);

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Token JWT: informe apenas o token (o prefixo 'Bearer ' é adicionado automaticamente).",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
            });
        });
        return services;
    }

    /// <summary>Health checks (banco relacional, MongoDB e serviços externos) e telemetria OpenTelemetry.</summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddHealthChecks()
            .AddDbContextCheck<PetFlowContext>("oracle-database", tags: new[] { "ready", "database" })
            .AddCheck<MongoHealthCheck>("mongodb", tags: new[] { "ready", "database" })
            .AddCheck<ExternalServicesHealthCheck>("external-services", tags: new[] { "ready", "external" });

        var consoleExporter = configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter");
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                if (consoleExporter) tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddMeter(PetFlowMetrics.MeterName);
                if (consoleExporter) metrics.AddConsoleExporter();
            });
        return services;
    }
}

public static class PetFlowMetrics
{
    public const string MeterName = "PetFlowAPI.Metrics";
    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("petflow.http.request.duration", "ms", "Duração das requisições HTTP");
}
