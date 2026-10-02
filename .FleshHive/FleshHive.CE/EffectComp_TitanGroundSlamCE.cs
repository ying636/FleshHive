using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive.CE;

public class EffectComp_TitanGroundSlamCE : EffectCompFlyPawn_Vertical
{
    public new EffectCompProperties_TitanGroundSlamCE Props => (EffectCompProperties_TitanGroundSlamCE)props;

    public override void DoEffect(Thing thing)
    {
        Pawn pawn = thing.Position.GetFirstPawn(thing.Map);
        if (pawn != null)
        {
            if (pawn.Flying || Pawn_PathFollower.GetPawnCellBaseCostOverride(pawn, pawn.Position) == 0)
            {
                return;
            }
            base.DoEffect(thing);
            return;
        }

        Thing target = thing.Position.GetThingList(thing.Map).Find(candidate => candidate != thing && candidate.def.useHitPoints);
        if (target == null || (!Props.attackFriendly && parent.Faction != null && target.Faction != null
            && parent.Faction.RelationKindWith(target.Faction) != FactionRelationKind.Hostile))
        {
            return;
        }

        target.TakeDamage(new DamageInfo(Props.damageDef, Props.amount, Props.armorPenetration));
    }
}
