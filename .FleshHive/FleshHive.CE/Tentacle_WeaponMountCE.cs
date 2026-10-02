using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Tentacle_WeaponMountCE : Tentacle_WeaponMount
{
    public Tentacle_WeaponMountCE()
    {
    }

    public Tentacle_WeaponMountCE(TentacleProperties props) : base(props)
    {
    }

    protected override bool PerformMeleeAttack(Verb_MeleeAttack verb, LocalTargetInfo target)
    {
        if (verb is Verb_MeleeAttackCE ceVerb)
        {
            ceVerb.TryCastShot();
            return true;
        }
        return base.PerformMeleeAttack(verb, target);
    }
}
