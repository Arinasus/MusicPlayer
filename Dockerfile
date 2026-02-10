FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MusicStore.csproj .
RUN dotnet restore --source "https://api.nuget.org/v3/index.json"

COPY . .
RUN dotnet publish MusicStore.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENTRYPOINT ["dotnet", "MusicStore.dll"]
