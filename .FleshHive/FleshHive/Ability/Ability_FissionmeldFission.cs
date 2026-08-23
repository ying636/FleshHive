using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class CompProperties_AbilityFissionmeldFission : CompProperties_AbilityEffect
{
    public CompProperties_AbilityFissionmeldFission()
    {
        this.compClass = typeof(CompAbilityEffect_FissionmeldFission);
    }

    public int spawnRadius = 5;
}

public class CompAbilityEffect_FissionmeldFission : CompAbilityEffect
{
    public new CompProperties_AbilityFissionmeldFission Props => (CompProperties_AbilityFissionmeldFission)this.props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null || !TryRandomParasiteKind(out PawnKindDef kind))
        {
            return;
        }

        Pawn flesh = FleshHiveFleshbeastSpawnUtility.GeneratePawn(kind, caster.Faction);
        ParasitismSystem system = caster.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null && system.Parasite(flesh))
        {
            return;
        }

        FleshHiveFleshbeastSpawnUtility.SpawnPawnAsFlyer(flesh, caster.PositionHeld, map, Props.spawnRadius);
        TryAssignEnemyLord(flesh, map);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        Pawn caster = this.parent.pawn;
        return caster.MapHeld != null
               && TryRandomParasiteKind(out _);
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return this.parent.pawn.MapHeld != null
               && TryRandomParasiteKind(out _)
               && base.CanApplyOn(target, dest);
    }

    private static bool TryRandomParasiteKind(out PawnKindDef kind)
    {
        return FleshHiveFleshbeastSpawnUtility.TryRandomKind(ParasiteKinds, IsValidParasiteKind, out kind);
    }

    private static void TryAssignEnemyLord(Pawn pawn, Map map)
    {
        if (pawn.Faction == null || pawn.Faction.IsPlayer || !pawn.Faction.HostileTo(Faction.OfPlayer))
        {
            return;
        }
        Lord lord = map.lordManager.lords.FirstOrDefault(l => l.faction == pawn.Faction && l.CanAddPawn(pawn));
        lord?.AddPawn(pawn);
    }

    private static IEnumerable<PawnKindDef> ParasiteKinds
    {
        get
        {
            foreach (PawnKindDef kind in FleshBeastKindUtility.SmallKinds)
            {
                if (IsValidParasiteKind(kind))
                {
                    yield return kind;
                }
            }
            foreach (PawnKindDef kind in FleshBeastKindUtility.MediumKinds)
            {
                if (IsValidParasiteKind(kind))
                {
                    yield return kind;
                }
            }
            foreach (PawnKindDef kind in FleshBeastKindUtility.LargeKinds)
            {
                if (IsValidParasiteKind(kind))
                {
                    yield return kind;
                }
            }
        }
    }

    private static bool IsValidParasiteKind(PawnKindDef kind)
    {
        return kind?.race?.GetCompProperties<ParasitismCompProperties>() != null;
    }
}
