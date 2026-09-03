using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace FleshHive;

public class Hediff_Hela : HediffWithComps
{
    public int ParasiteCapacity => parasiteCapacity;

    public int MaximumParasiteCapacity => MaxParasiteCapacity;

    public int TwistedFleshCapacity => BaseTwistedFleshCapacity;

    public int ActiveParasiteCapacity
    {
        get
        {
            ParasitismSystem system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
            return system?.Count ?? 0;
        }
    }

    public float BodyPartHealthFactor => 1f + ActiveParasiteCapacity * BodyPartHealthFactorPerCapacity;

    public override float PainFactor => 1f / BodyPartHealthFactor;

    public bool CanIncreaseParasiteCapacity => parasiteCapacity < MaxParasiteCapacity;

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
            builder.AppendLine("FH_Hela_ParasiteCapacityInfo".Translate(ParasiteCapacity, MaxParasiteCapacity));
            builder.AppendLine("FH_Hela_BodyPartHealthInfo".Translate(
                ActiveParasiteCapacity,
                BodyPartHealthFactor.ToStringPercent(),
                PainFactor.ToStringPercent()));
            return builder.ToString().TrimEnd();
        }
    }

    public static Hediff_Hela? GetCached(Pawn? pawn)
    {
        if (pawn != null && helaCache.TryGetValue(pawn, out Hediff_Hela hela))
        {
            return hela;
        }
        return null;
    }

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        RegisterCache();
        ParasitismSystem system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        system?.SetDirty();
    }

    public override void PostRemoved()
    {
        UnregisterCache();
        base.PostRemoved();
        ParasitismSystem system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        system?.SetDirty();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref parasiteCapacity, "helaParasiteCapacity", InitialParasiteCapacity);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            parasiteCapacity = Math.Max(InitialParasiteCapacity, Math.Min(MaxParasiteCapacity, parasiteCapacity));
            RegisterCache();
        }
    }

    public bool TryIncreaseParasiteCapacity()
    {
        if (!CanIncreaseParasiteCapacity)
        {
            return false;
        }

        parasiteCapacity = Math.Min(MaxParasiteCapacity, parasiteCapacity + ParasiteCapacityIncrease);
        ParasitismSystem system = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        system?.SetDirty();
        return true;
    }

    private void RegisterCache()
    {
        if (pawn == null)
        {
            return;
        }

        helaCache.Remove(pawn);
        helaCache.Add(pawn, this);
    }

    private void UnregisterCache()
    {
        if (pawn != null
            && helaCache.TryGetValue(pawn, out Hediff_Hela cached)
            && ReferenceEquals(cached, this))
        {
            helaCache.Remove(pawn);
        }
    }

    private const int InitialParasiteCapacity = 4;
    private const int MaxParasiteCapacity = 12;
    private const int ParasiteCapacityIncrease = 2;
    private const int BaseTwistedFleshCapacity = 100;
    private const float BodyPartHealthFactorPerCapacity = 0.25f;
    private int parasiteCapacity = InitialParasiteCapacity;
    private static readonly ConditionalWeakTable<Pawn, Hediff_Hela> helaCache = new ConditionalWeakTable<Pawn, Hediff_Hela>();
}
