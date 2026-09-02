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

public class CompFissionmeldDormant : ThingComp
{
    private CompProperties_FissionmeldDormant Props => (CompProperties_FissionmeldDormant)this.props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            ticksToResurrect = Props.resurrectTicks;
            ticksToNextSpawn = Props.spawnIntervalTicks;
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
        Scribe_Values.Look(ref ticksToResurrect, "ticksToResurrect", 60000);
        Scribe_Values.Look(ref ticksToNextSpawn, "ticksToNextSpawn", 24000);
    }

    private void SpawnFleshbeasts()
    {
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByPoints(Props.spawnOptions, Props.spawnPointsRange, this.parent.Faction, this.parent.PositionHeld, this.parent.MapHeld, Props.spawnRadius);
    }

    private void ResurrectFissionmeld()
    {
        Map map = this.parent.MapHeld;
        if (map == null || Props.resurrectKind == null)
        {
            return;
        }

        IntVec3 position = this.parent.PositionHeld;
        Faction faction = this.parent.Faction;
        Pawn pawn = PawnGenerator.GeneratePawn(GenerateRequest(Props.resurrectKind, faction));
        FleshParasiteUtility.TryApplyDefaultParasites(pawn);
        this.parent.Destroy(DestroyMode.Vanish);
        GenSpawn.Spawn(pawn, position, map, WipeMode.VanishOrMoveAside);
        HCFGameUtility.AssignGroup(pawn, map, true);
        TryAssignEnemyLord(pawn, map);
    }

    private static PawnGenerationRequest GenerateRequest(PawnKindDef kind, Faction faction)
    {
        return new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, null, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, 0f, 0f, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false);
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
}
