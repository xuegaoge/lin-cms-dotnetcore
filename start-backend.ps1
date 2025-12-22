# 后端启动脚本
Write-Host "正在启动 LinCMS 后端服务..." -ForegroundColor Green
Write-Host "API文档地址: https://localhost:5001/index.html" -ForegroundColor Cyan
Write-Host "Swagger地址: https://localhost:5001/swagger/index.html" -ForegroundColor Cyan
Write-Host ""

Set-Location "e:\work\选品管理\选品分析看板\lin-cms-dotnetcore"
dotnet run --project src/LinCms.Web/LinCms.Web.csproj
