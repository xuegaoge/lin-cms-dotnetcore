# LinCMS .NET Core 后端开发指南

## 技术栈

- **.NET 10.0** - 核心框架
- **ASP.NET Core Web API** - Web API 框架
- **FreeSql** - ORM（支持 MySQL、SQL Server、PostgreSQL、Oracle、SQLite）
- **AutoMapper** - 对象映射
- **Serilog** - 结构化日志
- **JWT Bearer** - 身份认证
- **Swashbuckle** - Swagger/OpenAPI 文档
- **AutoFac** - 依赖注入
- **CAP** - 分布式事件总线

## 项目架构

### 清洁架构 + DDD 分层

```
src/
├── LinCms.Core/              # 领域层
│   ├── Entities/             # 实体
│   ├── ValueObjects/         # 值对象
│   ├── DomainServices/       # 领域服务
│   ├── Exceptions/           # 领域异常
│   ├── Enums/                # 枚举
│   └── Utils/                # 工具类
├── LinCms.Application.Contracts/  # 应用层接口
│   ├── Dtos/                 # 数据传输对象
│   ├── Services/             # 应用服务接口
│   └── Validators/           # 验证器
├── LinCms.Application/       # 应用层实现
│   ├── Services/             # 应用服务实现
│   ├── Handlers/             # 事件处理器
│   ├── Mappings/             # AutoMapper 配置
│   └── Validators/           # 验证实现
├── LinCms.Infrastructure/    # 基础设施层
│   ├── Data/                 # 数据访问
│   ├── Repositories/         # 仓储实现
│   ├── External/             # 外部服务
│   └── Configurations/       # 配置
└── LinCms.Web/               # 表现层
    ├── Controllers/          # 控制器
    ├── Middleware/           # 中间件
    ├── Models/               # 请求/响应模型
    └── Configurations/       # 配置
test/LinCms.Test/             # 单元测试
```

## 快速开始

### 环境要求
- .NET 10.0 SDK
- Visual Studio 2022 或 JetBrains Rider

### 构建与运行
```bash
# 还原依赖
dotnet restore

# 构建解决方案
dotnet build

# 开发模式运行（热重载）
dotnet run --project src/LinCms.Web

# 使用特定配置文件
dotnet run --project src/LinCms.Web --launch-profile "LinCms.Web.Development"

# 监视模式（自动重启）
dotnet watch --project src/LinCms.Web
```

### Docker 运行
```bash
# 构建镜像
docker build -f src/LinCms.Web/Dockerfile -t lincms-web .

# 运行容器
docker run -p 8080:8080 lincms-web

# 或使用脚本
./run.sh        # 拉取并运行镜像
./build_push.sh # 构建并推送到仓库
```

## 开发工作流

### 创建新功能

1. **添加实体**
   ```bash
   # 在 Core 层添加实体
   src/LinCms.Core/Entities/User.cs
   ```

2. **定义 DTO**
   ```bash
   # 在 Contracts 层定义 DTO
   src/LinCms.Application.Contracts/Dtos/UserDto.cs
   ```

3. **实现服务**
   ```bash
   # 在 Application 层实现业务逻辑
   src/LinCms.Application/Services/UserService.cs
   ```

4. **创建控制器**
   ```bash
   # 在 Web 层添加 API 控制器
   src/LinCms.Web/Controllers/UserController.cs
   ```

### 数据库迁移
```bash
# 添加迁移
dotnet ef migrations add MigrationName \
  -p src/LinCms.Infrastructure \
  -s src/LinCms.Web

# 更新数据库
dotnet ef database update \
  -p src/LinCms.Infrastructure \
  -s src/LinCms.Web
```

## 测试

### 运行所有测试
```bash
dotnet test

# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"

# 运行特定测试项目
dotnet test test/LinCms.Test

# 详细输出
dotnet test --verbosity normal
```

### 测试框架
- **xUnit** - 测试框架
- **FluentAssertions** - 断言库
- **Microsoft.AspNetCore.TestHost** - 测试服务器
- **Xunit.DependencyInjection** - 依赖注入

### 测试类别
- 领域逻辑单元测试
- API 集成测试
- 仓储模式测试
- 服务层测试

