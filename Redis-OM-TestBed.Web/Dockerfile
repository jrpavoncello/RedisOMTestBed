#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Redis-OM-TestBed/Redis-OM-TestBed.Web.csproj", "Redis-OM-TestBed/"]
RUN dotnet restore "./Redis-OM-TestBed/./Redis-OM-TestBed.Web.csproj"
COPY . .
WORKDIR "/src/Redis-OM-TestBed"
RUN dotnet build "./Redis-OM-TestBed.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Redis-OM-TestBed.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Redis-OM-TestBed.Web.dll"]