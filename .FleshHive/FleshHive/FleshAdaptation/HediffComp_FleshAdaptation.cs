using RimWorld;
using Verse;

namespace FleshHive;

public class HediffComp_FleshAdaptation : HediffComp
{
    public override bool CompShouldRemove => remainingTicks == 0;

    public override string CompLabelInBracketsExtra => remainingTicks > 0
        ? remainingTicks.ToStringTicksToPeriod()
        : null!;

    public void SetDuration(int ticks)
    {
        remainingTicks = ticks;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (remainingTicks > 0)
        {
            remainingTicks--;
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref remainingTicks, "remainingTicks", -1);
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        Pawn.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        Pawn.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
    }

    private int remainingTicks = -1;
}
