using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Verb_ShootFleshTurretCE : Verb_ShootCE
{
    public override bool TryCastShot()
    {
        CompRefuelable? fuel = Caster.TryGetComp<CompRefuelable>();
        if (fuel != null && fuel.Fuel < verbProps.consumeFuelPerShot)
        {
            return false;
        }
        bool fired = base.TryCastShot();
        if (fired)
        {
            fuel?.ConsumeFuel(verbProps.consumeFuelPerShot);
        }
        return fired;
    }
}
