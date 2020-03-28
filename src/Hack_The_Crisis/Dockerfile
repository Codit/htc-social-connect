#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Hack_The_Crisis/Hack_The_Crisis.csproj", "Hack_The_Crisis/"]
RUN dotnet restore "Hack_The_Crisis/Hack_The_Crisis.csproj"
COPY . .
WORKDIR "/src/Hack_The_Crisis"
RUN dotnet build "Hack_The_Crisis.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hack_The_Crisis.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hack_The_Crisis.dll"]
