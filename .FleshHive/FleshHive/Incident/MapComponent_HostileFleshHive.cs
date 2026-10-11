using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class MapComponent_HostileFleshHive : MapComponent
{
    public MapComponent_HostileFleshHive(Map map) : base(map)
    {
    }

    public HostileFleshHiveColony GetColony(HiveLord lord)
    {
        HostileFleshHiveColony? colony = colonies.FirstOrDefault(colony => colony.Lord == lord);
        if (colony == null)
        {
            colony = new HostileFleshHiveColony(lord);
            colonies.Add(colony);
        }
        return colony;
    }

    public HostileFleshHiveColony? GetColony(Thing building)
    {
        if (building == null)
            return null;
        return colonies.FirstOrDefault(colony => colony.Owns(building));
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % 250 != 0)
            return;
        foreach (HiveLord lord in map.GetComponent<MapComponent_HiveLord>().lords.ToArray())
            if (lord.def is HiveLordDef_HostileFleshHive && lord.Active)
                GetColony(lord).Tick();
        RemoveInactiveColonies();
    }

    public void RemoveInactiveColonies()
    {
        foreach (HostileFleshHiveColony colony in colonies.Where(colony => colony.Lord?.Active != true).ToArray())
        {
            colony.Disband();
            colonies.Remove(colony);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref colonies, "FH_hostileColonies", LookMode.Deep);
    }

    private List<HostileFleshHiveColony> colonies = new();
}
