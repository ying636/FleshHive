using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class HiveLordDef_HostileFleshHive : HiveLordDef
{
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
            yield return error;
        if (layout.Count(slot => slot.building == expansionBuildings.FirstOrDefault()) != maxHives - 1
            || layout.Any(slot => slot.building == null || slot.cost < 0f || slot.minHives < 1)
            || reserveNutrition < 0f || huntRadius <= 0f || minHunters < 1 || maxHunters < minHunters)
            yield return $"{defName}: invalid hostile hive layout or resource/hunting settings.";
    }

    public List<HostileHivePlanSlot> layout = new();
    public float initialNutrition = 100f;
    public float reserveNutrition = 40f;
    public float huntStopNutrition = 140f;
    public float huntRadius = 60f;
    public int minHunters = 3;
    public int maxHunters = 5;
    public int huntTimeoutTicks = 15000;
    public int huntCooldownTicks = 5000;
    public int constructionTicks = 2500;
}
