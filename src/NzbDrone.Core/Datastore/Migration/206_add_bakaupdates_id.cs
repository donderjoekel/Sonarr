using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration;

[Migration(206)]
public class add_bakaupdates_info : NzbDroneMigrationBase
{
    protected override void MainDbUpgrade()
    {
        Alter.Table("Series").AlterColumn("TvdbId").AsInt64().NotNullable();
        Alter.Table("Episodes").AlterColumn("TvdbId").AsInt64().NotNullable();
    }
}
