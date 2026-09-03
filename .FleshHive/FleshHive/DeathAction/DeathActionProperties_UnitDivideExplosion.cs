using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class DeathActionProperties_UnitDivideExplosion : DeathActionProperties_UnitDivide
{
    public DeathActionProperties_UnitDivideExplosion()
    {
        workerClass = typeof(DeathActionWorker_UnitDivideExplosion);
    }

    public int baseDamage = 50;

    public float explosionRadius = 4.5f;
}

public class DeathActionWorker_UnitDivideExplosion : DeathActionWorker_UnitDivide
{
    public new DeathActionProperties_UnitDivideExplosion Props => (DeathActionProperties_UnitDivideExplosion)props;

    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        Map map = corpse.MapHeld;
        HashSet<Pawn> existingPawns = map == null
            ? new HashSet<Pawn>()
            : map.mapPawns.AllPawnsSpawned.ToHashSet();
        List<Thing> ignoredThings = new List<Thing>();
        DoExplosion(corpse, ignoredThings);
        base.PawnDied(corpse, prevLord);

        if (map == null)
        {
            return;
        }

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (!existingPawns.Contains(pawn))
            {
                ignoredThings.Add(pawn);
            }
        }
    }

    private void DoExplosion(Corpse corpse, List<Thing> ignoredThings)
    {
        Map map = corpse.MapHeld;
        if (map == null)
        {
            return;
        }

        IntVec3 position = corpse.PositionHeld;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, Props.explosionRadius, true))
        {
            if (cell.InBounds(map)
                && cell.Walkable(map)
                && !FleshTerrainUtility.IsFleshTerrain(map, cell)
                && !cell.GetTerrain(map).IsRiver
                && !cell.GetTerrain(map).IsWater)
            {
                map.terrainGrid.SetTerrain(cell, TerrainDefOf.Flesh);
            }
        }

        GenExplosion.DoExplosion(
            position,
            map,
            Props.explosionRadius,
            DamageDefOf.Bomb,
            corpse.InnerPawn,
            Props.baseDamage,
            ignoredThings: ignoredThings);
    }
}
