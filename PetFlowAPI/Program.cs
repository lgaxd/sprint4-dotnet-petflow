using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Application;
using PetFlowAPI.Extensions;
using PetFlowAPI.HealthChecks;
using PetFlowAPI.Infrastructure;
using PetFlowAPI.Infrastructure.Persistence;
using PetFlowAPI.Middleware;
using Serilog;
using Serilog.Formatting.Compact;

// Carrega o .env (se existir) ANTES de criar o builder, para que as variáveis entrem na configuração.
DotEnvLoader.LoadFromDefaultLocations();

var builder = WebApplication.CreateBuilder(args);

// Logging estruturado: JSON compacto (console e arquivo diário); template legível apenas em desenvolvimento.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", ServiceCollectionExtensions.ServiceName);

    if (context.HostingEnvironment.IsDevelopment())
        configuration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}");
    else
        configuration.WriteTo.Console(new RenderedCompactJsonFormatter());

    if (!context.HostingEnvironment.IsEnvironment("Testing"))
        configuration.WriteTo.File(new RenderedCompactJsonFormatter(), "logs/petflow-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment())
    .AddApiServices()
    .AddJwtAuth()
    .AddConfiguredCors(builder.Configuration)
    .AddSwaggerDocs()
    .AddObservability(builder.Configuration);

var app = builder.Build();

// Aplica as migrações do EF Core na inicialização quando Database:ApplyMigrationsOnStartup = true.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<PetFlowContext>().Database.Migrate();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("UserId", httpContext.User.Identity?.IsAuthenticated == true ? httpContext.User.Identity.Name ?? "authenticated" : "anonymous");
    };
});
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PetFlow API v1"));

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = HealthResponseWriter.WriteAsync }).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = HealthResponseWriter.WriteAsync }).AllowAnonymous();

app.Run();

/// <summary>Ponto de entrada visível para os testes de integração (WebApplicationFactory).</summary>
public partial class Program { }
