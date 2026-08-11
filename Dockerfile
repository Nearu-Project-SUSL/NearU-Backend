# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["NearU_Backend_Revised.csproj", "./"]
RUN dotnet restore "NearU_Backend_Revised.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "NearU_Backend_Revised.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NearU_Backend_Revised.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage — chiseled: non-root, minimal footprint, reduced attack surface
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port — ASP.NET defaults to 8080 in .NET 8+
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NearU_Backend_Revised.dll"]
