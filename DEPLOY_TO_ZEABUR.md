# 使用 Docker 部署到 Zeabur

## 前置步驟

1. **GitHub 帳戶**：確保你有 GitHub 帳戶
2. **Zeabur 帳戶**：在 [zeabur.com](https://zeabur.com) 註冊
3. **Docker Hub 帳戶**（可選）：用於儲存構建的 Docker 鏡像

## 部署方案

### 方案 1：使用 Zeabur 官方部署（推薦初期快速測試）

1. 推送代碼到 GitHub
2. 在 Zeabur 儀表板連接 GitHub 倉庫
3. 配置環境變數

### 方案 2：本地構建 Docker 並推送（推薦生產環境）

#### 步驟 1：構建 Docker 鏡像

```bash
# 構建後端 API
docker build -f Dockerfile --target api-runtime -t your-registry/stock-viewer-api:latest .

# 構建前端
cd frontend
docker build -t your-registry/stock-viewer-frontend:latest .
cd ..

# 構建 Worker（如果需要）
docker build -f Dockerfile --target worker-runtime -t your-registry/stock-viewer-worker:latest .
```

#### 步驟 2：推送到 Docker 鏡像倉庫

```bash
# 登錄 Docker Hub（或其他倉庫）
docker login

# 推送鏡像
docker push your-registry/stock-viewer-api:latest
docker push your-registry/stock-viewer-frontend:latest
docker push your-registry/stock-viewer-worker:latest
```

#### 步驟 3：在 Zeabur 部署

1. 進入 Zeabur 儀表板
2. 建立新服務
3. 選擇「Docker Image」
4. 輸入鏡像地址：`your-registry/stock-viewer-api:latest`
5. 配置環境變數

## 環境變數配置

### 後端 API / Worker
```
ASPNETCORE_ENVIRONMENT=Production
DefaultConnection=PostgreSQL連接字串
```

### 前端
```
NEXT_PUBLIC_API_URL=https://your-api-domain.zeabur.app
```

## Zeabur 環境變數設置

在 Zeabur 服務設置中添加：

**API 服務：**
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `DefaultConnection` = Zeabur PostgreSQL 連接字串

**前端服務：**
- `NEXT_PUBLIC_API_URL` = 後端 API 的 Zeabur 域名（例如 `https://api.zeabur.app`）

## 建立 Zeabur PostgreSQL

1. 在 Zeabur 儀表板新增服務
2. 選擇 PostgreSQL
3. 複製連接字串供後端使用

## 使用 docker-compose.prod.yml 在本地測試

```bash
# 複製環境變數模板
cp .env.example .env.production

# 啟動完整堆棧
docker-compose -f docker-compose.prod.yml up -d
```

## 常見問題

**Q: 部署後前端無法連接到 API**
A: 確認 `NEXT_PUBLIC_API_URL` 環境變數設置正確

**Q: 資料庫連接失敗**
A: 檢查 `DefaultConnection` 是否正確，確保 Zeabur PostgreSQL 已建立

**Q: Docker 鏡像過大**
A: 多階段構建已優化，.NET 映像應為 200-300MB

## 後續監控

- 查看 Zeabur 日誌：儀表板 → 服務 → 日誌
- 監控資源使用：儀表板 → 服務 → 統計資訊
- 設置告警通知

