# Multi-stage build for HireIQ Clean Architecture solution.
# Repo layout:
#   ./hireiq-backend.sln
#   ./src/HireIQ.Domain        (deps: -)
#   ./src/HireIQ.Application   (deps: Domain)
#   ./src/HireIQ.Infrastructure(deps: Application, Domain)
#   ./src/HireIQ.API           (deps: Application, Infrastructure, Domain)
#
# Build from repo root: docker build -t hireiq-backend .

# ---- restore stage (cache-friendly: only csprojs copied first) ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src
COPY hireiq-backend.sln ./
COPY src/HireIQ.Domain/HireIQ.Domain.csproj           src/HireIQ.Domain/
COPY src/HireIQ.Application/HireIQ.Application.csproj src/HireIQ.Application/
COPY src/HireIQ.Infrastructure/HireIQ.Infrastructure.csproj src/HireIQ.Infrastructure/
COPY src/HireIQ.API/HireIQ.API.csproj                 src/HireIQ.API/
RUN dotnet restore src/HireIQ.API/HireIQ.API.csproj

# ---- build stage ----
FROM restore AS build
COPY src/ src/
RUN dotnet publish src/HireIQ.API/HireIQ.API.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HireIQ.API.dll"]
