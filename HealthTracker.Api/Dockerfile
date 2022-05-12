FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["HealthTracker.Api/HealthTracker.Api.csproj", "HealthTracker.Api/"]
RUN dotnet restore "HealthTracker.Api/HealthTracker.Api.csproj"
COPY . .
WORKDIR "/src/HealthTracker.Api"
RUN dotnet build "HealthTracker.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HealthTracker.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HealthTracker.Api.dll"]
