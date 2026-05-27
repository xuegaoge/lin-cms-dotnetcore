using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading.Tasks;
using FreeRedis;
using LinCms.Aop.Attributes;
using LinCms.Common;
using Newtonsoft.Json.Serialization;

namespace LinCms.Middleware;

public class AopCacheableActionFilter : IAsyncActionFilter
{
    private readonly IConfiguration _configuration;
    private readonly IRedisClient? _redisClient;
    private readonly int _expireSeconds;

    public AopCacheableActionFilter(IConfiguration configuration, IRedisClient? redisClient = null)
    {
        _configuration = configuration;
        _redisClient = redisClient;
        _expireSeconds = int.Parse((string)configuration["Cache:ExpireSeconds"].ToString());
    }

    /// <summary>
    /// 尝试从方法中获取当前的工作单元配置
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private CacheableAttribute? TryGetCacheable(ActionExecutingContext context)
    {
        ControllerActionDescriptor? descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        MethodInfo method = descriptor?.MethodInfo ?? throw new ArgumentNullException("context");
        var cacheableAttribute = method.GetCacheableAttributeOrNull();

        return cacheableAttribute;
    }

    /// <summary>
    /// 方法执行前执行UnitOfWorkManager工作单元
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!(context.ActionDescriptor is ControllerActionDescriptor))
        {
            await next();
            return;
        }

        var cacheAttr = TryGetCacheable(context);
        if (cacheAttr == null)
        {
            await next();
            return;
        }

        if (_redisClient == null)
        {
            // 如果没有Redis，直接执行方法
            await next();
            return;
        }

        string cacheKey = GenerateCacheKey(cacheAttr.CacheKey, context);
        string cacheValue = await _redisClient.GetAsync(cacheKey);
        if (cacheValue != null)
        {
            var result = JsonConvert.DeserializeObject(cacheValue, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            context.Result = new JsonResult(result);
            return;
        }

        ActionExecutedContext resultContext = await next();
        if (resultContext.Result != null && resultContext.Exception == null && _redisClient != null)
        {
            var json = JsonConvert.SerializeObject((resultContext.Result as ObjectResult)?.Value, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            await _redisClient.SetAsync(cacheKey, json, _expireSeconds);
        }
    }

    private static string GenerateCacheKey(string key, ActionExecutingContext context)
    {
        ControllerActionDescriptor? descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        MethodInfo method = descriptor?.MethodInfo ?? throw new ArgumentNullException("context");
        string controllerName = descriptor.ControllerName;
        string actionName = method.Name;

        string cacheKey = $"{controllerName}:{actionName}:{key}";
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value == null) continue;

            string value = argument.Value.ToString() ?? string.Empty;
            cacheKey = $"{cacheKey}:{argument.Key}={value}";
        }

        return cacheKey;
    }
}
