# buildear app primero
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copiar la solución y proyectos para qe vengan con los paquetes descargados jeje
COPY ["siscomat.sln", "./"]
COPY ["Api/Siscomat.Api.csproj", "Api/"]
COPY ["Siscomat.Services/Siscomat.Services.csproj", "Siscomat.Services/"]
COPY ["Siscomat.Repository/Siscomat.Repositories.csproj", "Siscomat.Repository/"]
COPY ["Siscomat.Core/Siscomat.Core.csproj", "Siscomat.Core/"]

# restaurar dependencias
RUN dotnet restore "siscomat.sln"

COPY . .
WORKDIR "/src/Api"

# compilar
RUN dotnet build "Siscomat.Api.csproj" -c Release -o /app/build

# publicar app
FROM build AS publish
RUN dotnet publish "Siscomat.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ejecutar app en imagen más ligera
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT [ "dotnet", "Siscomat.Api.dll" ]