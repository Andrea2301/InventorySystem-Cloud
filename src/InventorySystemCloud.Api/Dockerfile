
WORKDIR /app

COPY *.sln ./

COPY **/InventorySystemCloud.Api.csproj ./src/InventorySystemCloud.Api/
COPY **/InventorySystemCloud.Infrastructure.csproj ./src/InventorySystemCloud.Infrastructure/
COPY **/InventorySystemCloud.Application.csproj ./src/InventorySystemCloud.Application/
COPY **/InventorySystemCloud.Domain.csproj ./src/InventorySystemCloud.Domain/
COPY **/InventorySystemCloud.Shared.csproj ./src/InventorySystemCloud.Shared/

RUN dotnet restore **/InventorySystemCloud.Api.csproj

COPY . .

RUN dotnet publish **/InventorySystemCloud.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM ://microsoft.com AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "InventorySystemCloud.Api.dll"]
