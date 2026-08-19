# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/Mes.Api/Mes.Api.csproj", "src/Mes.Api/"]
COPY ["src/Mes.Application/Mes.Application.csproj", "src/Mes.Application/"]
COPY ["src/Mes.Domain/Mes.Domain.csproj", "src/Mes.Domain/"]
COPY ["src/Mes.Infrastructure/Mes.Infrastructure.csproj", "src/Mes.Infrastructure/"]
COPY ["Directory.Build.props", "."]

RUN dotnet restore "src/Mes.Api/Mes.Api.csproj"

COPY . .

RUN dotnet publish "src/Mes.Api/Mes.Api.csproj" -c Release -o /app/publish


# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Mes.Api.dll"]
