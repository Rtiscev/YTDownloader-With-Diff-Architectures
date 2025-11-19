# 📈 YouTube Downloader – Beszel Monitoring Feature Branch

This branch introduces advanced application observability using **Beszel** (monitoring/metrics) and **K6** (load testing), extending the monolithic three-tier architecture.

---

## 🏗️ Architecture Overview

| Layer        | Technology                      | Purpose                |
|--------------|------------------------------- |------------------------|
| **Frontend** | Next.js                        | UI                     |
| **Backend**  | ASP.NET Core Web API           | Business logic, downloads |
| **Database & Storage** | PostgreSQL (users/downloads), MinIO (files) | Persistence, file storage |
| **Monitoring/Testing** | Beszel, K6 | Observability, dashboards, load testing |

---

## 🔥 Monitoring & Observability

### **Beszel**
- Integrated for **deep service monitoring** and metrics collection
- Instruments API routes, DB ops, and service health
- Beszel metrics shown in Prometheus/Grafana dashboards
- Configured via `docker-compose.yml` and `/loadtests`
- Endpoints expose health and metrics for scraping

### **K6 Load Testing**
- Runs performance/load tests via `/loadtests/test.js`
- Results exported to Prometheus and visualized in Grafana
- **Launch K6:**  
