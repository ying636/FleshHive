using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class Verb_AbilityShootMoveAfterAttack : Verb_AbilityShoot
{
    protected override bool TryCastShot()
    {
        if (Ability == null)
        {
            Ability = verbTracker?.directOwner as Ability;
            if (Ability == null)
            {
                Log.ErrorOnce($"{nameof(Verb_AbilityShootMoveAfterAttack)} could not resolve its owning ability for {caster}.",
                    Gen.HashCombineInt(caster?.thingIDNumber ?? 0, 183419672));
                return false;
            }
        }

        bool fired = base.TryCastShot();
        if (fired && CasterPawn?.TryGetComp<UnitComp>() is { } comp)
        {
            comp.lastMoveTick = -1250;
        }

        return fired;
    }
}
