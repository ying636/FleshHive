using System.Linq;
using Verse;

namespace FleshHive;

public class Verb_AbilityShootWithEffects : Verb_AbilityShoot
{
    public override void OrderForceTarget(LocalTargetInfo target)
    {
        Ability.QueueCastingJob(target, LocalTargetInfo.Invalid);
    }

    protected override bool TryCastShot()
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
        Scribe_Values.Look(ref effectsApplied, "effectsApplied");
    }

    private bool effectsApplied;
}
