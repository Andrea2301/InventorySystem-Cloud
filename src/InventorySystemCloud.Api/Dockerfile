# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el archivo de solución si existe en la raíz
COPY *.sln ./

# Buscar y copiar todos los archivos .csproj sin importar el nivel de carpetas
COPY src/InventorySystemCloud.Api/InventorySystemCloud.Api.csproj src/InventorySystemCloud.Api/
COPY src/InventorySystemCloud.Application/InventorySystemCloud.Application.csproj src/InventorySystemCloud.Application/
COPY src/InventorySystemCloud.Domain/InventorySystemCloud.Domain.csproj src/InventorySystemCloud.Domain/
COPY src/InventorySystemCloud.Shared/InventorySystemCloud.Shared.csproj src/InventorySystemCloud.Shared/
COPY src/InventorySystemCloud.Infrastructure/InventorySystemCloud.Infrastructure.csproj src/InventorySystemCloud.Infrastructure/

# Restaurar las dependencias de la API principal y sus capas internas
RUN dotnet restore "src/InventorySystemCloud.Api/InventorySystemCloud.Api.csproj"

# Copiar el resto del código fuente del repositorio
COPY . .

# Compilar y publicar los binarios optimizados para producción en la carpeta /app/publish
RUN dotnet publish "src/InventorySystemCloud.Api/InventorySystemCloud.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Entorno de ejecución (Runtime) ligero para producción
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Forzar el puerto 10000 que requiere Render para tráfico HTTP libre
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "InventorySystemCloud.Api.dll"]
