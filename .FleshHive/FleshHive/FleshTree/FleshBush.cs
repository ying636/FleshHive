using RimWorld;
using Verse;

namespace FleshHive;

public class FleshBush : Plant
{
    public override void TickLong()
    {
        base.TickLong();
        TryDesignateHarvest();
    }

    public override float GrowthRate
    {
        get
        {
            if (!FleshTerrainUtility.IsFleshTerrain(Map, Position))
            {
                return 0f;
            }
            return base.GrowthRate;
        }
    }

    public override float CurrentDyingDamagePerTick
    {
        get
        {
            if (!Spawned || FleshTerrainUtility.IsFleshTerrain(Map, Position))
            {
                return base.CurrentDyingDamagePerTick;
            }
            return 0.005f;
        }
    }

    private void TryDesignateHarvest()
    {
        if (!Spawned
            || !HarvestableNow
            || Map.designationManager.DesignationOn(this, DesignationDefOf.HarvestPlant) != null)
        {
            return;
        }

        Map.designationManager.AddDesignation(new Designation(this, DesignationDefOf.HarvestPlant));
    }
}
