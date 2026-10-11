using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

[DefOf]
public static class HostileFleshHiveDefOf
{
    static HostileFleshHiveDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(HostileFleshHiveDefOf));
    }

    [MayRequireAnomaly] public static ThingDef FH_HostileFleshHive = null!;
    [MayRequireAnomaly] public static ThingDef FH_HostileNerveCenter = null!;
    [MayRequireAnomaly] public static ThingDef FH_HostileFleshStore = null!;
    [MayRequireAnomaly] public static ThingDef FH_HostileFleshBarricade = null!;
    [MayRequireAnomaly] public static HiveResourceDef FH_Resource_HostileNutrition = null!;
    [MayRequireAnomaly] public static GroupWorkModeDef FH_GroupWorkMode_HostileHunt = null!;
    [MayRequireAnomaly] public static DutyDef FH_HostileHiveHunt = null!;
    [MayRequireAnomaly] public static JobDef FH_HostileHiveHaul = null!;
}
