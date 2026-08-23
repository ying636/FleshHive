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
    }

    public float raidParasiteChance = 0.6f;
}
