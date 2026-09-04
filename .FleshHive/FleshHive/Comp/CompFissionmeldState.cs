using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_FissionmeldState : CompProperties
{
    public CompProperties_FissionmeldState()
    {
        compClass = typeof(CompFissionmeldState);
    }
}

public class CompFissionmeldState : ThingComp
{
    public int DormantHitPoints
    {
        get => dormantHitPoints;
        set => dormantHitPoints = value;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref dormantHitPoints, "dormantHitPoints", -1);
    }

    private int dormantHitPoints = -1;
}
