using HiveCreatureFramework;

namespace FleshHive;

public class FleshHiveBuildingDef : HiveBuildingDef
{
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        FleshBlueprintUtility.ConfigureBlueprint(this);
    }
}
