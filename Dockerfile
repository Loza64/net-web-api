FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Module.slnx ./
COPY Module.Api/Module.Api.csproj Module.Api/
COPY Module.Application/Module.Application.csproj Module.Application/
COPY Module.Domain/Module.Domain.csproj Module.Domain/
COPY Module.Infrastructure/Module.Infrastructure.csproj Module.Infrastructure/
RUN dotnet restore Module.Api/Module.Api.csproj

COPY . .
RUN dotnet publish Module.Api/Module.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 3500
USER $APP_UID

ENTRYPOINT ["dotnet", "Module.Api.dll"]
