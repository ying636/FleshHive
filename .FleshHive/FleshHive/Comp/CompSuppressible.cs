using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class CompProperties_Suppressible : CompProperties
{
    public CompProperties_Suppressible()
    {
        compClass = typeof(CompSuppressible);
    }

    public float suppressIfAbove = 0.05f;
    public float suppressionFactor = 1f;
}

public class CompSuppressible : ThingComp
{
    public float SuppressionFactor => Props.suppressionFactor;

    private CompProperties_Suppressible Props => (CompProperties_Suppressible)props;

    public bool CanSuppress(Pawn pawn, bool forced = false)
    {
        if (parent?.Spawned != true || parent.Faction != Faction.OfPlayer)
        {
            return false;
        }

        MapComponent_FleshHive mapComp = parent.Map?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return false;
        }

        return mapComp.Activity > 0f && pawn.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced);
    }

    public override string CompInspectStringExtra()
    {
        MapComponent_FleshHive mapComp = parent.Map?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null || mapComp.Activity <= 0f)
        {
            return null;
        }

        return "FH_Suppressible_Inspect".Translate(mapComp.ActivityPercent.ToStringPercent("0"));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        if (parent?.Faction != Faction.OfPlayer)
        {
            yield break;
        }

        MapComponent_FleshHive mapComp = parent.Map?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            yield break;
        }

        yield return new Gizmo_FleshHiveCapacity(mapComp);
        yield return new Gizmo_FleshHiveActivity(mapComp);
    }
}

