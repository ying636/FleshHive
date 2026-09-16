using Verse;

namespace FleshHive;

public class Hediff_DeathOnDowned : Hediff
{
    public override bool CauseDeathNow()
    {
        return pawn.health.ShouldBeDowned() || base.CauseDeathNow();
    }
}
