using System.Collections.Generic;
using Verse;

namespace FleshHive;

public class MapFleshHive : IExposable
{
    public HashSet<Blueprint_FleshBuild> CachedFleshBlueprints
    {
        get
        {
            cachedFleshBlueprints ??= new HashSet<Blueprint_FleshBuild>();
            return cachedFleshBlueprints;
        }
    }

    public HashSet<Building_FleshHopper> CachedFleshHoppers
    {
        get
        {
            cachedFleshHoppers ??= new HashSet<Building_FleshHopper>();
            return cachedFleshHoppers;
        }
    }

    public HashSet<Building_FleshBox> CachedFleshBoxes
    {
        get
        {
            cachedFleshBoxes ??= new HashSet<Building_FleshBox>();
            return cachedFleshBoxes;
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref fleshTerrainCount, "fleshTerrainCount", 0);
        Scribe_Values.Look(ref hiveScale, "hiveScale", 0);
        Scribe_Values.Look(ref nutrition, "nutrition", 100f);
        Scribe_Values.Look(ref nutritionAllowedToFill, "nutritionAllowedToFill", true);
        Scribe_Values.Look(ref nutritionTargetValue, "nutritionTargetValue", 1f);
        Scribe_Values.Look(ref activity, "activity", 0f);
        Scribe_Values.Look(ref fullActivityTicks, "fullActivityTicks", 0);
        Scribe_Values.Look(ref autoSuppressActivity, "autoSuppressActivity");
        Scribe_Values.Look(ref autoSuppressActivityThreshold, "autoSuppressActivityThreshold", 0.5f);
        Scribe_Values.Look(ref autoRepairFleshBuildings, "autoRepairFleshBuildings", false);
        Scribe_Collections.Look(ref completedUpgrades, "completedUpgrades", LookMode.Def);
        Scribe_Defs.Look(ref activeUpgrade, "activeUpgrade");
        Scribe_Values.Look(ref activeUpgradeProgress, "activeUpgradeProgress", 0f);
        Scribe_Values.Look(ref activeUpgradeTotalTime, "activeUpgradeTotalTime", 0f);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            completedUpgrades ??= new HashSet<FleshHiveUpgradeDef>();
            completedUpgrades.RemoveWhere(upgrade => upgrade == null);
            if (activeUpgrade == null)
            {
                activeUpgradeProgress = 0f;
                activeUpgradeTotalTime = 0f;
            }
        }
    }

    public int fleshTerrainCount;
    public int hiveScale;
    public float nutrition = 100f;
    public bool nutritionAllowedToFill = true;
    public float nutritionTargetValue = 1f;
    public float activity;
    public int fullActivityTicks;
    public bool autoSuppressActivity;
    public float autoSuppressActivityThreshold = 0.5f;
    public bool autoRepairFleshBuildings;

    public HashSet<FleshHiveUpgradeDef> completedUpgrades = new HashSet<FleshHiveUpgradeDef>();
    public FleshHiveUpgradeDef activeUpgrade;
    public float activeUpgradeProgress;
    public float activeUpgradeTotalTime;

    private HashSet<Blueprint_FleshBuild> cachedFleshBlueprints;
    private HashSet<Building_FleshHopper> cachedFleshHoppers;
    private HashSet<Building_FleshBox> cachedFleshBoxes;
}
