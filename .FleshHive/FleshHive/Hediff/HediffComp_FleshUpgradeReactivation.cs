using RimWorld;
using Verse;

namespace FleshHive;

public class HediffCompProperties_FleshUpgradeReactivation : HediffCompProperties
{
    public HediffCompProperties_FleshUpgradeReactivation()
    {
        compClass = typeof(HediffComp_FleshUpgradeReactivation);
    }

    public PawnKindDef? spawnKind;
    public float chance = 0.1f;
}

public class HediffComp_FleshUpgradeReactivation : HediffComp
{
    public HediffCompProperties_FleshUpgradeReactivation Props =>
        (HediffCompProperties_FleshUpgradeReactivation)props;

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff? culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        Map map = Pawn.MapHeld;
        if (Pawn.Faction != Faction.OfPlayer || map == null || !Rand.Chance(Props.chance))
        {
            return;
        }

        if (Props.spawnKind == null)
        {
            Log.Error("[FleshHive] Reactivation upgrade Hediff is missing its spawnKind.");
            return;
        }

        Pawn spawnedPawn = FleshHiveFleshbeastSpawnUtility.GeneratePawn(Props.spawnKind, Faction.OfPlayer);
        GenSpawn.Spawn(spawnedPawn, Pawn.PositionHeld, map);
        MapComponent_FleshHive mapComp = map.GetComponent<MapComponent_FleshHive>();
        if (mapComp?.group is UnitGroup_TemporaryFleshHive group)
        {
            group.AcceptUnit(spawnedPawn);
        }
        else
        {
            Log.Error($"[FleshHive] Cannot assign reactivated {spawnedPawn.def.defName} to the temporary fleshbeast group.");
        }
        mapComp?.GrantFleshBeastUpgradeHediffs(spawnedPawn);
    }
}
