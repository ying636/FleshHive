using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ItemSpawnData_FleshShaping : ItemSpawnData
{
    public ItemSpawnData_FleshShaping()
    {
    }

    public ItemSpawnData_FleshShaping(ItemDef def, float productionSpeedFactor) : base(def)
    {
        if (productionSpeedFactor > 0f && productionSpeedFactor != 1f)
        {
            time = Mathf.Max(1f, time / productionSpeedFactor);
            totalTime = Mathf.Max(1f, totalTime / productionSpeedFactor);
        }
    }
}
