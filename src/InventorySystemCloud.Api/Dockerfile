# Etapa 1: Compilación
FROM ://microsoft.com AS build-env
WORKDIR /app

# Copiar el archivo de solución (.sln) si existe en la raíz
COPY *.sln ./

# Copiar TODOS los archivos .csproj de tus capas recreando sus carpetas
COPY src/InventorySystemCloud.Api/*.csproj ./src/InventorySystemCloud.Api/
COPY src/InventorySystemCloud.Infrastructure/*.csproj ./src/InventorySystemCloud.Infrastructure/
# NOTA: Si tienes capas adicionales como .Application o .Core, agrega una línea igual para ellas aquí:
# COPY src/InventorySystemCloud.Application/*.csproj ./src/InventorySystemCloud.Application/

# Restaurar dependencias de toda la solución
RUN dotnet restore

# Copiar absolutamente todo el código restante del repositorio
COPY . ./

# Moverse al directorio del proyecto ejecutable y compilar
WORKDIR /app/src/InventorySystemCloud.Api
RUN dotnet publish -c Release -o /out

# Etapa 2: Entorno de ejecución (Runtime)
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /out .

# Configurar el puerto dinámico requerido por Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "InventorySystemCloud.Api.dll"]
