using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_HiveGroupCapacityProvider : CompProperties
{
    public CompProperties_HiveGroupCapacityProvider()
    {
        compClass = typeof(CompHiveGroupCapacityProvider);
    }

    public int capacity = 10;
}

public class CompHiveGroupCapacityProvider : ThingComp
{
    public int Capacity => Props.capacity;

    private CompProperties_HiveGroupCapacityProvider Props => (CompProperties_HiveGroupCapacityProvider)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        parent.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterHiveCapacityProvider(this);
        registered = true;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (registered)
        {
            map?.GetComponent<MapComponent_FleshHive>()?.UnregisterHiveCapacityProvider(this);
            registered = false;
        }

        base.PostDeSpawn(map, mode);
    }

    private bool registered;
}

public class CompProperties_HiveScaleProvider : CompProperties
{
    public CompProperties_HiveScaleProvider()
    {
        compClass = typeof(CompHiveScaleProvider);
    }

    public int scale = 10;
}

public class CompHiveScaleProvider : ThingComp
{
    public int Scale => Props.scale;

    private CompProperties_HiveScaleProvider Props => (CompProperties_HiveScaleProvider)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        parent.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterHiveScaleProvider(this);
        registered = true;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (registered)
        {
            map?.GetComponent<MapComponent_FleshHive>()?.UnregisterHiveScaleProvider(this);
            registered = false;
        }

        base.PostDeSpawn(map, mode);
    }

    private bool registered;
}

public class CompProperties_HiveNutritionUpkeep : CompProperties
{
    public CompProperties_HiveNutritionUpkeep()
    {
        compClass = typeof(CompHiveNutritionUpkeep);
    }

    public float dailyNutritionCost = 1f;
}

public class CompHiveNutritionUpkeep : ThingComp
{
    public float DailyNutritionCost => Props.dailyNutritionCost;

    public bool Hungry => FleshHiveHungerUtility.IsHungry(parent);

    private CompProperties_HiveNutritionUpkeep Props => (CompProperties_HiveNutritionUpkeep)props;

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned)
        {
            return;
        }

        ticksUntilUpkeep--;
        if (ticksUntilUpkeep > 0)
        {
            return;
        }

        ticksUntilUpkeep = GenDate.TicksPerDay;
        TryConsumeUpkeep();
    }

    public override string CompInspectStringExtra()
    {
        string text = "FH_HiveNutritionUpkeep_Value".Translate(Props.dailyNutritionCost.ToString("0.##"));
        if (Hungry)
        {
            text += "\n" + "FH_HiveHungryInspect".Translate();
        }

        return text;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref ticksUntilUpkeep, "ticksUntilUpkeep", GenDate.TicksPerDay);
    }

    private void TryConsumeUpkeep()
    {
        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(parent.Map);
        if (fleshHive == null || Props.dailyNutritionCost <= 0f)
        {
            return;
        }

        fleshHive.nutrition = Mathf.Max(0f, fleshHive.nutrition - Props.dailyNutritionCost);
    }

    private int ticksUntilUpkeep = GenDate.TicksPerDay;
}
