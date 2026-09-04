using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public static class FleshParasiteUtility
{
    static FleshParasiteUtility()
    {
        motherParasiteKinds.AddRange(FleshBeastKindUtility.KindsOfSize(FleshBeastSize.Small).Where(IsValidParasiteKind));
        motherParasiteKinds.AddRange(FleshBeastKindUtility.KindsOfSize(FleshBeastSize.Medium).Where(IsValidParasiteKind));
    }

    public static bool IsApplying => applyingParasites;

    public static PawnKindDef[] GenerateParasitePlan(PawnKindDef hostKind, Faction faction, bool fillMotherCapacity, bool ignoreChance = false)
    {
        if (!IsEligibleHost(hostKind, faction))
        {
            return System.Array.Empty<PawnKindDef>();
        }
        if (!ignoreChance && !Rand.Chance(ParasiteChanceFor(hostKind)))
        {
            return System.Array.Empty<PawnKindDef>();
        }

        if (fillMotherCapacity && FleshBeastKindUtility.IsGiant(hostKind))
        {
            return GenerateMotherParasites(hostKind);
        }

        if (IsCultist(hostKind, faction))
        {
            int option = Rand.RangeInclusive(0, 2);
            if (option == 0)
            {
                return RandomParasites(FleshBeastSize.Small, 1);
            }
            if (option == 1)
            {
                return RandomParasites(FleshBeastSize.Small, 2);
            }
            return RandomParasites(FleshBeastSize.Medium, 1);
        }
        if (FleshBeastKindUtility.IsMedium(hostKind))
        {
            return RandomParasites(FleshBeastSize.Small, 1);
        }
        if (FleshBeastKindUtility.IsLarge(hostKind))
        {
            int option = Rand.RangeInclusive(0, 4);
            if (option == 0)
            {
                return RandomParasites(FleshBeastSize.Small, 1);
            }
            if (option == 1)
            {
                return RandomParasites(FleshBeastSize.Small, 2);
            }
            if (option == 2)
            {
                return RandomParasites(FleshBeastSize.Small, 3);
            }
            if (option == 3)
            {
                PawnKindDef[] oneSmall = RandomParasites(FleshBeastSize.Small, 1);
                PawnKindDef[] oneMedium = RandomParasites(FleshBeastSize.Medium, 1);
                if (oneSmall.Length == 0)
                {
                    return oneMedium;
                }
                if (oneMedium.Length == 0)
                {
                    return oneSmall;
                }
                return new[] { oneSmall[0], oneMedium[0] };
            }
            return RandomParasites(FleshBeastSize.Medium, 1);
        }
        return System.Array.Empty<PawnKindDef>();
    }

    public static void TryApplyDefaultParasites(Pawn pawn)
    {
        if (pawn == null)
        {
            return;
        }
        if (pawn.Faction?.IsPlayer == true)
        {
            return;
        }
        if (pawn.RaceProps?.FleshType != FleshTypeDefOf.Fleshbeast)
        {
            return;
        }
        if (pawn.health?.hediffSet == null)
        {
            return;
        }
        if (pawn.health.hediffSet.HasHediff(FleshHiveDefOf.FH_ParasitismSystem))
        {
            return;
        }

        PawnKindDef[] parasites = GenerateParasitePlan(pawn.kindDef, pawn.Faction, true);
        if (parasites.NullOrEmpty())
        {
            return;
        }
        if (!CanFitParasites(pawn.kindDef, parasites))
        {
            return;
        }
        ApplyParasites(pawn, parasites);
    }

    public static void ApplyParasites(Pawn pawn, IReadOnlyList<PawnKindDef> parasites)
    {
        if (pawn?.health?.hediffSet == null || parasites == null || parasites.Count == 0)
        {
            return;
        }
        ParasitismSystem system = pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            system = (ParasitismSystem)pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
        }
        if (system == null)
        {
            return;
        }
        applyingParasites = true;
        try
        {
            foreach (PawnKindDef parasiteKind in parasites)
            {
                Pawn parasite = PawnGenerator.GeneratePawn(parasiteKind, pawn.Faction);
                if (!system.Parasite(parasite, true) && !parasite.Destroyed)
                {
                    parasite.Destroy();
                }
            }
        }
        finally
        {
            applyingParasites = false;
        }
    }

    public static int ParasiteSpaceCost(IReadOnlyList<PawnKindDef> parasites)
    {
        int cost = 0;
        for (int i = 0; i < parasites.Count; i++)
        {
            cost += parasites[i]?.race?.GetCompProperties<ParasitismCompProperties>()?.cost ?? 1;
        }
        return cost;
    }

    private static PawnKindDef[] GenerateMotherParasites(PawnKindDef hostKind)
    {
        int remaining = HostParasitismCapacity(hostKind);
        List<PawnKindDef> result = new List<PawnKindDef>();
        while (remaining > 0)
        {
            if (!TryRandomMotherParasite(remaining, out PawnKindDef parasite))
            {
                break;
            }
            result.Add(parasite);
            remaining -= ParasiteCost(parasite);
        }
        if (remaining > 0)
        {
            Log.Warning($"[FleshHive] Mother parasite plan could not fill capacity: host={hostKind?.defName ?? "null"}, capacity={HostParasitismCapacity(hostKind)}, remaining={remaining}");
        }
        return result.ToArray();
    }

    private static bool TryRandomMotherParasite(int remaining, out PawnKindDef parasite)
    {
        parasite = null;
        int validCount = 0;
        foreach (PawnKindDef candidate in motherParasiteKinds)
        {
            if (ParasiteCost(candidate) > remaining || !Rand.Chance(1f / (++validCount)))
            {
                continue;
            }
            parasite = candidate;
        }
        return parasite != null;
    }

    private static bool IsCultist(PawnKindDef kind, Faction faction)
    {
        return faction?.def?.defName == "HoraxCult";
    }

    private static bool IsEligibleHost(PawnKindDef kind, Faction faction)
    {
        return IsCultist(kind, faction)
            || FleshBeastKindUtility.IsMedium(kind)
            || FleshBeastKindUtility.IsLarge(kind)
            || FleshBeastKindUtility.IsGiant(kind);
    }

    private static float ParasiteChanceFor(PawnKindDef hostKind)
    {
        return hostKind == FleshHiveDefOf.FH_Fleshwind ? 1f : FleshHiveMod.Settings.raidParasiteChance;
    }

    private static bool CanFitParasites(PawnKindDef hostKind, IReadOnlyList<PawnKindDef> parasites)
    {
        return ParasiteSpaceCost(parasites) <= HostParasitismCapacity(hostKind);
    }

    private static int HostParasitismCapacity(PawnKindDef hostKind)
    {
        if (hostKind?.RaceProps == null)
        {
            return 1;
        }
        float capacity = hostKind.race.GetStatValueAbstract(FleshHiveDefOf.FH_Stat_ParasitismCapacity);
        return Mathf.Min(Mathf.FloorToInt(capacity), 14);
    }

    private static int ParasiteCost(PawnKindDef kind)
    {
        return kind?.race?.GetCompProperties<ParasitismCompProperties>()?.cost ?? 1;
    }

    private static PawnKindDef[] RandomParasites(FleshBeastSize size, int count)
    {
        List<PawnKindDef> kinds = GetParasiteKinds(size);
        List<PawnKindDef> result = new List<PawnKindDef>(count);
        for (int i = 0; i < count; i++)
        {
            if (kinds.TryRandomElement(out PawnKindDef kind))
            {
                result.Add(kind);
            }
        }
        return result.ToArray();
    }

    private static List<PawnKindDef> GetParasiteKinds(FleshBeastSize size)
    {
        if (size == FleshBeastSize.Small)
        {
            smallParasiteKinds ??= FleshBeastKindUtility.KindsOfSize(size).Where(IsValidParasiteKind).ToList();
            return smallParasiteKinds;
        }
        mediumParasiteKinds ??= FleshBeastKindUtility.KindsOfSize(size).Where(IsValidParasiteKind).ToList();
        return mediumParasiteKinds;
    }

    private static bool IsValidParasiteKind(PawnKindDef kind)
    {
        return kind?.defName != "FH_FleshReplica"
            && kind?.race?.GetCompProperties<ParasitismCompProperties>() != null;
    }

    private static readonly List<PawnKindDef> motherParasiteKinds = new List<PawnKindDef>();

    private static List<PawnKindDef> smallParasiteKinds;
    private static List<PawnKindDef> mediumParasiteKinds;
    private static bool applyingParasites;

}
