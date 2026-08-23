using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class Building_RenameableFleshHive : Building_Hive, IRenameable
{
    public string RenamableLabel
    {
        get => customName ?? BaseLabel;
        set => customName = value;
    }

    public string BaseLabel => def.LabelCap;

    public string InspectLabel => RenamableLabel;

    public override string Label => RenamableLabel;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (this.TryGetComp<CompFleshHiveEvolution>() is { CanShowEvolutionButton: true } evolution)
        {
            yield return evolution.CreateEvolutionCommand();
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_FleshHive>()?.RegisterFleshHive(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map currentMap = Map;
        currentMap?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshHive(this);
        base.DeSpawn(mode);
    }

    public override string GetInspectString()
    {
        string inspectString = base.GetInspectString();
        int fleshbeastCount = this.TryGetComp<CompHiveGroup>()?.groups
            .SelectMany(group => group.units)
            .Distinct()
            .Count() ?? 0;
        string fleshbeastText = "FH_HiveOwnedFleshbeasts".Translate(fleshbeastCount);
        return inspectString.NullOrEmpty() ? fleshbeastText : inspectString + "\n" + fleshbeastText;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref customName, "customName");
    }

    private string? customName;
}
