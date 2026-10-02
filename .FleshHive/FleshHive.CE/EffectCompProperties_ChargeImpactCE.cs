using HiveCreatureFramework;

namespace FleshHive.CE;

public class EffectCompProperties_ChargeImpactCE : EffectCompPropertiesDamage
{
    public EffectCompProperties_ChargeImpactCE()
    {
        compClass = typeof(EffectComp_ChargeImpactCE);
    }

    public float armorPenetration;
}
