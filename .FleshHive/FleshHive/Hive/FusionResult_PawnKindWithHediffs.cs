using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class FusionResult_PawnKindWithHediffs : FusionResult_PawnKind
{
    public override void Do(Thing fuser, List<Thing> materials)
    {
        bool runaway = this.hediffs.Any(hediff => hediff?.defName == "FH_Trait_Runaway");
        Pawn pawn = runaway
            ? PawnGenerator.GeneratePawn(this.kind, Faction.OfEntities)
            : HCFGameUtility.SpawnUnit(fuser, this.kind);
        if (pawn?.health == null)
        {
            return;
        }

        if (runaway)
        {
            fuser.TryGetComp<CompHiveUpgrader>()?.Notify_UnitCompleted(pawn);
        }

        foreach (HediffDef hediff in this.hediffs)
        {
            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }
        }

        if (runaway)
        {
            GenSpawn.Spawn(pawn, fuser.Position, fuser.Map);
            LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_FleshbeastAssault(), fuser.Map, new[] { pawn });
            fuser.Map.GetComponent<MapComponent_Unit>().RequestUnitCacheRefresh();
        }
    }

    public List<HediffDef> hediffs = new List<HediffDef>();
}
