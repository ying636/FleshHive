using CombatExtended;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Projectile_TitanChargeCE : Projectile_Charge
{
    protected override void DamageTarget(Thing thing, ModExtension_Charge extension, DamageInfo damage)
    {
        ProjectilePropertiesCE properties = (ProjectilePropertiesCE)def.projectile;
        float penetration = extension.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp
            ? properties.armorPenetrationSharp : properties.armorPenetrationBlunt;
        base.DamageTarget(thing, extension, new DamageInfo(extension.damageDef, extension.amount,
            penetration, -1f, Launcher, weapon: def));
    }
}
