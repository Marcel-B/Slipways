FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Slipways.Domain/Slipways.Domain.csproj src/Slipways.Domain/
COPY src/Slipways.Infrastructure/Slipways.Infrastructure.csproj src/Slipways.Infrastructure/
COPY src/Slipways.Api/Slipways.Api.csproj src/Slipways.Api/
RUN dotnet restore src/Slipways.Api/Slipways.Api.csproj

COPY src/ src/
RUN dotnet publish src/Slipways.Api/Slipways.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# Volume mount points for the SQLite database and data protection keys, writable by the non-root app user.
RUN mkdir /data /keys && chown $APP_UID /data /keys
USER $APP_UID
# 8080 for the app, 9464 only for /metrics (see Monitoring:MetricsPort)
ENV ASPNETCORE_HTTP_PORTS="8080;9464;9465"
EXPOSE 8080 9464 9465
ENTRYPOINT ["dotnet", "Slipways.Api.dll"]
