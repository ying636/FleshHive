using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompProgressHolder_FleshHive : CompProgressHolder
{
    public override void CompTickInterval(int delta)
    {
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

    private static bool ShouldPause(Progress progress)
    {
        return progress is UnitSpawnData or ItemSpawnData or FormulaProgress;
    }
}
