using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class StatWorker_ParasitismParryCE : StatWorker
{
    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        base.FinalizeValue(req, ref val, applyPostProcess);
        if (applyPostProcess && req.Thing is Pawn pawn)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                val += hediff.def.GetModExtension<ModExtension_ParasitismParryCE>()?.parryChanceBonus ?? 0f;
            }
            val = Mathf.Clamp(val, stat.minValue, stat.maxValue);
        }
    }
}
