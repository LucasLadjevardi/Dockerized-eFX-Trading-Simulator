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

COPY --from=build /App/out .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EfxSimulator.Api.dll"]