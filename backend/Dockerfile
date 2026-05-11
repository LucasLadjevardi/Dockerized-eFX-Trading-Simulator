# =========================================
# Stage 1: Build the ASP.NET Core Application
# =========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /App/out

# =========================================
# Stage 2: Runtime
# =========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /App

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /App/out .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "EfxSimulator.Api.dll"]
