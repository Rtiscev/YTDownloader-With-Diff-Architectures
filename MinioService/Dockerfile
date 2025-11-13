# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["MinioService.csproj", "./"]
RUN dotnet restore "./MinioService.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "./MinioService.csproj" -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Set the entry point to run the app
ENTRYPOINT ["dotnet", "MinioService.dll"]
