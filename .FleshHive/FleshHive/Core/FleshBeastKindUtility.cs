using RimWorld;
using Verse;

namespace FleshHive;

[StaticConstructorOnStartup]
public static class FleshBeastKindUtility
{
    static FleshBeastKindUtility()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(FleshHiveDefOf));
        RegisterKinds();
    }

    public static IReadOnlyList<PawnKindDef> SmallKinds => smallKinds;

    public static IReadOnlyList<PawnKindDef> MediumKinds => mediumKinds;

    public static IReadOnlyList<PawnKindDef> LargeKinds => largeKinds;

    public static IReadOnlyList<PawnKindDef> GiantKinds => giantKinds;

    public static IReadOnlyList<PawnKindDef> KindsOfSize(FleshBeastSize size)
    {
        return size switch
        {
            FleshBeastSize.Small => SmallKinds,
            FleshBeastSize.Medium => MediumKinds,
            FleshBeastSize.Large => LargeKinds,
            FleshBeastSize.Giant => GiantKinds,
            _ => Array.Empty<PawnKindDef>()
        };
    }

    public static PawnKindDef RandomKind(FleshBeastSize size)
    {
        return KindsOfSizeMutable(size).RandomElement();
    }

    public static bool TryRandomKind(FleshBeastSize size, out PawnKindDef kind)
    {
        return KindsOfSize(size).TryRandomElement(out kind);
    }

    public static bool TryRandomKind(FleshBeastSize size, Predicate<PawnKindDef> validator, out PawnKindDef kind)
    {
        return KindsOfSize(size).Where(k => validator == null || validator(k)).TryRandomElement(out kind);
    }

    public static PawnKindDef RandomSmallKind()
    {
        return RandomKind(FleshBeastSize.Small);
    }

    public static PawnKindDef RandomMediumKind()
    {
        return RandomKind(FleshBeastSize.Medium);
    }

    public static PawnKindDef RandomLargeKind()
    {
        return RandomKind(FleshBeastSize.Large);
    }

    public static PawnKindDef RandomGiantKind()
    {
        return RandomKind(FleshBeastSize.Giant);
    }

    public static bool IsSize(PawnKindDef kind, FleshBeastSize size)
    {
        return SizeOf(kind) == size;
    }

    public static bool IsSmall(PawnKindDef kind)
    {
        return IsSize(kind, FleshBeastSize.Small);
    }

    public static bool IsMedium(PawnKindDef kind)
    {
        return IsSize(kind, FleshBeastSize.Medium);
    }

    public static bool IsLarge(PawnKindDef kind)
    {
        return IsSize(kind, FleshBeastSize.Large);
    }

    public static bool IsGiant(PawnKindDef kind)
    {
        return IsSize(kind, FleshBeastSize.Giant);
    }

    public static FleshBeastSize? SizeOf(PawnKindDef kind)
    {
        return kind?.race?.comps?.OfType<CompProperties_FleshBeastCache>().LastOrDefault(props => props.size.HasValue)?.size;
    }

    private static void RegisterKinds()
    {
        foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            FleshBeastSize? size = SizeOf(kind);
            if (size.HasValue)
            {
                KindsOfSizeMutable(size.Value).Add(kind);
            }
        }
    }

    private static List<PawnKindDef> KindsOfSizeMutable(FleshBeastSize size)
    {
        return size switch
        {
            FleshBeastSize.Small => smallKinds,
            FleshBeastSize.Medium => mediumKinds,
            FleshBeastSize.Large => largeKinds,
            FleshBeastSize.Giant => giantKinds,
            _ => smallKinds
        };
    }

    private static readonly List<PawnKindDef> smallKinds = new List<PawnKindDef>();
    private static readonly List<PawnKindDef> mediumKinds = new List<PawnKindDef>();
    private static readonly List<PawnKindDef> largeKinds = new List<PawnKindDef>();
    private static readonly List<PawnKindDef> giantKinds = new List<PawnKindDef>();
}

public enum FleshBeastSize
{
    Small,
    Medium,
    Large,
    Giant
}
