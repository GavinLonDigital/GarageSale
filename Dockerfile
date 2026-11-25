
# Stage 1: Build and publish GarageSale
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore NuGet dependencies
COPY ["GarageSale.csproj", "."]
RUN dotnet restore "GarageSale.csproj"

# Copy source and publish the application
COPY . .
RUN dotnet publish "GarageSale.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Azure Container Apps ingress target port
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Copy the published Blazor application
COPY --from=build --chown=app:app /app/publish .

# Run as a non-root user
USER app

ENTRYPOINT ["dotnet", "GarageSale.dll"]
