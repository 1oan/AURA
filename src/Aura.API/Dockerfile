FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Aura.slnx .
COPY src/Aura.Domain/Aura.Domain.csproj src/Aura.Domain/
COPY src/Aura.Application/Aura.Application.csproj src/Aura.Application/
COPY src/Aura.Infrastructure/Aura.Infrastructure.csproj src/Aura.Infrastructure/
COPY src/Aura.API/Aura.API.csproj src/Aura.API/
RUN dotnet restore

COPY src/ src/
RUN dotnet publish src/Aura.API/Aura.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Aura.API.dll"]