## 配置

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultDB": "YourDatabaseConnectionString"
  },
  "JwtSettings": {
    "SecretKey": "YourSecretKey",
    "Issuer": "LinCMS",
    "Audience": "LinCMS",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "FreeSql": {
    "DataType": "Sqlite",
    "ConnectionString": "YourConnectionString",
    "AutoSyncStructure": true
  },
  "CAP": {
    "DefaultStorage": 0,
    "DefaultMessageQueue": 0
  }
}
```

### 环境配置
- `appsettings.Development.json` - 开发环境
- `appsettings.Production.json` - 生产环境

## 本地与服务器环境规范

本项目采用**本地代码 + 服务器部署**的架构模式，区分本地开发环境和生产服务器环境。

### 环境架构关系

```
┌─────────────────────────────────────────────────────────┐
│                    本地开发环境                         │
│  e:\study\lincms  (源代码仓库)                           │
│                                                          │
│  ├── lin-cms-dotnetcore/                                │
│  │   ├── src/LinCms.Web/          ← 源代码              │
│  │   ├── deploy/nginx.conf.example ← 配置示例           │
│  │   └── appsettings.json          ← 默认配置           │
│                                                          │
│  ├── lin-cms-vue/               ← 前端源码              │
│  └── lin-cms-vvlog/             ← 门户前端源码          │
│                                                          │
│  *注：本地不运行服务，仅提供代码支持                     │
└─────────────────────────────────────────────────────────┘
                            ↓ (git pull)
┌─────────────────────────────────────────────────────────┐
│                    生产服务器环境                       │
│  api.okyu.xyz / test.dylanlee.com                      │
│                                                          │
│  ├── 后端服务 (Docker/Systemd)                          │
│  │   ├── MySQL 数据库                                   │
│  │   ├── Redis 缓存                                    │
│  │   └── Nginx 代理                                    │
│                                                          │
│  ├── 前端服务                                           │
│  │   ├── 管理端: http://localhost:8080/cms             │
│  │   └── 门户端: http://localhost:8081                 │
│                                                          │
│  *注：所有服务和数据库运行在服务器上                     │
└─────────────────────────────────────────────────────────┘
```

### 本地开发环境职责

#### ✅ 应该做的

1. **代码分析和修改**
   - 分析问题根因
   - 提供解决方案
   - 修改源代码
   - 更新配置示例

2. **文档编写**
   - 编写部署指南
   - 提供配置示例
   - 说明问题修复过程

3. **配置示例提供**
   - Nginx 配置示例：`deploy/nginx.conf.example`
   - 应用配置说明
   - 环境变量配置

#### ❌ 不应该做的

1. **不要启动本地服务**
   - ❌ `dotnet run`
   - ❌ `npm run dev`
   - ❌ 连接数据库
   - ❌ 连接 Redis
   - ❌ 启动测试服务器

2. **不要自动验证**
   - ❌ 测试 API 接口
   - ❌ 验证登录功能
   - ❌ 检查数据库连接
   - ❌ 运行自动化测试

3. **不要修改生产配置**
   - ❌ 不修改 `appsettings.Production.json`
   - ❌ 不修改服务器环境变量
   - ❌ 不直接操作生产环境

### 生产服务器环境职责

#### 服务器上运行的服务

1. **后端服务**
   - 框架：ASP.NET Core 10.0
   - 端口：http://localhost:5000
   - 代理：api.okyu.xyz/cms/*
   - 数据库：MySQL
   - 缓存：Redis (127.0.0.1:6379)

2. **前端服务**
   - 管理端：http://localhost:8080/cms
   - 门户端：http://localhost:8081
   - 构建：生产环境优化

3. **代理服务**
   - Nginx (OpenResty)
   - CORS 处理
   - SSL 终止
   - 静态文件服务

#### 需要服务器管理员操作的

1. **服务管理**
   ```bash
   # 重启后端服务
   sudo systemctl restart lincms-web

   # 查看服务状态
   sudo systemctl status lincms-web

   # 查看应用日志
   sudo journalctl -u lincms-web -f
   ```

2. **数据库管理**
   ```bash
   # 备份数据库
   mysqldump -u root -p lincms > lincms_backup_$(date +%Y%m%d).sql

   # 重置数据库
   mysql -u root -p lincms < lincms_backup_YYYYMMDD.sql
   ```

3. **Redis 管理**
   ```bash
   # 启动 Redis
   redis-server --daemonize yes

   # 重启 Redis
   sudo systemctl restart redis

   # 检查 Redis 状态
   redis-cli ping
   ```

4. **Nginx 配置**
   ```bash
   # 测试配置
   sudo nginx -t

   # 重载配置
   sudo nginx -s reload
   ```

### 典型问题修复流程

#### 示例：修复登录验证码问题

**第 1 步：本地分析（Claude 的职责）**
```bash
# 本地代码分析
分析源码 → 找到验证码配置 → 确定解决方案
```

**第 2 步：提供修复方案（Claude 的职责）**
```bash
# 修改 appsettings.json.example
"LoginCaptcha": {
  "Enabled": true,
  "Salt": "salt"
}

