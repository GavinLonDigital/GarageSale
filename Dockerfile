FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GarageSale.csproj", "."]
RUN dotnet restore "GarageSale.csproj"

COPY . .
RUN dotnet build "GarageSale.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GarageSale.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=publish /app/publish .

# Ensures the non-root user can read/write to the app folder if needed
RUN chown -R app:app /app

USER app
ENTRYPOINT ["dotnet", "GarageSale.dll"]