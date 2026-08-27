using Verse;

namespace FleshHive;

public class CompProperties_FleshBuildingCache : CompProperties
{
    public CompProperties_FleshBuildingCache()
    {
        compClass = typeof(CompFleshBuildingCache);
    }
}

public class CompFleshBuildingCache : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (parent is Building building)
        {
            building.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterFleshBuilding(building);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        if (parent is Building building)
        {
            map?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshBuilding(building);
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (parent is Building building)
        {
            previousMap?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshBuilding(building);
        }
    }
}
