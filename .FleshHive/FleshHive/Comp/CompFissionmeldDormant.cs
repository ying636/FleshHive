using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class CompProperties_FissionmeldDormant : CompProperties
{
    public CompProperties_FissionmeldDormant()
    {
        this.compClass = typeof(CompFissionmeldDormant);
    }

    public PawnKindDef resurrectKind;

    public List<PawnKindDef> spawnOptions;

    public IntRange spawnPointsRange = new IntRange(100, 300);

    public int spawnRadius = 5;

    public int spawnIntervalTicks = 24000;

    public int resurrectTicks = 60000;
}

public class CompFissionmeldDormant : ThingComp, IThingHolder
{
    public CompFissionmeldDormant()
    {
        corpseContainer = new ThingOwner<Corpse>(this, oneStackOnly: true, contentsLookMode: LookMode.Deep, removeContentsIfDestroyed: false);
    }

    private CompProperties_FissionmeldDormant Props => (CompProperties_FissionmeldDormant)this.props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (corpseContainer == null)
        {
            corpseContainer = new ThingOwner<Corpse>(this, oneStackOnly: true, contentsLookMode: LookMode.Deep, removeContentsIfDestroyed: false);
        }
        if (!respawningAfterLoad)
        {
            ticksToResurrect = Props.resurrectTicks;
            ticksToNextSpawn = Props.spawnIntervalTicks;
        }
    }

    public void StoreCorpse(Corpse corpse)
    {
        if (corpse == null || corpse.Destroyed || corpse.InnerPawn == null)
        {
            Log.Error("[FleshHive] Cannot store an invalid fissionmeld corpse.");
            return;
        }

        Pawn pawn = corpse.InnerPawn;
        CompFissionmeldState state = pawn.TryGetComp<CompFissionmeldState>();
        if (state != null && state.DormantHitPoints < 0)
        {
            state.DormantHitPoints = parent.MaxHitPoints;
        }

        if (!corpseContainer.TryAddOrTransfer(corpse))
        {
            Log.Error("[FleshHive] Failed to transfer the fissionmeld corpse into its dormant building.");
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!this.parent.Spawned)
        {
            return;
        }

        ticksToResurrect--;
        ticksToNextSpawn--;
        if (ticksToNextSpawn <= 0 && ticksToResurrect > 0)
        {
            SpawnFleshbeasts();
            ticksToNextSpawn = Props.spawnIntervalTicks;
        }
        if (ticksToResurrect <= 0)
        {
            ResurrectFissionmeld();
        }
    }

    public override string CompInspectStringExtra()
    {
        if (ticksToResurrect <= 0)
        {
            return null;
        }
        return "FH_FissionmeldDormant_ResurrectsIn".Translate(ticksToResurrect.ToStringTicksToPeriod());
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref corpseContainer, "corpseContainer", this);
        Scribe_Values.Look(ref ticksToResurrect, "ticksToResurrect", 60000);
        Scribe_Values.Look(ref ticksToNextSpawn, "ticksToNextSpawn", 24000);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return corpseContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, corpseContainer);
    }

    private void SpawnFleshbeasts()
    {
        Faction faction = this.parent.Faction ?? Faction.OfEntities;
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByPoints(Props.spawnOptions, Props.spawnPointsRange, faction, this.parent.PositionHeld, this.parent.MapHeld, Props.spawnRadius, sourcePawn: null);
    }

    private void ResurrectFissionmeld()
    {
        Map map = this.parent.MapHeld;
        if (map == null || !corpseContainer.Any)
        {
            return;
        }

        IntVec3 position = this.parent.PositionHeld;
        Corpse corpse = corpseContainer[0];
        Pawn pawn = corpse.InnerPawn;
        if (pawn == null)
        {
            Log.Error("[FleshHive] Dormant fissionmeld has no inner pawn to resurrect.");
            return;
        }

        CompFissionmeldState state = pawn.TryGetComp<CompFissionmeldState>();
        if (state != null)
        {
            state.DormantHitPoints = parent.HitPoints;
        }

        corpseContainer.TryDropAll(position, map, ThingPlaceMode.Near);
        if (!corpse.Spawned)
        {
            Log.Error("[FleshHive] Failed to place the stored fissionmeld corpse before resurrection.");
            return;
        }
        bool resurrected = ResurrectionUtility.TryResurrect(pawn);
        if (!resurrected)
        {
            Log.Error("[FleshHive] Failed to resurrect the stored fissionmeld pawn.");
            return;
        }

        this.parent.Destroy(DestroyMode.Vanish);
        if (pawn.Spawned)
        {
            HCFGameUtility.AssignGroup(pawn, map, true);
            map.GetComponent<MapComponent_FleshHive>()?.GrantFleshBeastUpgradeHediffs(pawn);
            TryAssignEnemyLord(pawn, map);
        }
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

    private int ticksToResurrect;

    private int ticksToNextSpawn;

    private ThingOwner<Corpse> corpseContainer;
}
