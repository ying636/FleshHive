using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_FleshHiveContainer : CompPropertiesHiveContainer
{
    public CompProperties_FleshHiveContainer()
    {
        compClass = typeof(CompFleshHiveContainer);
    }
}

public class CompFleshHiveContainer : CompHiveContainer
{
    public override void AddUnit(Pawn unit)
    {
        base.AddUnit(unit);
    }

    public override void Heal(Pawn unit, float point)
    {
        if (unit?.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hediff_Upgrade_FastHealing) == true)
        {
            point *= 1.5f;
        }

        base.Heal(unit, point);
    }

    protected override void FillPawnResources(Pawn pawn)
    {
        base.FillPawnResources(pawn);
        CompTwistedFlesh storage = pawn.TryGetComp<CompTwistedFlesh>();
        if (storage == null || !storage.AllowAutoRefillTwistedFlesh || storage.MaxTwistedFlesh <= 0)
        {
            return;
        }

        HiveResource? nutrition = Resource.resources.Find(resource => resource.def == FleshHiveDefOf.FH_Resource_Nutrition);
        if (nutrition == null || nutrition.Amount <= 0f)
        {
            return;
        }

        float target = Mathf.RoundToInt(storage.MaxTwistedFlesh * storage.TwistedFleshTargetValue);
        float amount = Mathf.Min(target - storage.CurrentTwistedFlesh, nutrition.Amount * 20f);
        if (amount <= 0f)
        {
            return;
        }

        float added = storage.AddResource(FleshHiveDefOf.FH_Resource_TwistedFlesh, amount);
        Resource.ConsumeResource(nutrition, added / 20f);
    }
}
