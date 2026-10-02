using HiveCreatureFramework;
using Verse;
using Verse.AI;

namespace FleshHive.CE;

public class EffectComp_TitanImpactCE : EffectCompDamage
{
    public new EffectCompProperties_TitanImpactCE Props => (EffectCompProperties_TitanImpactCE)props;

    public override void DoEffect(Thing thing)
    {
        if (thing is Pawn pawn && (pawn.Flying || Pawn_PathFollower.GetPawnCellBaseCostOverride(pawn, pawn.Position) == 0))
        {
            return;
        }

        thing.TakeDamage(new DamageInfo(Props.damageDef, Props.amount, Props.armorPenetration, weapon: parent.def));
    }
}
