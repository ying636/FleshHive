using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_FingerspikeAnimalFishing : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(job.GetTarget(SpotInd), job, 1, -1, null, errorOnFailed))
        {
            return pawn.Reserve(job.GetTarget(StandInd), job, 1, -1, null, errorOnFailed);
        }

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoCell(StandInd, PathEndMode.OnCell);
        int ticks = Mathf.RoundToInt(6000f / pawn.GetStatValue(StatDefOf.FishingSpeed));
        Toil toil = Toils_General.WaitWith(SpotInd, ticks, useProgressBar: false, maintainPosture: true,
            maintainSleep: false, SpotInd);
        toil.WithProgressBarToilDelay(StandInd);
        yield return toil;
        yield return CompleteFishingToil();
    }

    private Toil CompleteFishingToil()
    {
        Toil toil = ToilMaker.MakeToil("CompleteFishingToil");
        toil.initAction = delegate
        {
            IntVec3 cell = job.GetTarget(SpotInd).Cell;
            bool rare;
            List<Thing> catches = FishingUtility.GetCatchesFor(pawn, cell, animalFishing: true, out rare);
            if (!catches.Any())
            {
                return;
            }

            bool placed = false;
            int count = 0;
            foreach (Thing item in catches)
            {
                item.stackCount = Mathf.Max(1, Mathf.RoundToInt(item.stackCount * 0.25f));
                count += item.stackCount;
                placed |= GenPlace.TryPlaceThing(item, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }

            if (placed)
            {
                pawn.Map.waterBodyTracker.Notify_Fished(cell, count);
            }
        };
        return toil;
    }

    private const TargetIndex SpotInd = TargetIndex.A;
    private const TargetIndex StandInd = TargetIndex.B;
}
