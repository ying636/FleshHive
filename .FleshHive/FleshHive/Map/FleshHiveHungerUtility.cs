using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public static class FleshHiveHungerUtility
{
    public static bool IsHungry(Thing hive)
    {
        if (hive?.Map == null)
        {
            return false;
        }

        MapFleshHive mapFleshHive = MapComponent_FleshHive.GetMapFleshHive(hive.Map);
        if (mapFleshHive == null)
        {
            return false;
        }

        CompHiveNutritionUpkeep upkeep = hive.TryGetComp<CompHiveNutritionUpkeep>();
        if (upkeep != null && upkeep.DailyNutritionCost > 0f)
        {
            return mapFleshHive.nutrition < upkeep.DailyNutritionCost;
        }

        if (hive.TryGetComp<CompHiveGroup>() != null || hive.TryGetComp<CompHiveContainer>() != null)
        {
            return mapFleshHive.nutrition <= 0f;
        }

        return false;
    }
}
