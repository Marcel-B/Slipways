#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Slipways.DataProvider/Slipways.DataProvider.csproj", "Slipways.DataProvider/"]
COPY ["NuGet.config", "Slipways.DataProvider/"]

RUN dotnet restore "Slipways.DataProvider/Slipways.DataProvider.csproj" --configfile ./Slipways.DataProvider/NuGet.config
COPY . .
WORKDIR "/src/Slipways.DataProvider"
RUN dotnet build "Slipways.DataProvider.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Slipways.DataProvider.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Slipways.DataProvider.dll"]