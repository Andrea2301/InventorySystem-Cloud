# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InventorySystemCloud.Api/InventorySystemCloud.Api.csproj", "src/InventorySystemCloud.Api/"]
COPY ["src/InventorySystemCloud.Application/InventorySystemCloud.Application.csproj", "src/InventorySystemCloud.Application/"]
COPY ["src/InventorySystemCloud.Domain/InventorySystemCloud.Domain.csproj", "src/InventorySystemCloud.Domain/"]
COPY ["src/InventorySystemCloud.Shared/InventorySystemCloud.Shared.csproj", "src/InventorySystemCloud.Shared/"]
COPY ["src/InventorySystemCloud.Infrastructure/InventorySystemCloud.Infrastructure.csproj", "src/InventorySystemCloud.Infrastructure/"]
RUN dotnet restore "./src/InventorySystemCloud.Api/InventorySystemCloud.Api.csproj"
COPY . .
WORKDIR "/src/src/InventorySystemCloud.Api"
RUN dotnet build "./InventorySystemCloud.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InventorySystemCloud.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventorySystemCloud.Api.dll"]