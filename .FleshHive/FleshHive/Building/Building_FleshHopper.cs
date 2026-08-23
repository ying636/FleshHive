using RimWorld;
using Verse;

namespace FleshHive;

public class Building_FleshHopper : Building
{
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_FleshHive>()?.RegisterFleshHopper(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map currentMap = Map;
        MapComponent_FleshHive mapComponent = currentMap?.GetComponent<MapComponent_FleshHive>();
        mapComponent?.UnregisterFleshHopper(this);
        mapComponent?.CancelFleshHopperItemProgressesIfNeeded();
        base.DeSpawn(mode);
    }
}
