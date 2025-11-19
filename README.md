# YouTube Downloader – Three-Tier Architecture

A full-stack YouTube downloader project using a classic three-tier (presentation, business, data) monolithic design with ASP.NET Core, Next.js, PostgreSQL, and MinIO—all orchestrated via Docker.

## 🏗️ Architecture Overview

This branch demonstrates a traditional layered architecture:
- **Frontend:** Next.js app (UI)
- **Backend:** Single ASP.NET Core Web API hosting all business logic, authentication, yt-dlp integration, file and database access
- **Database & Storage:** PostgreSQL (users/downloads) and MinIO (files)

All backend concerns are handled within one deployable app.

### Core Components

- **Backend Web API (ASP.NET Core):** Authentication, download management, file operations
- **Frontend (Next.js):** User interface
- **PostgreSQL:** Relational database for persistent app data
- **MinIO:** S3-compatible object storage for downloaded files

## 🚀 Getting Started

#### Prerequisites
- Docker Desktop or Engine + Compose
- Git
- 4GB+ RAM
- Ports: 3000 (frontend), 7000 (backend), 5432 (db), 9000/9001 (MinIO), 9090 (Prometheus), 3001 (Grafana)

#### Setup

1. **Clone the branch**
   ```bash
   git clone https://github.com/Rtiscev/YTDownloader-With-Diff-Architectures.git
   cd YTDownloader-With-Diff-Architectures
   git checkout three-tier
   ```

2. **Configure environment**
   ```bash
   cp .env.copy .env
   ```
   Edit `.env` for database, MinIO, JWT, monitoring credentials.

3. **Start up (Docker Compose)**
   ```bash
   docker-compose up -d
   ```

4. **Verify**
   ```bash
   docker-compose ps
   ```

## 📦 Component Details

### Backend Web API (Port: 7000)
- ASP.NET Core
- Handles all routes for auth, downloading (using yt-dlp), file serve, and DB ops
- JWT security
- Direct SQL and MinIO access

### Frontend (Port: 3000)
- Next.js / React / TypeScript
- Auth, input & download management, browsing files

### Storage & Monitoring
- **PostgreSQL:** DB (localhost:5432)
- **MinIO:** http://localhost:9001 (console), http://localhost:9000 (API)
- **Prometheus:** http://localhost:9090
- **Grafana:** http://localhost:3001

## 🔧 Development & Logs

- Hot reload and development mode available via Compose and Dockerfile triggers
- Access running containers, logs, and endpoints as above

## 🚦 How to Launch Backend and Frontend Applications (Local Development)

### 1. Launch Backend
Navigate to the `Backend` directory and run:
```bash
# Start backend API with hot reload for development
dotnet watch run
# Or regular run
cd Backend
dotnet run
```
Backend will start and listen on port 7000 by default.

### 2. Launch Frontend
Navigate to the `frontv2` directory and run:
```bash
# Install dependencies (first time only)
npm install
# Start Next.js frontend
dotnet run dev  # typo, correct:
npm run dev
```
Frontend runs at http://localhost:3000

### Environment Integration
- Frontend will send requests to backend API at http://localhost:7000 by default
- To use a different backend port, adjust `NEXT_PUBLIC_API_URL` in `.env` or your environment variables
- Both projects can use the shared `.env` file for database, MinIO, JWT, etc
