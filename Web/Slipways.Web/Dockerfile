FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Slipways.Web/Slipways.Web.csproj", "Slipways.Web/"]
COPY ["NuGet.config", "NuGet.config"]

RUN dotnet restore "Slipways.Web/Slipways.Web.csproj" --configfile NuGet.config
COPY . .
WORKDIR "/src/Slipways.Web"
RUN dotnet build "Slipways.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Slipways.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Slipways.Web.dll"]