# Etapa 1: Compilación
FROM ://microsoft.com AS build
WORKDIR /app

# Copiar el archivo de solución (.sln) si existe
COPY *.sln ./

#  Busca todos los archivos .csproj en cualquier subcarpeta 
# y los copia al contenedor manteniendo la estructura para que funcione el restore
COPY **/InventorySystemCloud.Api.csproj ./src/InventorySystemCloud.Api/
COPY **/InventorySystemCloud.Infrastructure.csproj ./src/InventorySystemCloud.Infrastructure/
COPY **/InventorySystemCloud.Application.csproj ./src/InventorySystemCloud.Application/
COPY **/InventorySystemCloud.Domain.csproj ./src/InventorySystemCloud.Domain/
COPY **/InventorySystemCloud.Shared.csproj ./src/InventorySystemCloud.Shared/

# Restaurar dependencias usando un comodín flexible
RUN dotnet restore **/InventorySystemCloud.Api.csproj

# Copiar absolutamente todo el resto del código del repositorio
COPY . .

# Compilar y publicar buscando el archivo del proyecto ejecutable directamente
RUN dotnet publish **/InventorySystemCloud.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Entorno de ejecución para producción
FROM ://microsoft.com AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configurar puerto dinámico para Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "InventorySystemCloud.Api.dll"]
