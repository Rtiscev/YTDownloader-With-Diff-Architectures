# YouTube Downloader - Microservices Architecture

A full-stack YouTube downloader application built with a microservices architecture pattern using ASP.NET Core, Next.js, and Docker.

## 🏗️ Architecture Overview

This project demonstrates a microservices architecture where each service is independently deployable, scalable, and maintainable. The system is composed of multiple specialized services that communicate over a private Docker network.

### Core Services

- **API Gateway** (Ocelot) - Central entry point that routes requests to appropriate microservices
- **Auth Service** - Handles user authentication, authorization, and JWT token management
- **YtDlp Service** - Manages YouTube video download operations using yt-dlp
- **Minio Service** - Handles file storage operations and interacts with MinIO object storage
- **Frontend** (Next.js) - User interface built with React and TypeScript

### Infrastructure Services

- **PostgreSQL** - Relational database for user data and authentication
- **MinIO** - S3-compatible object storage for downloaded media files

## 🚀 Getting Started

### Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine + Docker Compose (Linux)
- Git
- At least 4GB of available RAM
- Ports available: 3000, 5000, 5432, 9000, 9001, 9090, 3001

### Environment Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/Rtiscev/YTDownloader-With-Diff-Architectures.git
   cd YTDownloader-With-Diff-Architectures
   git checkout microservices
   ```

2. **Create environment file**
   
   Copy the `.env.copy` file to `.env` and configure your environment variables:
   ```bash
   cp .env.copy .env
   ```

   Key environment variables:
   ```env
   # Frontend
   FRONTEND_PORT=3000
   
   # Database
   POSTGRES_USER=your_user
   POSTGRES_PASSWORD=your_password
   POSTGRES_DB=authdb
   POSTGRES_CONNECTION_STRING=Host=authdb;Database=authdb;Username=your_user;Password=your_password
   
   # JWT Configuration
   JWT_ISSUER=YourIssuer
   JWT_AUDIENCE=YourAudience
   JWT_SECRET=your-super-secret-key-min-32-chars
   
   # Admin Account
   ADMIN_EMAIL=admin@example.com
   ADMIN_PASSWORD=YourSecurePassword123!
   
   # MinIO
   MINIO_ROOT_USER=minioadmin
   MINIO_ROOT_PASSWORD=minioadmin123
   MINIO_ENDPOINT=minio:9000
   MINIO_API_PORT=9000:9000
   MINIO_CONSOLE_PORT=9001:9001
   
   # Monitoring
   GRAFANA_USER=admin
   GRAFANA_PASSWORD=admin
   
   # Load Testing
   K6_VUS=10
   K6_DURATION=30s
   ```

3. **Start the application**
   ```bash
   docker-compose up -d
   ```

4. **Verify services are running**
   ```bash
   docker-compose ps
   ```

## 📦 Service Details

### API Gateway (Port: 5000)
- **Technology**: ASP.NET Core with Ocelot
- **Purpose**: Routes requests to microservices, handles request aggregation
- **Key Features**:
  - JWT authentication validation
  - Request routing and load balancing
  - Rate limiting and throttling
  - API composition

### Auth Service
- **Technology**: ASP.NET Core, Entity Framework Core
- **Database**: PostgreSQL
- **Purpose**: User management and authentication
- **Endpoints**:
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login` - User login (returns JWT)
  - `GET /api/auth/me` - Get current user info

### YtDlp Service
- **Technology**: ASP.NET Core with yt-dlp
- **Purpose**: Download videos from YouTube and other platforms
- **Features**:
  - Video/audio download
  - Format selection
  - Quality options
  - Download progress tracking

### Minio Service
- **Technology**: ASP.NET Core with MinIO SDK
- **Purpose**: File storage management
- **Features**:
  - Upload/download files
  - Bucket management
  - Presigned URL generation
  - File metadata handling

### Frontend (Port: 3000)
- **Technology**: Next.js 14, React, TypeScript
- **Features**:
  - User authentication
  - Video URL input
  - Download management
  - File browsing
  - Responsive design

## 🔧 Development
### Accessing Services

- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:5000
- **PostgreSQL**: localhost:5432
- **MinIO Console**: http://localhost:9001
- **MinIO API**: http://localhost:9000
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3001

### Viewing Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f authservice
docker-compose logs -f ytdlpservice
```
