FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["netcoreapp.csproj", "netcoreapp/"]
RUN dotnet restore "netcoreapp/netcoreapp.csproj"
COPY . .
WORKDIR "/src/netcoreapp"
RUN dotnet build "netcoreapp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "netcoreapp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "netcoreapp.dll"]