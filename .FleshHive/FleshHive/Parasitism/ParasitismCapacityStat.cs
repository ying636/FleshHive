using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ParasitismCapacityStat : StatWorker
{
    public override float GetBaseValueFor(StatRequest request)
    {
        Pawn pawn = request.Thing as Pawn ?? request.Pawn;
        if (pawn != null)
        {
            if (Hediff_Hela.GetCached(pawn) is Hediff_Hela hela)
            {
                return hela.ParasiteCapacity;
            }

            bool isFleshbeast = pawn.RaceProps.FleshType == FleshTypeDefOf.Fleshbeast;
            return pawn.BodySize + (isFleshbeast ? 0f : 1f);
        }

        if (request.Def is ThingDef thingDef && thingDef.race != null)
        {
            bool isFleshbeast = thingDef.race.FleshType == FleshTypeDefOf.Fleshbeast;
            return thingDef.race.baseBodySize + (isFleshbeast ? 0f : 1f);
        }

        return 1;
    }

    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        base.FinalizeValue(req, ref val, applyPostProcess);
        val = Mathf.Min(val, 14);
        val = Mathf.FloorToInt(val);
    }
}
