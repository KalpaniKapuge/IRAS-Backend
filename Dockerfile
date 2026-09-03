# Multi-stage build: compile with the full SDK, run on the much smaller ASP.NET runtime image.
# Build context must be the repo root (backend/IRAS) since IRAS.API references the other three
# projects as siblings — a Dockerfile living inside IRAS.API/ couldn't reach them.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, based only on the .csproj files — lets Docker cache this layer and skip a
# full package restore on every rebuild when only .cs files changed.
COPY IRAS.Domain/IRAS.Domain.csproj IRAS.Domain/
COPY IRAS.Infrastructure/IRAS.Infrastructure.csproj IRAS.Infrastructure/
COPY IRAS.Application/IRAS.Application.csproj IRAS.Application/
COPY IRAS.API/IRAS.API.csproj IRAS.API/
RUN dotnet restore IRAS.API/IRAS.API.csproj

COPY IRAS.Domain/ IRAS.Domain/
COPY IRAS.Infrastructure/ IRAS.Infrastructure/
COPY IRAS.Application/ IRAS.Application/
COPY IRAS.API/ IRAS.API/
RUN dotnet publish IRAS.API/IRAS.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render's default expected port for web services — see appsettings.Production.json for the
# rest of the runtime config (connection string, CORS origins, etc. set via the Render
# dashboard's environment variables instead, never committed).
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "IRAS.API.dll"]
