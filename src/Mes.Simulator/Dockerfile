# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/Mes.Simulator/Mes.Simulator.csproj", "src/Mes.Simulator/"]
COPY ["src/Mes.Application/Mes.Application.csproj", "src/Mes.Application/"]
COPY ["src/Mes.Domain/Mes.Domain.csproj", "src/Mes.Domain/"]
COPY ["Directory.Build.props", "."]

RUN dotnet restore "src/Mes.Simulator/Mes.Simulator.csproj"

COPY . .

RUN dotnet publish "src/Mes.Simulator/Mes.Simulator.csproj" -c Release -o /app/publish


# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Mes.Simulator.dll"]
