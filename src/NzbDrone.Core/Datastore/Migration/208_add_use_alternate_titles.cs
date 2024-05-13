using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration;

[Migration(208)]
public class add_use_alternate_titles : NzbDroneMigrationBase
{
    protected override void MainDbUpgrade()
    {
        Alter.Table("Series").AddColumn("UseAlternateTitlesForSearch").AsBoolean().WithDefaultValue(false);
    }
}
