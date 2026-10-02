using CombatExtended;
using Verse;

namespace FleshHive.CE;

public class Verb_AbilityShootWithEffectsCE : Verb_AbilityShootCE
{
    public override bool TryCastShot()
    {
        if (!effectsApplied && Ability.EffectComps.Any(effect => !effect.CanCast))
        {
            return false;
        }
        bool fired = base.TryCastShot();
        if (fired && !effectsApplied)
        {
            effectsApplied = true;
            Ability.Activate(currentTarget, currentDestination);
        }
        return fired;
    }

    public override void WarmupComplete()
    {
        effectsApplied = false;
        base.WarmupComplete();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref effectsApplied, "ceEffectsApplied");
    }

    private bool effectsApplied;
}
