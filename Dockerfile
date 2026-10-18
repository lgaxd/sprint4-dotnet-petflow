# ── Build ──────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura primeiro (camada de cache): só os .csproj
COPY PetFlowAPI.Domain/PetFlowAPI.Domain.csproj                   PetFlowAPI.Domain/
COPY PetFlowAPI.Application/PetFlowAPI.Application.csproj         PetFlowAPI.Application/
COPY PetFlowAPI.Infrastructure/PetFlowAPI.Infrastructure.csproj   PetFlowAPI.Infrastructure/
COPY PetFlowAPI/PetFlowAPI.csproj                                 PetFlowAPI/
RUN dotnet restore PetFlowAPI/PetFlowAPI.csproj

COPY PetFlowAPI.Domain/         PetFlowAPI.Domain/
COPY PetFlowAPI.Application/    PetFlowAPI.Application/
COPY PetFlowAPI.Infrastructure/ PetFlowAPI.Infrastructure/
COPY PetFlowAPI/                PetFlowAPI/
RUN dotnet publish PetFlowAPI/PetFlowAPI.csproj -c Release -o /app/publish --no-restore

# ── Runtime ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PetFlowAPI.dll"]
