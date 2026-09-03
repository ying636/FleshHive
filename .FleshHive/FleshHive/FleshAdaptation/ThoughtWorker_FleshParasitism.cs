using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ThoughtWorker_FleshParasitism : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (FleshAdaptationUtility.HasAdaptation(p) || p?.health?.hediffSet?.hediffs == null)
        {
            return ThoughtState.Inactive;
        }

        int parasitismCount = 0;
        for (int i = 0; i < p.health.hediffSet.hediffs.Count; i++)
        {
            if (p.health.hediffSet.hediffs[i] is ParasitismHediff)
            {
                parasitismCount++;
            }
        }

        return parasitismCount > 0
            ? ThoughtState.ActiveAtStage(Mathf.Min(parasitismCount - 1, 13))
            : ThoughtState.Inactive;
    }
}
