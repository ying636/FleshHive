using RimWorld;
using Verse;

namespace FleshHive;

public class CompUseEffect_FleshAdaptationSerum : CompUseEffect_AddHediff
{
    public override void DoEffect(Pawn user)
    {
        Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, user);
        HediffComp_FleshAdaptation adaptation = hediff.TryGetComp<HediffComp_FleshAdaptation>();
        if (adaptation == null)
        {
            Log.Error("[FleshHive] Flesh adaptation serum requires HediffComp_FleshAdaptation.");
            return;
        }

        adaptation.SetDuration(GenDate.TicksPerDay * 15);
        user.health.AddHediff(hediff);
    }
}
