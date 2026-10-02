using CombatExtended;
using RimWorld;
using Verse;
using Verse.Sound;

namespace FleshHive.CE;

public class Tentacle_Attackable_CE : Tentacle_Attackable
{
    public Tentacle_Attackable_CE()
    {
    }

    public Tentacle_Attackable_CE(TentacleProperties prop) : base(prop)
    {
    }

    public new TentacleProperties_CE Prop => (TentacleProperties_CE)base.Prop;

    public override void Attack(Thing target)
    {
        Pawn pawn = Comp.Pawn;
        ThingDef source = Prop.meleeSource;
        List<ToolCE>? tools = source?.tools?.OfType<ToolCE>()
            .Where(tool => tool.linkedBodyPartsGroup?.defName != "HeadAttackTool").ToList();
        if (source == null || tools == null || Prop.sourceToolIndex < 0 || Prop.sourceToolIndex >= tools.Count)
        {
            Log.Error($"[FleshHive] CE tentacle {Comp.parent.def.defName} has no matching tool on {source?.defName}.");
            return;
        }

        ToolCE tool = tools[Prop.sourceToolIndex];
        ManeuverDef? maneuver = tool.Maneuvers.FirstOrDefault();
        if (maneuver?.verb?.meleeDamageDef == null)
        {
            Log.Error($"[FleshHive] CE tentacle source tool {source.defName}/{tool.id} has no melee maneuver.");
            return;
        }

        Verb_MeleeAttack_FleshTentacleCE verb = new Verb_MeleeAttack_FleshTentacleCE
        {
            caster = pawn,
            verbTracker = pawn.VerbTracker,
            tool = tool,
            maneuver = maneuver,
            verbProps = maneuver.verb,
            criticalChance = source.GetStatValueAbstract(CE_StatDefOf.MeleeCritChance),
            weaponHediff = Comp.parent.def
        };
        rotateTime = Prop.rotatingTime;
        verb.Attack(target);
        targetAngle = 90 - (target.Position - pawn.Position).AngleFlat;
        cooldown = tool.cooldownTime * 60f;
    }
}
