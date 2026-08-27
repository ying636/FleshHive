using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_DreadmeldCollapse : CompProperties
{
    public CompProperties_DreadmeldCollapse()
    {
        compClass = typeof(CompDreadmeldCollapse);
    }
}

public class CompDreadmeldCollapse : ThingComp
{
    public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
    {
        if (!ModLister.CheckAnomaly("Dreadmeld"))
        {
            return;
        }

        UndercaveMapComponent? undercave = prevMap?.GetComponent<UndercaveMapComponent>();
        if (undercave == null)
        {
            return;
        }

        if (undercave.pitGate == null)
        {
            Log.Error("[FleshHive] 地下母兽死亡时找不到对应的 PitGate，无法开始坍塌。");
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
