using Verse;

namespace FleshHive;

public class HediffComp_FleshAdaptation : HediffComp
{
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
}
