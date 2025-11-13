FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app

EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["YtDlpService.csproj", "./"]
RUN dotnet restore "YtDlpService.csproj"
COPY . .
RUN dotnet build "YtDlpService.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "YtDlpService.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install Python3, pip, ffmpeg, and yt-dlp via pip
RUN apt-get update \
    && apt-get install -y python3 python3-pip ffmpeg \
    && pip3 install yt-dlp --break-system-packages

# Create downloads directory with permissions
RUN mkdir -p /downloads && chmod 777 /downloads

ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "YtDlpService.dll"]
