using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_HiveInitialNutrition : CompProperties
{
    public CompProperties_HiveInitialNutrition()
    {
        compClass = typeof(CompHiveInitialNutrition);
    }

    public float initialNutrition = 100f;
}

public class CompHiveInitialNutrition : ThingComp
{
    private CompProperties_HiveInitialNutrition Props => (CompProperties_HiveInitialNutrition)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (respawningAfterLoad || addedNutrition)
        {
            return;
        }

        AddInitialNutrition();
        addedNutrition = true;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref addedNutrition, "addedNutrition", false);
    }

    private void AddInitialNutrition()
    {
        MapComponent_FleshHive.AddNutrition(parent.Map, Props.initialNutrition);
    }

    private bool addedNutrition;
}

public class CompProperties_HiveNutritionProducer : CompProperties
{
    public CompProperties_HiveNutritionProducer()
    {
        compClass = typeof(CompHiveNutritionProducer);
    }

    public float nutritionPerInterval = 0.1f;

    public int intervalTicks = 2500;

    public float activityPerHour;
}

public class CompHiveNutritionProducer : ThingComp
{
    private CompProperties_HiveNutritionProducer Props => (CompProperties_HiveNutritionProducer)props;

    public override void CompTick()
    {
        base.CompTick();
        TickInterval(1);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        TickInterval(GenTicks.TickRareInterval);
    }

    public override string CompInspectStringExtra()
    {
        return "FH_HiveNutritionProducer_Value".Translate(GetIntervalLabel(), Props.nutritionPerInterval.ToString("0.##"));
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref ticksSinceProduce, "ticksSinceProduce", 0);
    }

    private string GetIntervalLabel()
    {
        return Props.intervalTicks.ToStringTicksToPeriod(true, false, true, true, false);
    }

    private void TickInterval(int ticks)
    {
        if (!parent.Spawned)
        {
            return;
        }

        ticksSinceProduce += ticks;
        if (ticksSinceProduce >= Props.intervalTicks)
        {
            ticksSinceProduce = 0;
            ProduceNutrition();
        }
    }

    private void ProduceNutrition()
    {
        float producedNutrition = MapComponent_FleshHive.AddNutrition(parent.Map, Props.nutritionPerInterval);
        if (producedNutrition <= 0f || Props.activityPerHour <= 0f || parent.Map == null)
        {
            return;
        }

        MapComponent_FleshHive mapComp = parent.Map.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return;
        }

        mapComp.Activity += Props.activityPerHour * Props.intervalTicks / GenDate.TicksPerHour;
    }

    private int ticksSinceProduce;
}
