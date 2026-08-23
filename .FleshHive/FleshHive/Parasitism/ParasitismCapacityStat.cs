using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ParasitismCapacityStat : StatWorker
{
    public override float GetBaseValueFor(StatRequest request)
    {
        if (request.Thing is Pawn pawn)
        {
            if (Hediff_Hela.GetCached(pawn) is Hediff_Hela hela)
            {
                return hela.ParasiteCapacity;
            }
            return pawn.BodySize + 1;
        }
        if (request.Pawn != null)
        {
            if (Hediff_Hela.GetCached(request.Pawn) is Hediff_Hela hela)
            {
                return hela.ParasiteCapacity;
            }
            return request.Pawn.BodySize + 1;
        }

        return 1;
    }

    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        base.FinalizeValue(req, ref val, applyPostProcess);
        Pawn pawn = req.Pawn ?? req.Thing as Pawn;
        if (Hediff_Hela.GetCached(pawn) is Hediff_Hela hela)
        {
            val = Mathf.Min(val, hela.MaximumParasiteCapacity);
            return;
        }
        val = Mathf.Min(val, 14);
    }
}
