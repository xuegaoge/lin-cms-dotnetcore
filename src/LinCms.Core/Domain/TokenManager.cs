using DotNetCore.Security;
using IGeekFan.FreeKit.Extras.Dependency;
using IGeekFan.FreeKit.Extras.Security;
using LinCms.Entities;
using LinCms.IRepositories;
using LinCms.Security;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinCms.Domain;

public class TokenManager(IJwtService jsonWebTokenService, ILogger<TokenManager> logger, IUserRepository userRepository)
    : ITokenManager, ITransientDependency
{
    public async Task<UserAccessToken> CreateTokenAsync(LinUser user)
    {
        List<Claim> claims = new()
        {
            new Claim(FreeKitClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(FreeKitClaimTypes.Email, user.Email ?? ""),
            new Claim(FreeKitClaimTypes.Name, user.Nickname ?? ""),
            new Claim(FreeKitClaimTypes.UserName, user.Username ?? ""),
        };
        user.LinGroups?.ForEach(r =>
        {
            claims.Add(new Claim(FreeKitClaimTypes.Role, r.Name));
            claims.Add(new Claim(LinCmsClaimTypes.GroupIds, r.Id.ToString()));
        });

        string token = jsonWebTokenService.Encode(claims);

        // 只更新需要修改的字段，避免 FreeSql 内部错误
        user.AddRefreshToken();
        await userRepository.UpdateDiy.Set(u => new LinUser
        {
            RefreshToken = user.RefreshToken,
            LastLoginTime = user.LastLoginTime
        }).Where(u => u.Id == user.Id).ExecuteAffrowsAsync();

        return new UserAccessToken(token, user.RefreshToken, 24 * 60 * 60, "Bearer", 24 * 60 * 60 * 30);
    }
}