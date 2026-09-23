using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class FleshHiveMod : Mod
{
    public FleshHiveMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FleshHiveSettings>();
    }

    public override string SettingsCategory()
    {
        return "FleshHive_SettingsCategory".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("FleshHive_RaidParasiteChance".Translate(Settings.raidParasiteChance.ToStringPercent()));
        Settings.raidParasiteChance = listing.Slider(Settings.raidParasiteChance, 0f, 1f);
        bool previousDisableTutorial = Settings.disableTutorial;
        bool previousUnlockUnmodifiedFleshbeastsByFusionResearch = Settings.unlockUnmodifiedFleshbeastsByFusionResearch;
        listing.CheckboxLabeled("FH_Tutorial_Disable".Translate(), ref Settings.disableTutorial);
        listing.CheckboxLabeled(
            "FH_Settings_ResearchUnlockGestation".Translate(),
            ref Settings.unlockUnmodifiedFleshbeastsByFusionResearch,
            "FH_Settings_ResearchUnlockGestationTip".Translate());
        if (previousDisableTutorial != Settings.disableTutorial
            || previousUnlockUnmodifiedFleshbeastsByFusionResearch != Settings.unlockUnmodifiedFleshbeastsByFusionResearch)
        {
            WriteSettings();
        }
        if (listing.ButtonText("FH_Tutorial_Open".Translate()))
        {
            Find.WindowStack.Add(new Window_FleshHiveTutorial());
        }
        listing.End();
        base.DoSettingsWindowContents(inRect);
    }

    public static FleshHiveSettings Settings = new FleshHiveSettings();
}

public class FleshHiveSettings : ModSettings
{
    public override void ExposeData()
    {
        Scribe_Values.Look(ref raidParasiteChance, "raidParasiteChance", 0.6f);
        Scribe_Values.Look(ref disableTutorial, "disableTutorial", false);
        Scribe_Values.Look(ref unlockUnmodifiedFleshbeastsByFusionResearch, "unlockUnmodifiedFleshbeastsByFusionResearch", false);
    }

    public float raidParasiteChance = 0.6f;
    public bool disableTutorial;
    public bool unlockUnmodifiedFleshbeastsByFusionResearch;
}
