FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY DartsLeaderboard.sln ./
COPY src/ ./src/
RUN dotnet restore src/DartsLeaderboard.Web/DartsLeaderboard.Web.csproj
RUN dotnet publish src/DartsLeaderboard.Web/DartsLeaderboard.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN adduser --disabled-password --no-create-home --uid 10001 darts
COPY --from=build /app/publish ./
USER darts
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DartsLeaderboard.Web.dll"]
