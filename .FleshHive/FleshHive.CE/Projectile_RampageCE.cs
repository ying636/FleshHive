using CombatExtended;
using HiveCreatureFramework;
using Verse;

namespace FleshHive.CE;

public class Projectile_RampageCE : Projectile_Charge
{
    protected override void DamageTarget(Thing thing, ModExtension_Charge extension, DamageInfo damage)
    {
        ProjectilePropertiesCE properties = (ProjectilePropertiesCE)def.projectile;
        DamageInfo hit = new DamageInfo(extension.damageDef, extension.amount,
            properties.armorPenetrationSharp, -1f, Launcher, weapon: def);
        base.DamageTarget(thing, extension, hit);
    }
}
