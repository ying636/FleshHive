using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public static class ParasitismSystemUtility
{
    public static HediffStage Get(float rate)
    {
        if (!stageDictionary.ContainsKey(rate))
        {
            stageDictionary[rate] = new HediffStage(){hungerRateFactorOffset = rate};
        }
        return stageDictionary[rate];
    }

    static Dictionary<float, HediffStage> stageDictionary = new Dictionary<float, HediffStage>();
}

public class ParasitismSystem : HediffWithComps
{
    public int Limit
    {
        get
        {
            if (this.cacheLimit == -1)
            {
                if (this.pawn != null)
                {
                    this.cacheLimit = Mathf.FloorToInt(this.pawn.GetStatValue(FleshHiveDefOf.FH_Stat_ParasitismCapacity));
                }
                else
                {
                    return 1;
                }
            }
            return this.cacheLimit;
        }
    }

    public int CurrentTwistedFlesh
    {
        get => currentTwistedFlesh;
        private set => currentTwistedFlesh = value;
    }

    public float TwistedFleshTargetValue
    {
        get => twistedFleshTargetValue;
        set => twistedFleshTargetValue = Mathf.Clamp01(value);
    }

    public bool AllowAutoRefillTwistedFlesh
    {
        get => allowAutoRefillTwistedFlesh;
        set => allowAutoRefillTwistedFlesh = value;
    }

