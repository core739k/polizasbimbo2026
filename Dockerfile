# syntax=docker/dockerfile:1.7
# -------- build stage --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PolizasBimbo.slnx ./
COPY src/PolizasBimbo.Domain/*.csproj ./src/PolizasBimbo.Domain/
COPY src/PolizasBimbo.Application/*.csproj ./src/PolizasBimbo.Application/
COPY src/PolizasBimbo.Infrastructure/*.csproj ./src/PolizasBimbo.Infrastructure/
COPY src/PolizasBimbo.Web/*.csproj ./src/PolizasBimbo.Web/
RUN dotnet restore src/PolizasBimbo.Web/PolizasBimbo.Web.csproj

COPY src/ ./src/
RUN dotnet publish src/PolizasBimbo.Web/PolizasBimbo.Web.csproj -c Release -o /app /p:UseAppHost=false

# -------- runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PolizasBimbo.Web.dll"]
