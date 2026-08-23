using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace FleshHive;

public class CompProperties_AbilityBastionmeldSummonDeadguard : CompProperties_AbilityEffect
{
    public CompProperties_AbilityBastionmeldSummonDeadguard()
    {
        this.compClass = typeof(CompAbilityEffect_BastionmeldSummonDeadguard);
    }

    public PawnKindDef summonKind;

    public int summonCount = 4;

    public float aiSearchRadius = 40f;

    public List<ThingDef> weaponOptions = new();
}

public class CompAbilityEffect_BastionmeldSummonDeadguard : CompAbilityEffect
{
    public new CompProperties_AbilityBastionmeldSummonDeadguard Props => (CompProperties_AbilityBastionmeldSummonDeadguard)this.props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null || Props.summonKind == null)
        {
            return;
        }

        for (int i = 0; i < Props.summonCount; i++)
        {
            Pawn summoned = GeneratePawn(Props.summonKind, caster.Faction);
            EquipRandomWeapon(summoned);
            SpawnPawnAsFlyer(summoned, caster.PositionHeld, map);
            TryAssignEnemyLord(summoned, map);
        }
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        Pawn caster = this.parent.pawn;
        return caster.MapHeld != null
               && Props.summonKind != null
               && !Props.weaponOptions.NullOrEmpty()
               && AttackTargetFinder.BestAttackTarget(caster, TargetScanFlags.NeedThreat, t => t.HostileTo(caster), Props.aiSearchRadius) != null;
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return this.parent.pawn.MapHeld != null
               && Props.summonKind != null
               && !Props.weaponOptions.NullOrEmpty()
               && base.CanApplyOn(target, dest);
    }

    private void EquipRandomWeapon(Pawn pawn)
    {
        if (pawn.equipment == null || Props.weaponOptions.NullOrEmpty())
        {
            return;
        }

        ThingDef weaponDef = Props.weaponOptions.Where(def => def != null).RandomElementWithFallback();
        if (weaponDef == null)
        {
            return;
        }

        ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
        if (weapon != null)
        {
            pawn.equipment.AddEquipment(weapon);
        }
    }

    private static Pawn GeneratePawn(PawnKindDef kind, Faction faction)
    {
        return PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, null, false, false, false, true,
            false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, 0f,
            0f, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult,
            null, null, null, false, false, false, -1, 0, false));
    }

    private static void TryAssignEnemyLord(Pawn pawn, Map map)
    {
        if (pawn.GetLord() != null
            || pawn.Faction == null
            || pawn.Faction.IsPlayer
            || !pawn.Faction.HostileTo(Faction.OfPlayer))
        {
            return;
        }
        Lord lord = map.lordManager.lords.FirstOrDefault(l => l.faction == pawn.Faction && l.CanAddPawn(pawn));
        lord?.AddPawn(pawn);
    }

    private static void SpawnPawnAsFlyer(Pawn pawn, IntVec3 center, Map map)
    {
        GenSpawn.Spawn(pawn, center, map, WipeMode.VanishOrMoveAside);
        HCFGameUtility.AssignGroup(pawn, map, true);
        if (RCellFinder.TryFindRandomCellNearWith(center, c => c.Standable(map) && !c.Fogged(map) && c.GetFirstPawn(map) == null, map, out IntVec3 dest, 2, 5))
        {
            PawnFlyer flyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, pawn, dest, null, null);
            if (flyer != null)
            {
                GenSpawn.Spawn(flyer, dest, map, WipeMode.Vanish);
            }
        }
    }
}

public class CompProperties_AbilityBastionmeldFleshPulse : CompProperties_AbilityEffect
{
    public CompProperties_AbilityBastionmeldFleshPulse()
    {
        this.compClass = typeof(CompAbilityEffect_BastionmeldFleshPulse);
    }

    public float radius = 10.9f;

    public SoundDef? explosionSound;
}

public class CompAbilityEffect_BastionmeldFleshPulse : CompAbilityEffect
{
    public new CompProperties_AbilityBastionmeldFleshPulse Props => (CompProperties_AbilityBastionmeldFleshPulse)this.props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null)
        {
            return;
        }

        GenExplosion.DoExplosion(caster.PositionHeld, map, Props.radius, DamageDefOf.EMP, caster, -1, -1f, Props.explosionSound);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return HasEmpTargetNearby();
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return this.parent.pawn.MapHeld != null && base.CanApplyOn(target, dest);
    }

    public override void DrawEffectPreview(LocalTargetInfo target)
    {
        GenDraw.DrawRadiusRing(this.parent.pawn.PositionHeld, Props.radius, Color.cyan);
    }

    private bool HasEmpTargetNearby()
    {
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null)
        {
            return false;
        }

        foreach (Thing thing in GenRadial.RadialDistinctThingsAround(caster.PositionHeld, map, Props.radius, true))
        {
            if (IsEmpRelevantTarget(caster, thing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEmpRelevantTarget(Pawn caster, Thing thing)
    {
        if (thing == null || !thing.Spawned || !thing.HostileTo(caster))
        {
            return false;
        }

        if (thing is Pawn pawn)
        {
            return pawn.RaceProps.IsMechanoid;
        }

        if (thing is Building_Turret)
        {
            return true;
        }

        return thing.TryGetComp<CompProjectileInterceptor>() != null;
    }
}
