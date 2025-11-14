FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

# Install Python3, pip, ffmpeg, and yt-dlp (Alpine uses apk)
RUN apk add --no-cache \
    python3 \
    py3-pip \
    ffmpeg \
    curl \
    && pip3 install --no-cache-dir yt-dlp --break-system-packages

# Create downloads directory
RUN mkdir -p /downloads && chmod 755 /downloads


FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy only project file first for better caching
COPY ["YtDlpService.csproj", "./"]
RUN dotnet restore "YtDlpService.csproj"

# Copy everything else
COPY . .
RUN dotnet build "YtDlpService.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "YtDlpService.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "YtDlpService.dll"]