    public int MaxTwistedFlesh
    {
        get
        {
            if (cacheMaxTwistedFlesh == -1)
            {
                cacheMaxTwistedFlesh = 0;
                if (pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela) is Hediff_Hela hela)
                {
                    cacheMaxTwistedFlesh += hela.TwistedFleshCapacity;
                }
                foreach (var hd in ParasitismHediffs)
                {
                    if (hd.flesh?.TryGetComp<ParasitismComp>() is ParasitismComp comp)
                    {
                        cacheMaxTwistedFlesh += comp.Props.twistedFleshCapacity;
                    }
                }
            }
            return cacheMaxTwistedFlesh;
        }
    }

    public bool HasFleshwind
    {
        get
        {
            if (!cacheHasFleshwind.HasValue)
            {
                cacheHasFleshwind = ParasitismHediffs.Any(hediff => hediff.def == FleshHiveDefOf.FH_Parasitism_Fleshwind);
            }
            return cacheHasFleshwind.Value;
        }
    }

    public bool CanConsumeTwistedFlesh(int amount)
    {
        return CurrentTwistedFlesh >= amount;
    }

    public bool ConsumeTwistedFlesh(int amount)
    {
        if (!CanConsumeTwistedFlesh(amount))
        {
            return false;
        }
        CurrentTwistedFlesh -= amount;
        return true;
    }

    public void FillTwistedFlesh(int amount)
    {
        CurrentTwistedFlesh += amount;
        if (CurrentTwistedFlesh > MaxTwistedFlesh)
        {
            CurrentTwistedFlesh = MaxTwistedFlesh;
        }
    }

    public float HungerRate => this.ParasitismHediffs.Count * 0.2f;

    public int Count
    {
        get
        {
            if (this.cacheCount == -1)
            {
                this.cacheCount = ParasitismHediffs.Sum(h =>
                    ((ParasitismHediff)h).Count);
            }
            return this.cacheCount;
        }
    }

    public HashSet<ParasitismHediff> ParasitismHediffs
    {
        get
        {
            if (this.hds == null)
            {
                this.hds = new HashSet<ParasitismHediff>();
                if (pawn?.health?.hediffSet?.hediffs == null)
                {
                    return this.hds;
                }
                foreach (var parasitismHediff in pawn.health.hediffSet.hediffs.FindAll(h => h is ParasitismHediff).ConvertAll(h => (ParasitismHediff)h))
                {
                    hds.Add(parasitismHediff);
                }
            }
            return this.hds;
        }
    }

    public override HediffStage CurStage => ParasitismSystemUtility.Get(this.HungerRate);

    public override IEnumerable<Gizmo> GetGizmos()
    {
        yield return new ParasitismCapacityGizmo(this);
        if (MaxTwistedFlesh > 0)
        {
            yield return new Gizmo_TwistedFleshForParasitism(this);
        }
        if (ParasitismHediffs.Any(hediff =>
            hediff.TryGetComp<HediffComp_Parasitism>() is { ShowAttackGizmo: true } comp &&
            comp.AttackTentacles.Any()))
        {
            yield return new ParasitismAbilityGizmo(this);
        }
        if (DebugSettings.ShowDevGizmos)
        {
            if (MaxTwistedFlesh > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "FH_DevAddTwistedFlesh".Translate(),
                    action = delegate
                    {
                        FillTwistedFlesh(Math.Max(1, MaxTwistedFlesh / 10));
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "FH_DevFillTwistedFlesh".Translate(),
                    action = delegate
                    {
                        FillTwistedFlesh(MaxTwistedFlesh);
                    }
                };
            }
            yield return new Command_Action
            {
                defaultLabel = "FH_DevGiveParasite".Translate(),
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(
                        DefDatabase<PawnKindDef>.AllDefsListForReading
                            .Where(kind => kind.race?.comps?.Any(properties => properties is ParasitismCompProperties) == true)
                            .OrderBy(kind => kind.label)
                            .Select(kind => new FloatMenuOption(kind.LabelCap, () => DebugParasite(kind)))
                            .ToList()
                    ));
                }
            };
        }
    }

    public bool Parasite(Pawn flesh, bool parentChildParasite = false)
    {
        if (flesh == null)
        {
            return false;
        }
        ParasitismComp comp = flesh.TryGetComp<ParasitismComp>();
        if (comp == null || comp.Props.hediff == null)
        {
            return false;
        }
        if (this.ParasitismHediffs.Count >= 14)
        {
            return false;
        }
        int spaceCost = comp.Props.cost;
        if (this.Limit - this.Count >= spaceCost)
        {
            bool synchronizeHost = comp.Props.synchronizeHost;
            if (!synchronizeHost && flesh.Spawned)
            {
                flesh.DeSpawn();
            }
            EnsureAbilityTracker(this.pawn);
            ParasitismHediff hd = (ParasitismHediff)this.pawn.health.AddHediff(comp.Props.hediff);
            if (hd == null)
            {
                return false;
            }
            hd.spaceCost = spaceCost;
            hd.flesh = flesh;
            hd.lord = this.pawn.GetLord();
            hd.parentChildParasite = parentChildParasite;
            if (parentChildParasite)
            {
                flesh.TryGetComp<UnitComp>()?.group?.RemoveUnit(flesh);
                if (synchronizeHost)
                {
                    flesh.Map?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshBeast(flesh);
                }
            }
            if (synchronizeHost)
            {
                (flesh as FleshReplicaUnit)?.SyncTo(this.pawn, hd);
            }
            SetDirty();
            this.ParasitismHediffs.Add(hd);
            UpdateParasitismMood();
            AssignAngle();
            return true;
        }
        return false;
    }

    public void RemoveFlesh(ParasitismHediff hd, Thing pod)
    {
        Pawn flesh = hd.flesh;
        if (flesh != null)
        {
            (flesh as FleshReplicaUnit)?.ClearSync();
            if (!flesh.Spawned)
            {
                GenSpawn.Spawn(flesh, pod.Position, pod.Map);
            }
        }
        hd.flesh = null;
        if (flesh?.Map is Map map)
        {
            map.GetComponent<MapComponent_FleshHive>()?.RegisterFleshBeast(flesh);
            HCFGameUtility.AssignGroup(flesh, map, true);
        }
        this.pawn.health.RemoveHediff(hd);
        SetDirty();
        this.ParasitismHediffs.Remove(hd);
        UpdateParasitismMood();
        AssignAngle();
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        ReleaseAllParasitesOnDeath();
    }

    public void AssignAngle()
    {
        int index = 1;
        List<HediffComp_Parasitism> cs = this.ParasitismHediffs.ToList().ConvertAll(h => h.TryGetComp<HediffComp_Parasitism>());
        cs.RemoveAll(c => c == null);
        if (cs.NullOrEmpty())
        {
            return;
        }
        int count = cs.Sum(c => c.TentacleCount);
        foreach (var c in cs)
        {
            c.GetAngle(ref index, count);
        }
    }

    private void ReleaseAllParasitesOnDeath()
    {
        Map map = this.pawn.MapHeld;
        if (map == null)
        {
            return;
        }
        IntVec3 position = this.pawn.PositionHeld;
        foreach (ParasitismHediff hd in this.ParasitismHediffs.ToList())
        {
            ReleaseParasiteOnDeath(hd, position, map);
        }
        SetDirty();
        AssignAngle();
    }

    private void ReleaseParasiteOnDeath(ParasitismHediff hd, IntVec3 position, Map map)
    {
        if (hd == null)
        {
            return;
        }
        Pawn flesh = hd.flesh;
        hd.flesh = null;
        (flesh as FleshReplicaUnit)?.ClearSync();
        if (flesh != null && !flesh.Spawned)
        {
            GenSpawn.Spawn(flesh, position, map, WipeMode.VanishOrMoveAside);
            if (hd.lord != null && flesh.Faction != null && !flesh.Faction.IsPlayer && flesh.Faction.HostileTo(Faction.OfPlayer) && hd.lord.CanAddPawn(flesh))
            {
                hd.lord.AddPawn(flesh);
            }
            FleshbeastUtility.SpawnPawnAsFlyer(flesh, map, position, 5, true);
            if (!hd.parentChildParasite)
            {
                HCFGameUtility.AssignGroup(flesh, map, true);
            }
        }
        if (flesh != null && hd.parentChildParasite)
        {
            map.GetComponent<MapComponent_FleshHive>()?.RegisterFleshBeast(flesh);
            HCFGameUtility.AssignGroup(flesh, map, true);
        }
        this.pawn.health.RemoveHediff(hd);
        this.ParasitismHediffs.Remove(hd);
    }

    public void EnsureSynchronizedReplicaSpawned(Pawn flesh)
    {
        if (flesh.Spawned || this.pawn?.MapHeld == null)
        {
            return;
        }

        GenSpawn.Spawn(flesh, this.pawn.PositionHeld, this.pawn.MapHeld, WipeMode.VanishOrMoveAside);
        HCFGameUtility.AssignGroup(flesh, this.pawn.MapHeld, true);
    }

    public void SetDirty()
    {
        this.cacheLimit = -1;
        this.cacheCount = -1;
        this.cacheMaxTwistedFlesh = -1;
        this.cacheHasFleshwind = null;
        this.hds = null;
    }

    public void DebugParasite(PawnKindDef kindDef)
    {
        if (kindDef == null)
        {
            return;
        }
        Pawn flesh = PawnGenerator.GeneratePawn(kindDef, Faction.OfPlayer);
        if (flesh == null)
        {
            return;
        }
        ParasitismComp comp = flesh.TryGetComp<ParasitismComp>();
        if (comp == null || comp.Props.hediff == null)
        {
            flesh.Destroy();
            return;
        }
        if (flesh.Spawned)
        {
            flesh.DeSpawn();
        }
        EnsureAbilityTracker(this.pawn);
        ParasitismHediff hd = (ParasitismHediff)this.pawn.health.AddHediff(comp.Props.hediff);
        if (hd == null)
        {
            if (!flesh.Destroyed)
            {
                flesh.Destroy();
            }
            return;
        }
        hd.spaceCost = comp.Props.cost;
        hd.flesh = flesh;
        hd.lord = this.pawn.GetLord();
        if (comp.Props.synchronizeHost)
        {
            (flesh as FleshReplicaUnit)?.SyncTo(this.pawn, hd);
            EnsureSynchronizedReplicaSpawned(flesh);
        }
        SetDirty();
        this.ParasitismHediffs.Add(hd);
        UpdateParasitismMood();
        AssignAngle();
    }

    public static void EnsureAbilityTracker(Pawn pawn)
    {
        if (pawn.abilities == null)
        {
            pawn.abilities = new Pawn_AbilityTracker(pawn);
        }
    }

    private void UpdateParasitismMood()
    {
        if (pawn?.needs?.mood?.thoughts == null)
        {
            return;
        }

        Thought_Memory memory = pawn.needs.mood.thoughts.memories
            .GetFirstMemoryOfDef(FleshHiveDefOf.FH_Thought_FleshParasitism);
        if (ParasitismHediffs.Count == 0)
        {
            if (memory != null)
            {
                pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
            }
            return;
        }

        if (memory == null)
        {
            memory = (Thought_Memory)ThoughtMaker.MakeThought(FleshHiveDefOf.FH_Thought_FleshParasitism);
            memory.permanent = true;
            pawn.needs.mood.thoughts.memories.TryGainMemory(memory);
        }

        memory.moodOffset = -5 * ParasitismHediffs.Count;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref currentTwistedFlesh, "currentTwistedFlesh");
        Scribe_Values.Look(ref twistedFleshTargetValue, "twistedFleshTargetValue", 1f);
        Scribe_Values.Look(ref allowAutoRefillTwistedFlesh, "allowAutoRefillTwistedFlesh", true);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            UpdateParasitismMood();
            AssignAngle();
            if (this.pawn != null && this.pawn.Spawned && MaxTwistedFlesh > 0)
            {
                MapComponent_FleshHive comp = this.pawn.Map?.GetComponent<MapComponent_FleshHive>();
                comp?.RegisterTwistedFlesh(this.pawn);
            }
        }
    }

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        UpdateParasitismMood();
        if (this.pawn != null && this.pawn.Spawned && MaxTwistedFlesh > 0)
        {
            MapComponent_FleshHive comp = this.pawn.Map?.GetComponent<MapComponent_FleshHive>();
            comp?.RegisterTwistedFlesh(this.pawn);
        }
    }

    public override void PostTick()
    {
        base.PostTick();
        if (needAssignAngleAfterLoad)
        {
            needAssignAngleAfterLoad = false;
            SetDirty();
            AssignAngle();
            NotifyRenderTreeChanged();
        }
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        if (this.pawn != null)
        {
            MapComponent_FleshHive comp = this.pawn.Map?.GetComponent<MapComponent_FleshHive>();
            comp?.UnregisterTwistedFlesh(this.pawn);
        }
    }

    public void NotifyAngleAssignmentAfterLoad()
    {
        needAssignAngleAfterLoad = true;
    }

    private void NotifyRenderTreeChanged()
    {
        if (this.pawn == null)
        {
            return;
        }

        this.pawn.Drawer.renderer.renderTree.SetDirty();
        this.pawn.Drawer.renderer.EnsureGraphicsInitialized();
    }

    public HashSet<ParasitismHediff> hds = null;
    private int cacheCount = -1;
    int cacheLimit = -1;
    private int cacheMaxTwistedFlesh = -1;
    private bool? cacheHasFleshwind;
    private int currentTwistedFlesh;
    private float twistedFleshTargetValue = 1f;
    private bool allowAutoRefillTwistedFlesh = true;
    private bool needAssignAngleAfterLoad;
}