# 或者修改配置示例文档
说明：需要服务器管理员在 appsettings.Production.json 中启用验证码
```

**第 3 步：服务器操作（用户的职责）**
```bash
# 用户手动操作
git pull origin main
# 编辑服务器上的 appsettings.Production.json
sudo systemctl restart lincms-web
# 测试验证码功能
```

**第 4 步：用户验证（用户的职责）**
```bash
# 用户在浏览器中测试
访问 http://localhost:8080/cms/#/login
# 确认验证码显示并正常工作
```

### 关键配置文件说明

#### 本地配置文件

| 文件 | 用途 | 说明 |
|------|------|------|
| `src/LinCms.Web/appsettings.json` | 默认配置 | 本地开发环境配置，不应直接修改 |
| `deploy/nginx.conf.example` | Nginx 配置示例 | 完整的 CORS 配置，用户需复制到服务器 |
| `CLAUDE.md` | 项目协作指南 | 本地与服务器环境关系说明 |

#### 服务器配置文件

| 文件 | 用途 | 说明 |
|------|------|------|
| `/www/lincmsdotnet/appsettings.Production.json` | 生产配置 | 服务器上实际使用的配置文件 |
| `/etc/nginx/sites-available/lincms` | Nginx 配置 | 服务器上的实际 Nginx 配置 |
| `/etc/systemd/system/lincms-web.service` | 服务配置 | 后端服务的 Systemd 配置 |

### 重要提醒

#### 对于开发者（Claude）

1. **专注代码分析**
   - 分析问题根因
   - 提供解决方案
   - 编写配置示例

2. **不启动本地服务**
   - 不连接数据库
   - 不连接 Redis
   - 不验证登录功能

3. **提供明确的操作指南**
   - 说明需要在服务器上做的操作
   - 提供具体的配置示例
   - 给出验证方法

#### 对于服务器管理员（用户）

1. **手动部署**
   - 拉取代码后手动部署
   - 更新配置文件
   - 重启服务

2. **验证功能**
   - 在浏览器中测试
   - 查看服务器日志
   - 确认问题解决

3. **备份重要数据**
   - 数据库备份
   - 配置文件备份
   - 重要操作前备份

## API 文档

- **Swagger UI**：https://localhost:5001/swagger/index.html
- **API Base URL**：https://localhost:5001

### API 分组
- `/swagger/cms` - 管理域 API（需认证）
- `/swagger/blog` - 博客域 API（公开）
- `/swagger/v1` - 通用 API（版本化）

## 核心功能

### 认证与授权
- JWT Bearer 认证
- 基于角色的访问控制（RBAC）
- 当前用户服务（ICurrentUser）

### 日志系统
- Serilog 结构化日志
- 控制台和文件输出
- 不同级别的日志记录

### 数据访问
- FreeSql ORM
- 支持多种数据库
- 仓储模式实现
- 审计功能

### 事件总线
- CAP 分布式事件
- OutBox 模式
- InMemory 存储（避免 MySQL 依赖）

### 文件管理
- 静态文件托管
- 文件上传/下载
- 图片处理（可选）

### 视频下载
- yt-dlp 集成
- Sidecar 架构（FastAPI）
- 异步任务处理

## 部署

### Azure DevOps 管道
- Pipeline: `azure-pipelines.yml`
- Ubuntu 最新版本构建
- Docker 镜像构建
- 推送到阿里云容器镜像服务

### 手动部署
```bash
cd lin-cms-dotnetcore
./build_push.sh  # 构建并推送到仓库
./run.sh         # 拉取并运行容器
```

### 部署注意事项

#### 1. CORS 配置
前端访问后端 API 时可能出现跨域错误，需正确配置 Nginx 或后端 CORS 策略。

**配置文件位置**：
- `deploy/nginx.conf.example` - Nginx CORS 配置示例

**应用配置**：
```bash
# 1. 备份当前配置
sudo cp /path/to/current.conf /path/to/current.conf.backup

