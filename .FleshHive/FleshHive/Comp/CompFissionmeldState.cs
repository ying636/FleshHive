using System.Collections.Generic;
using RimWorld;
using Verse;
using HiveCreatureFramework;

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
    public List<UnitGroup> PreservedGroups => preservedGroups;

    public int DormantHitPoints
    {
        get => dormantHitPoints;
        set => dormantHitPoints = value;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref dormantHitPoints, "dormantHitPoints", -1);
        Scribe_Collections.Look(ref preservedGroups, "preservedGroups", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            preservedGroups ??= new List<UnitGroup>();
        }
    }

    private int dormantHitPoints = -1;
    private List<UnitGroup> preservedGroups = new List<UnitGroup>();
}
