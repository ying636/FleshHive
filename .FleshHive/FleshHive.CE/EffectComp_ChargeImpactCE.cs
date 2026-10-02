using HiveCreatureFramework;
using Verse;

namespace FleshHive.CE;

public class EffectComp_ChargeImpactCE : EffectCompDamage
{
    public new EffectCompProperties_ChargeImpactCE Props => (EffectCompProperties_ChargeImpactCE)props;

    public override void DoEffect(Thing thing)
    {
        thing.TakeDamage(new DamageInfo(Props.damageDef, Props.amount, Props.armorPenetration,
            instigator: parent, weapon: parent.def));
    }
}
