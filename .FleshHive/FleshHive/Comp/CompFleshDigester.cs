using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_FleshDigester : CompProperties
{
    public CompProperties_FleshDigester()
    {
        compClass = typeof(CompFleshDigester);
    }

    public float attackRadius = 8.9f;

    public int attackIntervalTicks = 180;

    public int attackDurationTicks = 90;

    public int attackMouthOpenTicks = 60;

    public int maxAttackTargets = 15;

    public DamageDef attackDamageDef = DamageDefOf.Bite;

    public float attackDamage = 8f;

    public float attackArmorPenetration = 0f;

    public float attackAngle = -1f;

}

public class CompFleshDigester : ThingComp
{
    private CompProperties_FleshDigester Props => (CompProperties_FleshDigester)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            attackTicks = 0;
        }

        UpdateMouthGraphic();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        EndAttack();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned || parent.Map == null)
        {
            return;
        }

        UpdateMouthGraphic();
        attackTicks++;

        TickAttackEffect();

        if (attackTargets.NullOrEmpty() && attackTicks >= Props.attackIntervalTicks)
        {
            attackTicks = 0;
            TryStartAttack();
        }
    }

    public override string CompInspectStringExtra()
    {
        return string.Join("\n",
            "FH_FleshDigester_AttackRadius".Translate(Props.attackRadius.ToString("0.#")));
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (parent.Map == null)
        {
            return;
        }

        GenDraw.DrawRadiusRing(parent.Position, Props.attackRadius, Color.white);
        DrawFleshAttackCells();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref attackTargets, "attackTargets", LookMode.Reference);
        Scribe_Values.Look(ref attackEndTick, "attackEndTick");
        Scribe_Values.Look(ref mouthOpenUntilTick, "mouthOpenUntilTick");
        Scribe_Values.Look(ref attackTicks, "attackTicks");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            attackTargets ??= new List<Pawn>();
            attackTargets.RemoveAll(pawn => pawn == null || pawn.Destroyed);
        }
    }

    private void TryStartAttack()
    {
        List<Pawn> targets = FindAttackTargets();
        if (targets.Count == 0)
        {
            return;
        }

        EndAttack();
        attackTargets.AddRange(targets);
        attackEndTick = Find.TickManager.TicksGame + Props.attackDurationTicks;
        mouthOpenUntilTick = Find.TickManager.TicksGame + Props.attackMouthOpenTicks;
        UpdateMouthGraphic();
        foreach (Pawn target in attackTargets)
        {
            UpdateAttackRoot(target, forceRecreate: true);
            target.stances?.stunner?.StunFor(Props.attackDurationTicks, parent, false, false, false);
            target.TakeDamage(new DamageInfo(Props.attackDamageDef, Props.attackDamage, Props.attackArmorPenetration, Props.attackAngle, parent));
            EffecterDefOf.HarbingerTreeConsume.Spawn(target.Position, target.Map, 1f);
        }
    }

    private void TickAttackEffect()
    {
        if (attackTargets.NullOrEmpty())
        {
            return;
        }

        if (Find.TickManager.TicksGame >= attackEndTick)
        {
            EndAttack();
            return;
        }

        tmpAttackTargets.Clear();
        tmpAttackTargets.AddRange(attackTargets);
        foreach (Pawn target in tmpAttackTargets)
        {
            if (!CanContinueAttackEffect(target))
            {
                DestroyAttackRoot(target);
                attackTargets.Remove(target);
                continue;
            }

            UpdateAttackRoot(target);
        }

        if (attackTargets.Count == 0)
        {
            EndAttack();
        }
    }

    private void UpdateAttackRoot(Pawn target, bool forceRecreate = false)
    {
        if (target == null || target.Map != parent.Map)
        {
            return;
        }

        IntVec3 currentCell = target.PositionHeld;
        if (!forceRecreate &&
            attackRootMotes.TryGetValue(target, out Mote currentMote) &&
            currentMote != null &&
            attackRootCells.TryGetValue(target, out IntVec3 rootCell) &&
            currentCell == rootCell)
        {
            return;
        }

        DestroyAttackRoot(target);
        attackRootCells[target] = currentCell;
        float exactRot = target.Drawer?.renderer?.BodyAngle(PawnRenderFlags.None) ?? 0f;
        attackRootMotes[target] = MoteMaker.MakeStaticMote(currentCell.ToVector3Shifted(), parent.Map, ThingDefOf.Mote_HarbingerTreeRoots, 1f, false, exactRot);
    }

    private void EndAttack()
    {
        foreach (Pawn target in attackRootMotes.Keys.ToList())
        {
            DestroyAttackRoot(target);
        }

        attackTargets.Clear();
        attackEndTick = 0;
    }

    private void DestroyAttackRoot(Pawn target)
    {
        if (target == null)
        {
            return;
        }

        if (attackRootMotes.TryGetValue(target, out Mote mote) && mote != null)
        {
            mote.Destroy(DestroyMode.Vanish);
        }

        attackRootMotes.Remove(target);
        attackRootCells.Remove(target);
    }

    private void UpdateMouthGraphic()
    {
        parent.overrideGraphicIndex = Find.TickManager.TicksGame < mouthOpenUntilTick ? OpenMouthGraphicIndex : ClosedMouthGraphicIndex;
    }

    private List<Pawn> FindAttackTargets()
    {
        return parent.Map.mapPawns.AllPawnsSpawned
            .Where(CanAttackTarget)
            .OrderBy(pawn => pawn.Position.DistanceToSquared(parent.Position))
            .Take(Props.maxAttackTargets)
            .ToList();
    }

    private bool CanAttackTarget(Pawn pawn)
    {
        return pawn != null &&
               pawn.Spawned &&
               !pawn.Dead &&
               pawn.Map == parent.Map &&
               pawn.HostileTo(Faction.OfPlayer) &&
               pawn.Position.InHorDistOf(parent.Position, Props.attackRadius) &&
               FleshTerrainUtility.IsFleshTerrain(parent.Map, pawn.Position);
    }

    private bool CanShowAttackCell(IntVec3 cell)
    {
        return FleshTerrainUtility.IsFleshTerrain(parent.Map, cell);
    }

    private void DrawFleshAttackCells()
    {
        tmpAttackCells.Clear();
        int cellCount = GenRadial.NumCellsInRadius(Props.attackRadius);
        for (int i = 0; i < cellCount; i++)
        {
            IntVec3 cell = parent.Position + GenRadial.RadialPattern[i];
            if (CanShowAttackCell(cell))
            {
                tmpAttackCells.Add(cell);
            }
        }

        GenDraw.DrawFieldEdges(tmpAttackCells, Color.red);
    }

    private bool CanContinueAttackEffect(Pawn pawn)
    {
        return pawn != null &&
               pawn.Spawned &&
               !pawn.Dead &&
               pawn.Map == parent.Map &&
               !pawn.Destroyed;
    }

    private List<Pawn> attackTargets = new();

    private readonly Dictionary<Pawn, Mote> attackRootMotes = new();

    private readonly Dictionary<Pawn, IntVec3> attackRootCells = new();

    private int attackEndTick;

    private int mouthOpenUntilTick;

    private int attackTicks;

    private readonly List<Pawn> tmpAttackTargets = new();

    private readonly List<IntVec3> tmpAttackCells = new();

    private const int OpenMouthGraphicIndex = 0;
    private const int ClosedMouthGraphicIndex = 1;
}
