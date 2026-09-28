using HiveCreatureFramework;
using HiveCreatureFramework.Evolution;
using Verse;

namespace FleshHive;

public class CompProgressHolder_FleshHive : CompProgressHolder
{
    public override void CompTickInterval(int delta)
    {
        CancelStrandedEvolutionProgress();
        MapComponent_FleshHive? component = parent.Map?.GetComponent<MapComponent_FleshHive>();
        if (component?.IsHiveHungry != true)
        {
            base.CompTickInterval(delta);
            return;
        }

        Progress? progress = progresses.Find(current => current.CanBeProceed(this) && !ShouldPause(current));
        if (progress == null)
        {
            return;
        }

        progress.TickInterval(this, delta * ProgressSpeed);
        if (progress.ShouldBeRemoved)
        {
            progresses.Remove(progress);
        }
    }

    private void CancelStrandedEvolutionProgress()
    {
        if (parent.def != FleshHiveDefOf.FH_FleshPrimaryNest
            || parent.TryGetComp<CompHiveEvolution>() != null)
        {
            return;
        }

        for (int index = progresses.Count - 1; index >= 0; index--)
        {
            if (progresses[index] is not HiveEvolutionProgress progress)
            {
                continue;
            }

            progress.Cancel(this);
            progresses.RemoveAt(index);
            Log.Warning($"[FleshHive] Cancelled stranded evolution {progress.def?.defName ?? "null"} on {parent.def.defName}.");
        }
    }

    private static bool ShouldPause(Progress progress)
    {
        return progress is UnitSpawnData or ItemSpawnData or FormulaProgress;
    }
}
