using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using IGeekFan.FreeKit.Extras.FreeSql;
using LinCms.Common;
using LinCms.Data;
using LinCms.Entities;
using Microsoft.Extensions.Logging;

namespace LinCms.FreeSql;

public class DataSeedContributor : IDataSeedContributor
{
    private readonly IAuditBaseRepository<LinPermission> _permissionRepository;
    private readonly IAuditBaseRepository<LinGroupPermission> _groupPermissionRepository;
    private readonly IAuditBaseRepository<LinUser> _userRepository;
    private readonly IAuditBaseRepository<LinGroup> _groupRepository;
    private readonly ILogger<DataSeedContributor> _logger;

    public DataSeedContributor(
        IAuditBaseRepository<LinPermission> permissionRepository,
        IAuditBaseRepository<LinGroupPermission> groupPermissionRepository,
        IAuditBaseRepository<LinUser> userRepository,
        IAuditBaseRepository<LinGroup> groupRepository,
        ILogger<DataSeedContributor> logger)
    {
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task InitAdminPermission()
    {
        bool valid = await _groupPermissionRepository.Select.AnyAsync();
        if (valid) return;

        List<LinPermission> allPermissions = await _permissionRepository.Select.ToListAsync();

        List<LinGroupPermission> groupPermissions = allPermissions.Select(u => new LinGroupPermission(LinConsts.Group.Admin, u.Id)).ToList();

        await _groupPermissionRepository.InsertAsync(groupPermissions);

    }

    /// <summary>
    /// 权限标签上的Permission改变时，删除数据库中存在的无效权限，并生成新的权限。
    /// </summary>
    /// <returns></returns>
    public async Task SeedPermissionAsync(List<PermissionDefinition> linCmsAttributes, CancellationToken cancellationToken)
    {

        List<LinPermission> insertPermissions = new();
        List<LinPermission> updatePermissions = new();

        List<LinPermission> allPermissions = await _permissionRepository.Select.Where(r => r.PermissionType == PermissionType.Permission).ToListAsync(cancellationToken);

        Expression<Func<LinGroupPermission, bool>> expression = u => false;
        Expression<Func<LinPermission, bool>> permissionExpression = u => false;

        allPermissions.ForEach(permissioin =>
        {
            if (linCmsAttributes.All(r => r.Permission != permissioin.Name))
            {
                expression = expression.Or(r => r.PermissionId == permissioin.Id);
                permissionExpression = permissionExpression.Or(r => r.Id == permissioin.Id);
            }
        });
        int effectRows = await _permissionRepository.DeleteAsync(permissionExpression, cancellationToken);
        effectRows += await _groupPermissionRepository.DeleteAsync(expression, cancellationToken);
        _logger.LogInformation($"删除了{effectRows}条数据");


        #region Module 目录
        var allModules = await _permissionRepository.Select.Where(r => r.PermissionType == PermissionType.Folder).ToListAsync(cancellationToken);

        var permissionDefinitionsByModules = linCmsAttributes.GroupBy(r => r.Module).ToList();

        var insertMoudles = new List<LinPermission>();
        var sortCode = 10;
        foreach (var module in permissionDefinitionsByModules)
        {
            LinPermission permissionEntity = allModules.FirstOrDefault(u => u.Name == module.Key);
            if (permissionEntity == null)
            {
                insertMoudles.Add(new LinPermission()
                {
                    PermissionType = PermissionType.Folder,
                    Name = module.Key,
                    ParentId = 0,
                    SortCode = sortCode
                });
                sortCode += 10;
            }
        }
        await _permissionRepository.InsertAsync(insertMoudles, cancellationToken);
        #endregion

        allModules = await _permissionRepository.Select.Where(r => r.PermissionType == PermissionType.Folder).ToListAsync(cancellationToken);

        sortCode = 0;
        linCmsAttributes.ForEach(r =>
        {
            LinPermission permissionEntity = allPermissions.FirstOrDefault(u => u.Name == r.Permission);

            var parent = allModules.First(u => u.Name == r.Module);
            var parentId = parent.Id;
            sortCode = parent.SortCode;
            if (permissionEntity == null)
            {
                insertPermissions.Add(new LinPermission(r.Permission, PermissionType.Permission, r.Router)
                {
                    ParentId = parentId,
                    SortCode = sortCode
                });
            }
            else
            {
                bool routerExist = allPermissions.Any(u => u.Name == r.Permission);
                if (!routerExist)
                {
                    permissionEntity.Router = r.Router;
                    permissionEntity.ParentId = parentId;
                    permissionEntity.PermissionType = PermissionType.Permission;
                    permissionEntity.SortCode = sortCode;
                    updatePermissions.Add(permissionEntity);
                }
            }

            sortCode += 1;
        });

        await _permissionRepository.InsertAsync(insertPermissions, cancellationToken);
        _logger.LogInformation($"新增了{insertPermissions.Count}条数据");

        await _permissionRepository.UpdateAsync(updatePermissions, cancellationToken);
        _logger.LogInformation($"更新了{updatePermissions.Count}条数据");
    }

    public async Task InitAdminUser()
    {
        bool hasUser = await _userRepository.Select.AnyAsync();
        if (hasUser) return;

        _logger.LogInformation("开始初始化管理员账号...");

        // 1. 确保角色存在
        var adminGroup = await _groupRepository.Where(g => g.Name == "系统管理员").FirstAsync();
        if (adminGroup == null)
        {
            adminGroup = new LinGroup("系统管理员", "系统管理员", true);
            adminGroup = await _groupRepository.InsertAsync(adminGroup);
        }

        // 2. 创建管理员用户
        LinUser admin = new LinUser()
        {
            Nickname = "系统管理员",
            Username = "admin",
            Active = LinCms.Data.Enums.UserStatus.Active,
            CreateTime = DateTime.Now,
            IsDeleted = false,
            Salt = "9fd248c8-e9da-412f-bad9-aa5f7f1d7b80",
            LinUserIdentitys = new List<LinUserIdentity>()
            {
                new LinUserIdentity(LinUserIdentity.Password, "admin", "IWxIlqMAE3SU3JTogdDAJw==", DateTime.Now) // 密码: 123qwe
            },
            LinUserGroups = new List<LinUserGroup>()
            {
                new LinUserGroup(1, adminGroup.Id)
            }
        };

        await _userRepository.InsertAsync(admin);
        _logger.LogInformation("管理员账号 (admin/123qwe) 初始化成功！");
    }
}