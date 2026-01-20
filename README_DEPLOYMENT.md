# Stock Viewer

這是一個股票監控系統，包含後端 .NET API 和前端 Next.js 應用。

## 系統架構

- **Backend**: .NET 9.0 Web API + PostgreSQL
- **Frontend**: Next.js + React
- **Worker**: .NET 背景工作者（爬蟲任務）
- **Database**: PostgreSQL

## 本地開發

### 前置條件

- Docker & Docker Compose
- .NET 9 SDK
- Node.js 18+

### 啟動步驟

1. 啟動 PostgreSQL:
```bash
docker-compose up -d
```

2. 啟動後端 API:
```bash
cd backend/StockViewer.Api
dotnet run
```
API 在 `http://localhost:5000`

3. 啟動前端:
```bash
cd frontend
npm install
npm run dev
```
前端在 `http://localhost:3000`

4. (可選) 啟動背景 Worker:
```bash
cd backend/StockViewer.Worker
dotnet run
```

## 部署到 Zeabur

### 準備

1. 初始化 Git 倉庫（如果還沒有）:
```bash
git init
git add .
git commit -m "Initial commit"
```

2. 推送到 GitHub/GitLab

### Zeabur 配置

1. 在 Zeabur 儀表板建立新服務
2. 連接你的 GitHub 倉庫
3. 配置環境變數:
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `DefaultConnection`: 使用 Zeabur PostgreSQL 連接字串

4. 設定 PostgreSQL 服務（如果沒有外部資料庫）

## 環境變數

- `DefaultConnection`: PostgreSQL 連接字串
- `ASPNETCORE_ENVIRONMENT`: `Development` 或 `Production`
- `ASPNETCORE_URLS`: 監聽的 URL (預設: http://+:5000)

## 專案結構

- `backend/` - .NET 後端
  - `StockViewer.Api/` - Web API
  - `StockViewer.Core/` - 核心邏輯和資料模型
  - `StockViewer.Worker/` - 背景爬蟲
- `frontend/` - Next.js 前端
- `docker-compose.yml` - 本地 PostgreSQL 配置
- `Dockerfile` - 多階段構建 Docker 映像