# 2. 复制示例配置
sudo cp deploy/nginx.conf.example /etc/nginx/sites-available/your-site

# 3. 测试配置
sudo nginx -t

# 4. 重载配置
sudo nginx -s reload
```

**关键配置点**：
- ✅ 允许 `tag` 请求头（验证码签名）
- ✅ 允许 `X-Request-Id` 请求头
- ✅ 正确处理 OPTIONS 预检请求
- ✅ 设置 `Access-Control-Expose-Headers`
- ✅ 允许凭据传输（credentials）

#### 2. 常见 CORS 错误
- **错误**：`CORS policy: The 'Access-Control-Allow-Origin' header`
- **解决**：确保 Nginx 配置了正确的 Origin 和允许的请求头

- **错误**：`Request header field tag is not allowed`
- **解决**：将 `tag` 添加到 `Access-Control-Allow-Headers` 中

- **错误**：`Response to preflight request doesn't pass`
- **解决**：确保 OPTIONS 请求返回正确的 CORS 头部

#### 3. 前后端集成验证
部署后可通过以下方式验证：
1. 访问前端管理端：http://localhost:8080
2. 检查浏览器控制台（无 CORS 错误）
3. 测试登录功能（可能需要验证码）

**完整部署流程**：
```bash
# 1. 部署后端
cd lin-cms-dotnetcore
docker build -f src/LinCms.Web/Dockerfile -t lincms-web .
docker run -p 5001:8080 lincms-web

# 2. 配置 Nginx（包含 CORS）
sudo cp deploy/nginx.conf.example /etc/nginx/sites-available/lincms
sudo ln -s /etc/nginx/sites-available/lincms /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# 3. 验证部署
curl -I http://your-domain/cms/user/login
# 应返回: Access-Control-Allow-Origin: http://localhost:8080
```

### Docker 镜像
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinCms.Web.dll"]
```

## 最佳实践

### 代码规范
- 遵循 Microsoft C# 编码规范
- 使用异步/等待模式
- 依赖注入优先
- 接口命名以 I 开头
- DTO 使用 PascalCase
- 私有字段使用 _camelCase

### 架构原则
- 清洁架构分层
- 领域驱动设计
- 单一职责原则
- 依赖倒置原则
- 避免跨层依赖

### 性能优化
- 数据库查询优化
- 懒加载和预加载
- 缓存策略
- 异步处理

### 安全
- 密码哈希存储（BCrypt/Argon2）
- JWT Token 管理
- CORS 配置
- 敏感数据保护

## 故障排除

- **端口冲突**：检查 `Properties/launchSettings.json`
- **数据库连接**：验证 `appsettings.json` 连接字符串
- **构建失败**：清理 `bin/obj` 文件夹
- **MySQL 报错**：设置 `CAP.DefaultStorage=0`（InMemory）
- **热重载问题**：使用 `dotnet watch`
- **依赖包问题**：运行 `dotnet restore`

### ❗ CORS 跨域错误（重要）

**确保 `appsettings.json` 中的 `WithOrigins` 配置同时包含 `localhost` 和 `127.0.0.1` 格式的地址**：

```json
{
  "WithOrigins": [
    "http://localhost:8080",
    "http://127.0.0.1:8080",
    "https://admin.okyu.xyz",
    "https://api.okyu.xyz"
  ]
}
```

**原因**：浏览器可能使用不同格式的地址（localhost vs 127.0.0.1）访问前端，导致 CORS 策略不匹配，触发跨域错误。

**验证方法**：检查后端日志中是否出现 `CORS policy execution successful`

## 相关资源

- **官方文档**：https://igeekfan.cn/dotnetcore/lin-cms/
- **API 文档**：https://api.igeekfan.cn/swagger/index.html
- **管理端演示**：https://cms.igeekfan.cn (admin/123qwe)
- **.NET 文档**：https://docs.microsoft.com/zh-cn/dotnet/
- **FreeSql 文档**：https://github.com/dotnetcore/FreeSql
