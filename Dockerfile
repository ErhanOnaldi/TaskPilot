FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TaskPilot.API/TaskPilot.API.csproj", "TaskPilot.API/"]
COPY ["TaskPilot.Application/TaskPilot.Application.csproj", "TaskPilot.Application/"]
COPY ["TaskPilot.Domain/TaskPilot.Domain.csproj", "TaskPilot.Domain/"]
COPY ["TaskPilot.Infrastructure/TaskPilot.Infrastructure.csproj", "TaskPilot.Infrastructure/"]
COPY ["TaskPilot.Persistence/TaskPilot.Persistence.csproj", "TaskPilot.Persistence/"]
RUN dotnet restore "TaskPilot.API/TaskPilot.API.csproj"
COPY . .
RUN dotnet publish "TaskPilot.API/TaskPilot.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TaskPilot.API.dll"]
