using CombatExtended;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.Sound;

namespace FleshHive.CE;

public class Verb_MeleeAttack_FleshTentacleCE : Verb_MeleeAttackCE
{
    protected override float PenetrationSkillMultiplier => 1f;

    protected override float PenetrationOtherMultipliers => 1f;

    public bool Attack(LocalTargetInfo target)
    {
        currentTarget = target;
        return TryCastShot();
    }

    public override bool TryCastShot()
    {
        Thing target = currentTarget.Thing;
        if (target == null || target.Destroyed || !target.Spawned || CasterPawn?.Spawned != true)
        {
            return false;
        }
        Pawn? defender = target as Pawn;
        bool mobile = defender != null && !defender.Downed && defender.GetPosture() == PawnPosture.Standing;
        CasterPawn.rotationTracker.Face(target.DrawPos);
        if (mobile && CasterPawn.skills != null && defender?.IsColonyMech != true)
        {
            CasterPawn.skills.Learn(SkillDefOf.Melee, 200f * verbProps.AdjustedFullCycleTime(this, CasterPawn));
        }
        bool hit = Rand.Chance(hitChance(this, currentTarget));
        SoundDef sound = tool.soundMeleeMiss ?? CasterPawn.def.race.soundMeleeMiss ?? SoundDefOf.Pawn_Melee_Punch_Miss;
        if (hit && mobile && !surpriseAttack && defender!.stances.stunner.Stunned != true
            && Rand.Chance(dodgeChance(this, defender)))
        {
            hit = false;
            sound = defender.def.race.soundMeleeDodge ?? sound;
            CreateCombatLog(m => m.combatLogRulesDodge, false);
        }
        else if (hit && !surpriseAttack && defender != null && CanDoParry(defender)
            && Rand.Chance(GetComparativeChanceAgainst(defender, CasterPawn, CE_StatDefOf.MeleeParryChance, 0.2f)))
        {
            Apparel_Shield? shield = defender.apparel?.WornApparel.OfType<Apparel_Shield>().FirstOrDefault();
            Thing parryThing = shield != null && Rand.Chance(0.75f) ? shield : (Thing?)defender.equipment?.Primary ?? defender;
            float counterChance = GetComparativeChanceAgainst(defender, CasterPawn, CE_StatDefOf.MeleeCritChance, 0.1f);
            DoParry(defender, parryThing, Rand.Chance(counterChance), Rand.Chance(counterChance));
            hit = false;
            CreateCombatLog(m => m.combatLogRulesDeflect, false);
        }
        else if (hit)
        {
            BattleLogEntry_MeleeCombat log = CreateCombatLog(m => m.combatLogRulesHit, false);
            ApplyMeleeDamageToTarget(currentTarget).AssociateWithLog(log);
            sound = tool.soundMeleeHit ?? (target.def.category == ThingCategory.Building
                ? CasterPawn.def.race.soundMeleeHitBuilding ?? SoundDefOf.MeleeHit_Unarmed
                : CasterPawn.def.race.soundMeleeHitPawn ?? SoundDefOf.Pawn_Melee_Punch_HitPawn);
        }
        else
        {
            CreateCombatLog(m => m.combatLogRulesMiss, false);
        }
        sound?.PlayOneShot(new TargetInfo(target.PositionHeld, target.MapHeld));
        CasterPawn.Drawer.Notify_MeleeAttackOn(target);
        CasterPawn.caller?.Notify_DidMeleeAttack();
        if (defender?.Spawned == true && !defender.Dead)
        {
            defender.stances.stagger.StaggerFor(95);
            defender.mindState.meleeThreat = CasterPawn;
            defender.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
        }
        return hit;
    }

    public override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
    {
        currentTarget = target;
        isCrit = target.Thing is Pawn && Rand.Chance(criticalChance);
        return base.ApplyMeleeDamageToTarget(target);
    }

    protected override IEnumerable<DamageInfo> DamageInfosToApply(LocalTargetInfo target, bool isCrit = false)
    {
        foreach (DamageInfo original in base.DamageInfosToApply(target, isCrit))
        {
            DamageInfo damage = original;
            if (damage.Def == verbProps.meleeDamageDef)
            {
                damage.SetAmount(tool.power);
            }
            else if (damage.Def == DamageDefOf.Stun && isCrit)
            {
                damage.SetAmount(tool.power * 0.25f);
            }
            damage.SetWeaponHediff(weaponHediff);
            yield return damage;
        }
    }

    public float criticalChance;
    public HediffDef weaponHediff = null!;

    private static readonly Func<Verb_MeleeAttackCE, LocalTargetInfo, float> hitChance =
        (Func<Verb_MeleeAttackCE, LocalTargetInfo, float>)typeof(Verb_MeleeAttackCE)
            .GetMethod("GetHitChance", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Func<Verb_MeleeAttackCE, LocalTargetInfo, float>));
    private static readonly Func<Verb_MeleeAttackCE, Pawn, float> dodgeChance =
        (Func<Verb_MeleeAttackCE, Pawn, float>)typeof(Verb_MeleeAttackCE)
            .GetMethod("GetDodgeChance", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Func<Verb_MeleeAttackCE, Pawn, float>));
}
