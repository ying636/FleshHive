using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FleshHive;

public enum FleshHiveUpgradeEffect
{
    FleshExpansion,
    NutritionAbsorption,
    SelfRepair,
    CellDivision,
    Reactivation,
    Agility,
    BoneSpikePenetration,
    ParasiticSpace,
    NestMasterCarapace,
    NestTaming,
    NestHealing,
    FleshbeastTaming,
    FleshShaping,
    Robust
}

public class FleshHiveUpgradeDef : Def
{
    public bool IsAvailable(MapComponent_FleshHive mapComp)
    {
        if (mapComp == null || mapComp.IsUpgradeCompleted(this) || mapComp.IsUpgradeProcessing(this))
        {
            return false;
        }

        if (requiresPrimaryNest && !mapComp.HasPrimaryNest)
        {
            return false;
        }

        return prerequisites.NullOrEmpty() || prerequisites.TrueForAll(mapComp.IsUpgradeCompleted);
    }

    public List<FleshHiveUpgradeDef> prerequisites = new();
    public FleshHiveUpgradeEffect effect;
    public float effectValue;
    public float nutritionCost = 300f;
    public int nerveFleshCost;
    public int processingTicks = GenDate.TicksPerDay * 3;
    public bool requiresPrimaryNest;
}
