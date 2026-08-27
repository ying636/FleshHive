using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints))]
public static class Patch_PawnGroupMakerUtility_ChoosePawnGenOptionsByPoints_FleshParasites
{
    public static bool Prefix(float pointsTotal, List<PawnGenOption> options, PawnGroupMakerParms groupParms, ref IEnumerable<PawnGenOptionWithXenotype> __result)
    {
        return FleshParasiteRaidGenerator.TryChoosePawnGenOptionsByPoints(pointsTotal, options, groupParms, ref __result);
    }
}

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
public static class Patch_PawnGenerator_GeneratePawn_FleshParasites
{
    public static void Postfix(PawnGenerationRequest request, Pawn __result)
    {
        FleshParasiteRaidGenerator.TryApplyParasites(request, __result);
    }
}

public static class FleshParasiteRaidGenerator
{
    public static bool TryChoosePawnGenOptionsByPoints(float pointsTotal, List<PawnGenOption> options, PawnGroupMakerParms groupParms, ref IEnumerable<PawnGenOptionWithXenotype> result)
    {
        if (groupParms?.faction == null)
        {
            return true;
        }
        if (groupParms.seed.HasValue)
        {
            Rand.PushState(groupParms.seed.Value);
        }
        try
        {
            List<PlannedPawnGenOption> candidates = new List<PlannedPawnGenOption>();
            List<PawnGenOptionWithXenotype> chosenOptions = new List<PawnGenOptionWithXenotype>();
            List<PawnKindDef> chosenHostKinds = new List<PawnKindDef>();
            List<PawnKindDef[]> chosenParasites = new List<PawnKindDef[]>();
            float pointsLeft = pointsTotal;
            bool leaderChosen = false;
            float highestCost = -1f;
            currentFaction = groupParms.faction;
            while (true)
            {
            candidates.Clear();
            foreach (PawnGenOptionWithXenotype option in PawnGroupMakerUtility.GetOptions(groupParms, groupParms.faction.def, options, pointsTotal, pointsLeft, null, chosenOptions, leaderChosen))
            {
                PawnKindDef[] parasites = GenerateParasitePlan(option.Option.kind);
                float cost = option.Cost + ParasiteCost(parasites);
                if (cost > pointsLeft)
                {
                        continue;
                    }
                    if (cost > highestCost)
                    {
                        highestCost = cost;
                    }
                    candidates.Add(new PlannedPawnGenOption(option, parasites, cost));
                }
                Func<PlannedPawnGenOption, float> weightSelector = candidate =>
                    !PawnGroupMakerUtility.PawnGenOptionValid(candidate.Option.Option, groupParms, chosenOptions)
                        ? 0f
                        : candidate.Option.SelectionWeight * PawnWeightFactorByMostExpensivePawnCostFractionCurve.Evaluate(candidate.Cost / highestCost);
                if (!candidates.TryRandomElementByWeight(weightSelector, out PlannedPawnGenOption chosen))
                {
                    break;
                }
                chosenOptions.Add(chosen.Option);
                chosenHostKinds.Add(chosen.Option.Option.kind);
                chosenParasites.Add(chosen.Parasites);
                pointsLeft -= chosen.Cost;
                if (chosen.Option.Option.kind.factionLeader)
                {
                    leaderChosen = true;
                }
            }
            if (chosenOptions.Count == 1 && pointsLeft > pointsTotal / 2f)
            {
                Log.Warning($"Used only {pointsTotal - pointsLeft} / {pointsTotal} points generating for {groupParms.faction}");
            }
            plannedParasites.Clear();
            plannedHostKinds.Clear();
            plannedHostKinds.AddRange(chosenHostKinds);
            plannedParasites.AddRange(chosenParasites);
            plannedFaction = groupParms.faction;
            result = chosenOptions;
            return false;
        }
        finally
        {
            if (groupParms.seed.HasValue)
            {
                Rand.PopState();
            }
            currentFaction = null;
        }
    }

    public static void TryApplyParasites(PawnGenerationRequest request, Pawn pawn)
    {
        if (applyingParasites || pawn == null || request.KindDef != pawn.kindDef)
        {
            return;
        }
        if (request.KindDef == FleshHiveDefOf.FH_Fleshwind && (request.Faction != plannedFaction || plannedParasites.Count == 0 || plannedHostKinds.Count == 0 || plannedHostKinds[0] != request.KindDef))
        {
            ApplyParasites(pawn, OneSmall());
            return;
        }
        if (request.Faction != plannedFaction || plannedParasites.Count == 0 || plannedHostKinds.Count == 0)
        {
            return;
        }
        if (plannedHostKinds[0] != request.KindDef)
        {
            return;
        }
        PawnKindDef[] parasites = plannedParasites[0];
        plannedHostKinds.RemoveAt(0);
        plannedParasites.RemoveAt(0);
        if (plannedParasites.Count == 0)
        {
            plannedFaction = null;
        }
        if (parasites.NullOrEmpty())
        {
            return;
        }
        ApplyParasites(pawn, parasites);
    }

