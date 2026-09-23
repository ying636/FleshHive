using System.Text;
using RimWorld;
using Verse;

namespace FleshHive;

public class Hediff_FleshRetainer : HediffWithComps, IShardExpandableParasiteCapacity
{
    public int ParasiteCapacity => parasiteCapacity;

    public int MaximumParasiteCapacity => MaxParasiteCapacity;

    public bool CanIncreaseParasiteCapacity => parasiteCapacity < MaxParasiteCapacity;

    public HediffDef ShardComaDef => FleshHiveDefOf.FH_FleshRetainerShardComa;

    public override string TipStringExtra
    {
        get
        {
            StringBuilder builder = new StringBuilder();
            string baseTip = base.TipStringExtra;
            if (!baseTip.NullOrEmpty())
            {
                builder.AppendLine(baseTip.TrimEnd());
            }
            builder.AppendLine("FH_ParasiteCapacity_Info".Translate(ParasiteCapacity, MaxParasiteCapacity));
            return builder.ToString().TrimEnd();
        }
    }

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        ParasitismSystem? system = pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            system = pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        }
        if (system == null)
        {
            Log.Error($"[FleshHive] Failed to add parasitism system to flesh retainer {pawn}.");
            return;
        }

        system.SetDirty();
        if (!initialParasiteGenerated)
        {
            GenerateInitialParasite(system);
        }
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        ParasitismSystem? system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        system?.SetDirty();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref parasiteCapacity, "fleshRetainerParasiteCapacity", InitialParasiteCapacity);
        Scribe_Values.Look(ref initialParasiteGenerated, "fleshRetainerInitialParasiteGenerated");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            parasiteCapacity = Math.Max(InitialParasiteCapacity, Math.Min(MaxParasiteCapacity, parasiteCapacity));
            ParasitismSystem? system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
            system?.SetDirty();
        }
    }

    public bool TryIncreaseParasiteCapacity()
    {
        if (!CanIncreaseParasiteCapacity)
        {
            return false;
        }

        parasiteCapacity = Math.Min(MaxParasiteCapacity, parasiteCapacity + ParasiteCapacityIncrease);
        ParasitismSystem? system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        system?.SetDirty();
        return true;
    }

    private void GenerateInitialParasite(ParasitismSystem system)
    {
        if (!FleshBeastKindUtility.TryRandomKind(FleshBeastSize.Small,
                kind => kind.race?.GetCompProperties<ParasitismCompProperties>()?.hediff != null,
                out PawnKindDef kind))
        {
            Log.Error($"[FleshHive] No parasitic small fleshbeast is available for flesh retainer {pawn}.");
            return;
        }

        Pawn parasite = PawnGenerator.GeneratePawn(kind, pawn.Faction);
        if (!system.Parasite(parasite))
        {
            if (!parasite.Destroyed)
            {
                parasite.Destroy();
            }
            Log.Error($"[FleshHive] Failed to give initial parasite {kind.defName} to flesh retainer {pawn}.");
            return;
        }

        initialParasiteGenerated = true;
    }

    private const int InitialParasiteCapacity = 4;
    private const int MaxParasiteCapacity = 10;
    private const int ParasiteCapacityIncrease = 2;
    private int parasiteCapacity = InitialParasiteCapacity;
    private bool initialParasiteGenerated;
}
