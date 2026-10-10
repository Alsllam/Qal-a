namespace Qala.Framework.Domain.Security;

/// <summary>Implemented by each module to list its permission names, so the DbMigrator can seed them.</summary>
public interface IPermissionDefinitionProvider
{
    IEnumerable<string> GetPermissions();
}
