using HiveCreatureFramework;
using System.Reflection;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompPropertiesHivePlan_FleshHive : CompPropertiesHivePlan
{
    public CompPropertiesHivePlan_FleshHive()
    {
        compClass = typeof(CompHivePlan_FleshHive);
    }
}

public class CompHivePlan_FleshHive : CompHivePlan
{
    public int CurrentCheckIntervalTicks
    {
        get
        {
            if (CheckIntervalTicksField == null)
            {
                Log.Error("[FleshHive] HCF CompHivePlan.checkIntervalTicks field was not found; shared plan timing is unavailable.");
                return 0;
            }

            int interval = (int)CheckIntervalTicksField.GetValue(this);
            return interval > 0 ? interval : Props.checkIntervalTicks;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        parent.Map?.GetComponent<MapComponent_FleshHive>()?.PreparePlanCheckInterval(this);
    }

    public override void CompTickInterval(int delta)
    {
        parent.Map?.GetComponent<MapComponent_FleshHive>()?.PreparePlanCheckInterval(this);
        base.CompTickInterval(delta);
    }

    public void ApplySharedCheckInterval(int intervalTicks)
    {
        if (intervalTicks <= 0)
        {
            return;
        }

        if (CheckIntervalTicksField == null || NextCheckTickField == null)
        {
            Log.Error("[FleshHive] HCF CompHivePlan timing fields were not found; shared plan timing cannot be applied.");
            return;
        }

        CheckIntervalTicksField.SetValue(this, intervalTicks);
        ResetSharedCheckSchedule();
    }

    public void ResetSharedCheckSchedule()
    {
        if (NextCheckTickField == null)
        {
            Log.Error("[FleshHive] HCF CompHivePlan.nextCheckTick field was not found; shared plan timing cannot be scheduled.");
            return;
        }

        NextCheckTickField.SetValue(this, Find.TickManager.TicksGame + CurrentCheckIntervalTicks);
    }

    protected override bool CanSchedule(HivePlanEntry entry)
    {
        if (!base.CanSchedule(entry))
        {
            return false;
        }

        if (entry.targetType != HivePlanTargetType.Item)
        {
            return true;
        }

        if (HasPendingItem(ProgressHolder, entry.itemDef.thing))
        {
            return false;
        }

        if (entry.itemDef.worker is ItemSpawnWorker_FleshHopper)
        {
            foreach (Building_FleshHopper hopper in FleshHopperUtility.GetCachedHoppers(parent.Map))
            {
                if (hopper.Faction == parent.Faction
                    && HasPendingItem(hopper.TryGetComp<CompProgressHolder>(), entry.itemDef.thing))
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected override void CheckEntries()
    {
        MapComponent_FleshHive? component = parent.Map?.GetComponent<MapComponent_FleshHive>();
        if (component?.IsHiveHungry == true)
        {
            return;
        }

        base.CheckEntries();
    }

    private static bool HasPendingItem(CompProgressHolder? holder, ThingDef thingDef)
    {
        return holder?.progresses.Any(progress => progress is ItemSpawnData itemProgress
            && itemProgress.item?.thing == thingDef
            && itemProgress.time >= itemProgress.totalTime) == true;
    }

    private static readonly FieldInfo CheckIntervalTicksField = typeof(CompHivePlan)
        .GetField("checkIntervalTicks", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo NextCheckTickField = typeof(CompHivePlan)
        .GetField("nextCheckTick", BindingFlags.Instance | BindingFlags.NonPublic);
}
