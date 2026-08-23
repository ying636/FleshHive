using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_FleshPowerPlant : CompProperties_Power
{
    public CompProperties_FleshPowerPlant()
    {
        compClass = typeof(CompFleshPowerPlant);
    }

    public float powerOutput = 1000f;
    public float dailyNutritionCost = 1.2f;
}

public class CompFleshPowerPlant : CompPowerPlant
{
    public float DailyNutritionCost => Props.dailyNutritionCost;

    private new CompProperties_FleshPowerPlant Props => (CompProperties_FleshPowerPlant)props;

    protected override float DesiredPowerOutput => HasNutritionForNextTick() ? Props.powerOutput : 0f;

    public override void CompTick()
    {
        base.CompTick();
        if (PowerOutput > 0f && !TryConsumeNutrition())
        {
            PowerOutput = 0f;
        }
    }

    public override string CompInspectStringExtra()
    {
        string text = base.CompInspectStringExtra();
        string upkeepText = "FH_FleshPowerPlant_NutritionUpkeep".Translate(Props.dailyNutritionCost.ToString("0.##"));
        if (!HasNutritionForNextTick())
        {
            upkeepText += "\n" + "FH_FleshPowerPlant_Inactive".Translate();
        }

        return text.NullOrEmpty() ? upkeepText : text + "\n" + upkeepText;
    }

    private bool TryConsumeNutrition()
    {
        if (!parent.Spawned || Props.dailyNutritionCost <= 0f)
        {
            return false;
        }

        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(parent.Map);
        if (fleshHive == null)
        {
            return false;
        }

        float nutritionCost = Props.dailyNutritionCost / GenDate.TicksPerDay;
        if (fleshHive.nutrition < nutritionCost)
        {
            fleshHive.nutrition = 0f;
            return false;
        }

        fleshHive.nutrition = Mathf.Max(0f, fleshHive.nutrition - nutritionCost);
        return true;
    }

    private bool HasNutritionForNextTick()
    {
        if (!parent.Spawned || Props.dailyNutritionCost <= 0f)
        {
            return false;
        }

        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(parent.Map);
        return fleshHive != null && fleshHive.nutrition >= Props.dailyNutritionCost / GenDate.TicksPerDay;
    }
}
