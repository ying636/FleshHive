using HiveCreatureFramework;

namespace FleshHive.CE;

public class EffectCompProperties_TitanGroundSlamCE : EffectCompPropertiesFlyPawn_Vertical
{
    public EffectCompProperties_TitanGroundSlamCE()
    {
        compClass = typeof(EffectComp_TitanGroundSlamCE);
    }

    public float armorPenetration = 30f;
}
