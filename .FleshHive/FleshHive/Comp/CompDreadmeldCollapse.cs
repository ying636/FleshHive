using RimWorld;
using Verse;

namespace FleshHive;

public class CompDreadmeldCollapse : ThingComp
{
    public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
    {
        BeginCollapse(prevMap);
    }

    private void BeginCollapse(Map map)
    {
        if (!ModsConfig.AnomalyActive)
        {
            return;
        }

        UndercaveMapComponent? undercave = map?.GetComponent<UndercaveMapComponent>();
        if (undercave == null)
        {
            return;
        }

        if (undercave.pitGate == null)
        {
            Log.Error("[FleshHive] Cannot begin undercave collapse: the mother's map has no PitGate.");
            return;
        }
        if (undercave.pitGate.IsCollapsing)
        {
            return;
        }

        Find.LetterStack.ReceiveLetter(
            LetterMaker.MakeLetter(
                "LetterLabelUndercaveCollapsing".Translate(),
                "LetterUndercaveCollapsing".Translate(),
                LetterDefOf.NeutralEvent));
        undercave.pitGate.BeginCollapsing();
    }
}
