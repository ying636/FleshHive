using HiveCreatureFramework;

namespace FleshHive.CE;

public class EffectCompProperties_TitanImpactCE : EffectCompPropertiesDamage
{
    public EffectCompProperties_TitanImpactCE()
    {
        compClass = typeof(EffectComp_TitanImpactCE);
    }

    public float armorPenetration = 30f;
}