    private static void ApplyParasites(Pawn pawn, PawnKindDef[] parasites)
    {
        if (parasites.NullOrEmpty())
        {
            return;
        }
        ParasitismSystem system = pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            system = (ParasitismSystem)pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
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

    private static PawnKindDef[] GenerateParasitePlan(PawnKindDef hostKind)
    {
        if (!IsEligibleHost(hostKind, currentFaction))
        {
            return Array.Empty<PawnKindDef>();
        }
        PawnKindDef[] parasites = GenerateParasitesFor(hostKind);
        return CanFitParasites(hostKind, parasites) ? parasites : Array.Empty<PawnKindDef>();
    }

    private static PawnKindDef[] GenerateParasitesFor(PawnKindDef hostKind)
    {
        if (IsCultist(hostKind, currentFaction))
        {
            if (!Rand.Chance(FleshHiveMod.Settings.raidParasiteChance))
            {
                return Array.Empty<PawnKindDef>();
            }
            int option = Rand.RangeInclusive(0, 2);
            if (option == 0)
            {
                return OneSmall();
            }
            if (option == 1)
            {
                return TwoSmall();
            }
            return OneMedium();
        }
        if (FleshBeastKindUtility.IsSize(hostKind, FleshBeastSize.Medium))
        {
            return Rand.Chance(ParasiteChanceFor(hostKind)) ? OneSmall() : Array.Empty<PawnKindDef>();
        }
        if (FleshBeastKindUtility.IsSize(hostKind, FleshBeastSize.Large))
        {
            if (!Rand.Chance(ParasiteChanceFor(hostKind)))
            {
                return Array.Empty<PawnKindDef>();
            }
            int option = Rand.RangeInclusive(0, 4);
            if (option == 0)
            {
                return OneSmall();
            }
            if (option == 1)
            {
                return TwoSmall();
            }
            if (option == 2)
            {
                return ThreeSmall();
            }
            if (option == 3)
            {
                PawnKindDef[] oneSmall = OneSmall();
                PawnKindDef[] oneMedium = OneMedium();
                if (oneSmall.NullOrEmpty())
                {
                    return oneMedium;
                }
                if (oneMedium.NullOrEmpty())
                {
                    return oneSmall;
                }
                return new[] { oneSmall[0], oneMedium[0] };
            }
            return OneMedium();
        }
        return Array.Empty<PawnKindDef>();
    }

    private static bool IsCultist(PawnKindDef kind, Faction faction)
    {
        return faction?.def?.defName == "HoraxCult";
    }

    private static bool IsEligibleHost(PawnKindDef kind, Faction faction)
    {
        return IsCultist(kind, faction)
            || FleshBeastKindUtility.IsSize(kind, FleshBeastSize.Medium)
            || FleshBeastKindUtility.IsSize(kind, FleshBeastSize.Large);
    }

    private static float ParasiteChanceFor(PawnKindDef hostKind)
    {
        return hostKind == FleshHiveDefOf.FH_Fleshwind ? 1f : FleshHiveMod.Settings.raidParasiteChance;
    }

    private static bool CanFitParasites(PawnKindDef hostKind, PawnKindDef[] parasites)
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

    private static int ParasiteSpaceCost(PawnKindDef[] parasites)
    {
        int cost = 0;
        for (int i = 0; i < parasites.Length; i++)
        {
            cost += parasites[i].race?.GetCompProperties<ParasitismCompProperties>()?.cost ?? 1;
        }
        return cost;
    }

    private static float ParasiteCost(PawnKindDef[] parasites)
    {
        float cost = 0f;
        for (int i = 0; i < parasites.Length; i++)
        {
            cost += parasites[i].combatPower;
        }
        return cost;
    }

    private static bool IsValidParasiteKind(PawnKindDef kind)
    {
        return kind?.race?.GetCompProperties<ParasitismCompProperties>() != null;
    }

    private static PawnKindDef[] OneSmall()
    {
        return RandomParasites(FleshBeastSize.Small, 1);
    }

    private static PawnKindDef[] TwoSmall()
    {
        return RandomParasites(FleshBeastSize.Small, 2);
    }

    private static PawnKindDef[] ThreeSmall()
    {
        return RandomParasites(FleshBeastSize.Small, 3);
    }

    private static PawnKindDef[] OneMedium()
    {
        return RandomParasites(FleshBeastSize.Medium, 1);
    }

    private static PawnKindDef[] RandomParasites(FleshBeastSize size, int count)
    {
        List<PawnKindDef> kinds = new List<PawnKindDef>();
        for (int i = 0; i < count; i++)
        {
            if (FleshHiveFleshbeastSpawnUtility.TryRandomKind(FleshBeastKindUtility.KindsOfSize(size), IsValidParasiteKind, out PawnKindDef kind))
            {
                kinds.Add(kind);
            }
        }
        return kinds.ToArray();
    }

    private static readonly SimpleCurve PawnWeightFactorByMostExpensivePawnCostFractionCurve = new SimpleCurve
    {
        new CurvePoint(0.2f, 0.01f),
        new CurvePoint(0.3f, 0.3f),
        new CurvePoint(0.5f, 1f)
    };

    private static readonly List<PawnKindDef[]> plannedParasites = new List<PawnKindDef[]>();
    private static readonly List<PawnKindDef> plannedHostKinds = new List<PawnKindDef>();

    private static bool applyingParasites;
    private static Faction plannedFaction;
    private static Faction currentFaction;

    private readonly struct PlannedPawnGenOption
    {
        public PlannedPawnGenOption(PawnGenOptionWithXenotype option, PawnKindDef[] parasites, float cost)
        {
            Option = option;
            Parasites = parasites;
            Cost = cost;
        }

        public PawnGenOptionWithXenotype Option { get; }
        public PawnKindDef[] Parasites { get; }
        public float Cost { get; }
    }
}
