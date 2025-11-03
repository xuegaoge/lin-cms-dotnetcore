# 部署配置目录

此目录包含 LinCMS 后端的部署相关配置文件。

## 文件说明

### nginx.conf.example
完整的 Nginx 配置文件示例，包含：
- SSL/HTTPS 配置
- 代理设置
- **完整的 CORS 配置**（解决跨域问题）
- WebSocket 支持

#### 关键特性
- ✅ 支持 `tag` 请求头（验证码签名）
- ✅ 支持 `X-Request-Id` 请求头
- ✅ 正确处理 OPTIONS 预检请求
- ✅ 暴露必要的响应头

#### 使用方法
```bash
# 1. 备份当前配置
sudo cp /etc/nginx/sites-available/current.conf /etc/nginx/sites-available/current.conf.backup

# 2. 复制配置文件
sudo cp nginx.conf.example /etc/nginx/sites-available/lincms

# 3. 测试配置
sudo nginx -t

# 4. 重载配置
sudo nginx -s reload
```

## 更多信息

详细说明请参阅：
- [../CLAUDE.md](../CLAUDE.md) - 部署注意事项章节
- 项目文档：https://igeekfan.cn/dotnetcore/lin-cms/

## CORS 问题解决

如果遇到跨域错误，请检查：
1. Nginx 配置是否包含 `tag` 请求头
2. OPTIONS 预检请求是否返回正确的 CORS 头部
3. 前端地址和 `Access-Control-Allow-Origin` 是否匹配

详见 `nginx.conf.example` 文件顶部的注释。
