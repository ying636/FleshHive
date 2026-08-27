using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_FleshBeastCache : CompProperties
{
    public CompProperties_FleshBeastCache()
    {
        this.compClass = typeof(CompFleshBeastCache);
    }

    public FleshBeastSize? size;
}

public class CompFleshBeastCache : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (this.parent is Pawn pawn)
        {
            MapComponent_FleshHive? comp = pawn.Map?.GetComponent<MapComponent_FleshHive>();
            comp?.RegisterFleshBeast(pawn);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        if (this.parent is Pawn pawn)
        {
            map?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshBeast(pawn);
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (this.parent is Pawn pawn)
        {
            MapComponent_FleshHive? comp = previousMap?.GetComponent<MapComponent_FleshHive>();
            comp?.UnregisterFleshBeast(pawn);
        }
    }
}
