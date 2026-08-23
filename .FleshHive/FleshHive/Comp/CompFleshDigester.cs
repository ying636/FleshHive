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

    public float consumeRadius = 8.9f;

    public int attackIntervalTicks = 180;

    public int attackDurationTicks = 90;

    public int attackMouthOpenTicks = 60;

    public int maxAttackTargets = 15;

    public DamageDef attackDamageDef = DamageDefOf.Bite;

    public float attackDamage = 8f;

    public float attackArmorPenetration = 0f;

    public float attackAngle = -1f;

    public int consumeIntervalTicks = 5000;

    public int upkeepIntervalTicks = 60000;

    public float upkeepNutritionCost = 2f;
}

public class CompFleshDigester : ThingComp
{
    private CompProperties_FleshDigester Props => (CompProperties_FleshDigester)props;

    private IEnumerable<IntVec3> ConsumeCells => GenRadial.RadialCellsAround(parent.Position, Props.consumeRadius, true);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            upkeepTicks = 0;
            consumeTicks = 0;
            attackTicks = 0;
            activeThisCycle = true;
        }

        UpdateMouthGraphic();
        LongEventHandler.ExecuteWhenFinished(UpdateConsumeRoots);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        EndAttack();
        if (roots != null)
        {
            foreach (Mote mote in roots.Values.Where(mote => mote != null))
            {
                mote.Destroy(DestroyMode.Vanish);
            }
            roots.Clear();
        }

        rootTargets?.Clear();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned || parent.Map == null)
        {
            return;
        }

        UpdateMouthGraphic();
        upkeepTicks++;
        attackTicks++;
        consumeTicks++;

        if (upkeepTicks >= Props.upkeepIntervalTicks)
        {
            upkeepTicks = 0;
            activeThisCycle = TryConsumeHiveNutrition(Props.upkeepNutritionCost);
        }

        if (!activeThisCycle)
        {
            EndAttack();
            return;
        }

        TickAttackEffect();

        if (parent.IsHashIntervalTick(250))
        {
            UpdateConsumeRoots();
        }

        if (consumeTicks >= Props.consumeIntervalTicks)
        {
            consumeTicks = 0;
            TryConsumeNearbyNutrition();
        }

        if (attackTargets.NullOrEmpty() && attackTicks >= Props.attackIntervalTicks)
        {
            attackTicks = 0;
            TryStartAttack();
        }
    }

    public override string CompInspectStringExtra()
    {
        string statusKey = activeThisCycle ? "FH_FleshDigester_Status_Active" : "FH_FleshDigester_Status_Inactive";
        return string.Join("\n",
            "FH_FleshDigester_NutritionUpkeep".Translate(Props.upkeepNutritionCost.ToString("0.##")),
            "FH_FleshDigester_AttackRadius".Translate(Props.attackRadius.ToString("0.#")),
            "FH_FleshDigester_ConsumeRadius".Translate(Props.consumeRadius.ToString("0.#")),
            statusKey.Translate());
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
        Scribe_Values.Look(ref upkeepTicks, "upkeepTicks");
        Scribe_Values.Look(ref consumeTicks, "consumeTicks");
        Scribe_Values.Look(ref attackTicks, "attackTicks");
        Scribe_Values.Look(ref activeThisCycle, "activeThisCycle", true);
        Scribe_Collections.Look(ref rootTargets, "rootTargets", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            attackTargets ??= new List<Pawn>();
            attackTargets.RemoveAll(pawn => pawn == null || pawn.Destroyed);
            rootTargets?.RemoveAll(thing => thing == null || thing.Destroyed);
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

    private void TryConsumeNearbyNutrition()
    {
        if (parent.Map == null)
        {
            return;
        }

        UpdateConsumeRoots();
        List<ThingWithComps> candidates = rootTargets.Where(CanConsume).ToList();
        if (!candidates.TryRandomElement(out ThingWithComps target))
        {
            return;
        }

        if (!TryConsumeTarget(target, out float nutrition))
        {
            return;
        }

        MapComponent_FleshHive.AddNutrition(parent.Map, nutrition);
        EffecterDefOf.HarbingerTreeConsume.Spawn(target.Position, parent.Map, 1f);
    }

    private void UpdateConsumeRoots()
    {
        if (parent.Map == null)
        {
            return;
        }

        EnsureRootCollections();
        tmpRadialCells.Clear();
        tmpRadialCells.AddRange(ConsumeCells);

        foreach (ThingWithComps rootTarget in rootTargets)
        {
            TryMakeRoot(rootTarget);
        }

        foreach (IntVec3 cell in tmpRadialCells)
        {
            if (!cell.InBounds(parent.Map))
            {
                continue;
            }

            tmpThings.Clear();
            tmpThings.AddRange(cell.GetThingList(parent.Map));
            foreach (Thing thing in tmpThings)
            {
                if (thing is ThingWithComps thingWithComps && CanConsume(thingWithComps))
                {
                    TryMakeRoot(thingWithComps);
                }
            }
        }

        foreach (ThingWithComps rootTarget in rootTargets)
        {
            if (rootTarget.Destroyed || !tmpRadialCells.Contains(rootTarget.PositionHeld) || !CanConsume(rootTarget))
            {
                deferredDestroy.Enqueue(rootTarget);
            }
        }

        while (deferredDestroy.Count > 0)
        {
            DestroyRoot(deferredDestroy.Dequeue());
        }
    }

    private void EnsureRootCollections()
    {
        roots ??= new Dictionary<ThingWithComps, Mote>();
        rootTargets ??= new List<ThingWithComps>();
    }

    private bool TryConsumeHiveNutrition(float amount)
    {
        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(parent.Map);
        if (fleshHive == null || fleshHive.nutrition < amount)
        {
            return false;
        }

        fleshHive.nutrition -= amount;
        return true;
    }

    private bool CanConsume(ThingWithComps thing)
    {
        if (thing == null || thing.Destroyed || thing == parent || !CanBeConsumed(thing))
        {
            return false;
        }

        CompHarbingerTreeConsumable comp = thing.TryGetComp<CompHarbingerTreeConsumable>();
        if (comp != null && comp.CanBeConsumed && comp.AvailableNutrition(false) > 0f)
        {
            return true;
        }

        if (thing is Corpse corpse)
        {
            return GetNutritionFromCorpse(corpse, false) > 0f;
        }

        return CanConsumeItem(thing) && GetNutritionFromItemStack(thing, false) > 0f;
    }

    private bool TryConsumeTarget(ThingWithComps thing, out float nutrition)
    {
        nutrition = 0f;
        if (thing == null || thing.Destroyed)
        {
            return false;
        }

        CompHarbingerTreeConsumable comp = thing.TryGetComp<CompHarbingerTreeConsumable>();
        if (comp != null && comp.CanBeConsumed && comp.AvailableNutrition(true) > 0f)
        {
            nutrition = 55f;
            return true;
        }

        if (thing is Corpse corpse)
        {
            nutrition = GetNutritionFromCorpse(corpse, true);
            return nutrition > 0f;
        }

        if (CanConsumeItem(thing))
        {
            nutrition = GetNutritionFromItemStack(thing, true);
            return nutrition > 0f;
        }

        return false;
    }

    private bool CanBeConsumed(ThingWithComps thing)
    {
        if (!thing.Spawned)
        {
            return false;
        }

        SlotGroup slotGroup = thing.GetSlotGroup();
        return slotGroup == null || slotGroup.parent.Isnt<Building>();
    }

    private bool CanConsumeItem(ThingWithComps thing)
    {
        return thing.def.category == ThingCategory.Item &&
               thing.GetStatValue(StatDefOf.Nutrition) > 0f &&
               (thing.def.IsMeat || thing.def.ingestible?.foodType.HasFlag(FoodTypeFlags.Meat) == true);
    }

    private float GetNutritionFromCorpse(Corpse corpse, bool applyDigestion)
    {
        if (corpse.InnerPawn == null || !corpse.InnerPawn.RaceProps.IsFlesh)
        {
            return 0f;
        }

        corpse.InnerPawn.health.hediffSet.GetNotMissingParts()
            .Where(part => part.depth == BodyPartDepth.Outside && !part.def.conceptual && part != corpse.InnerPawn.RaceProps.body.corePart)
            .TryRandomElement(out BodyPartRecord bodyPart);
        if (bodyPart == null)
        {
            float nutrition = FoodUtility.GetBodyPartNutrition(corpse, corpse.InnerPawn.RaceProps.body.corePart);
            if (applyDigestion)
            {
                corpse.Destroy(DestroyMode.Vanish);
            }
            // return nutrition * FleshHiveResearchUtility.NutritionAbsorptionFactor;
            return nutrition;
        }

        float bodyPartNutrition = FoodUtility.GetBodyPartNutrition(corpse, bodyPart);
        if (applyDigestion)
        {
            Hediff_MissingPart missingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, corpse.InnerPawn, bodyPart);
            missingPart.IsFresh = true;
            missingPart.lastInjury = HediffDefOf.Digested;
            corpse.InnerPawn.health.AddHediff(missingPart);
        }
            // return bodyPartNutrition * FleshHiveResearchUtility.NutritionAbsorptionFactor;
            return bodyPartNutrition;
    }

    private float GetNutritionFromItemStack(ThingWithComps item, bool applyDigestion)
    {
        int count = Mathf.Min(item.stackCount, ItemConsumeCountRange.RandomInRange);
        if (!applyDigestion)
        {
            // return item.GetStatValue(StatDefOf.Nutrition) * ((float)count / item.stackCount) * FleshHiveResearchUtility.NutritionAbsorptionFactor;
            return item.GetStatValue(StatDefOf.Nutrition) * ((float)count / item.stackCount);
        }

        Thing splitItem = item.SplitOff(count);
        // float nutrition = splitItem.GetStatValue(StatDefOf.Nutrition) * count * FleshHiveResearchUtility.NutritionAbsorptionFactor;
        float nutrition = splitItem.GetStatValue(StatDefOf.Nutrition) * count;
        splitItem.Destroy(DestroyMode.Vanish);
        return nutrition;
    }

    private void TryMakeRoot(ThingWithComps thing)
    {
        if (thing == null || thing.Destroyed)
        {
            return;
        }

        if (!roots.ContainsKey(thing))
        {
            float exactRot = 0f;
            if (thing is Corpse corpse)
            {
                exactRot = corpse.InnerPawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
            }
            roots[thing] = MoteMaker.MakeStaticMote(thing.Position.ToVector3Shifted(), parent.Map, ThingDefOf.Mote_HarbingerTreeRoots, 1f, false, exactRot);
        }

        if (!rootTargets.Contains(thing))
        {
            rootTargets.Add(thing);
        }
    }

    private void DestroyRoot(ThingWithComps thing)
    {
        if (thing == null)
        {
            return;
        }

        if (roots.TryGetValue(thing, out Mote mote) && mote != null)
        {
            mote.Destroy(DestroyMode.Vanish);
        }
        roots.Remove(thing);
        rootTargets.Remove(thing);
    }

    private List<Pawn> attackTargets = new();

    private readonly Dictionary<Pawn, Mote> attackRootMotes = new();

    private readonly Dictionary<Pawn, IntVec3> attackRootCells = new();

    private int attackEndTick;

    private int mouthOpenUntilTick;

    private int upkeepTicks;

    private int consumeTicks;

    private int attackTicks;

    private bool activeThisCycle = true;

    private Dictionary<ThingWithComps, Mote> roots;

    private List<ThingWithComps> rootTargets;

    private readonly Queue<ThingWithComps> deferredDestroy = new();

    private readonly List<Thing> tmpThings = new();

    private readonly List<IntVec3> tmpRadialCells = new();

    private readonly List<Pawn> tmpAttackTargets = new();

    private readonly List<IntVec3> tmpAttackCells = new();

    private static readonly IntRange ItemConsumeCountRange = new(4, 12);
    private const int OpenMouthGraphicIndex = 0;
    private const int ClosedMouthGraphicIndex = 1;
}
