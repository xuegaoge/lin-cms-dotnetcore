using Autofac;
using LinCms.Cms.Account;
using LinCms.Cms.Files;
using LinCms.Cms.Users;
using LinCms.Entities;
using LinCms.Middleware;
using LinCms.Cms.VideoDownload;

namespace LinCms.Startup.Configuration;

/// <summary>
/// 注入Application层中的Service
/// </summary>
public class ServiceModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // 暂时注释掉 AOP 缓存拦截器，开发环境无需缓存
        // builder.RegisterType<AopCacheIntercept>();
        // builder.RegisterType<AopCacheAsyncIntercept>();
        
        //一个接口多个实现，使用Named，区分
        builder.RegisterType<LocalFileService>().Named<IFileService>(LinFile.LocalFileService).InstancePerLifetimeScope();
        builder.RegisterType<QiniuService>().Named<IFileService>(LinFile.QiniuService).InstancePerLifetimeScope();

        builder.RegisterType<GithubOAuth2Serivice>().Named<IOAuth2Service>(LinUserIdentity.GitHub).InstancePerLifetimeScope();
        builder.RegisterType<GiteeOAuth2Service>().Named<IOAuth2Service>(LinUserIdentity.Gitee).InstancePerLifetimeScope();

        // MVP: 视频下载服务注册
        builder.RegisterType<VideoDownloadService>().As<IVideoDownloadService>().InstancePerLifetimeScope();
    }
}