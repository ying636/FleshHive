using System.Reflection;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class ITab_FleshHiveGroup : ITab
{
    public ITab_FleshHiveGroup()
    {
        labelKey = "FH_ITab_HiveGroup";
    }

    public override bool IsVisible => SelThing?.Faction == Faction.OfPlayer
        && (SelThing.def == FleshHiveDefOf.FH_FleshHopper
            || SelThing.def == FleshHiveDefOf.FH_FleshPrimaryNest
            || SelThing.def.defName == FleshHiveDefName);

    public override void OnOpen()
    {
        base.OnOpen();

        bool opened = SelThing switch
        {
            Building_FleshHopper hopper => OpenItemProduction(hopper),
            ThingWithComps hive => OpenFleshbeastGestation(hive),
            _ => false
        };

        if (!opened)
        {
            NotifyOpenFailed();
        }

        CloseTab();
    }

    protected override void FillTab()
    {
        CloseTab();
    }

    private bool OpenFleshbeastGestation(ThingWithComps hive)
    {
        HiveRaceCategoryDef category = DefDatabase<HiveRaceCategoryDef>.GetNamedSilentFail(FleshHiveCategoryDefName);
        HiveTabOptionDef optionDef = DefDatabase<HiveTabOptionDef>.GetNamedSilentFail(FleshbeastGestationOptionDefName);
        CompHiveSpawner_FleshTrait spawner = hive.TryGetComp<CompHiveSpawner_FleshTrait>();
        if (category == null || optionDef?.option is not HiveTabOption_FleshbeastGestation gestation
            || spawner == null)
        {
            return false;
        }

        gestation.SelectSpawner(spawner);
        return OpenHiveGroupOption(category, optionDef);
    }

    private bool OpenItemProduction(Building_FleshHopper hopper)
    {
        HiveRaceCategoryDef category = DefDatabase<HiveRaceCategoryDef>.GetNamedSilentFail(FleshHiveCategoryDefName);
        HiveTabOptionDef optionDef = DefDatabase<HiveTabOptionDef>.GetNamedSilentFail(ItemProductionOptionDefName);
        if (category == null || optionDef?.option is not HiveTabOption_ItemProduction itemProduction)
        {
            return false;
        }

        itemProduction.SelectProducer(hopper);
        return OpenHiveGroupOption(category, optionDef);
    }

    private bool OpenHiveGroupOption(HiveRaceCategoryDef category, HiveTabOptionDef optionDef)
    {
        Find.MainTabsRoot.SetCurrentTab(HCFDefOf.HCF_MainButton_HiveGroup);

        MainTabWindow_HiveGroup mainWindow = Find.WindowStack.WindowOfType<MainTabWindow_HiveGroup>();
        FieldInfo curOptionField = typeof(MainTabWindow_HiveGroup).GetField("curOption",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (mainWindow == null || curOptionField == null)
        {
            return false;
        }

        mainWindow.SelectCategory(category);
        curOptionField.SetValue(mainWindow, optionDef);
        return true;
    }

    private void NotifyOpenFailed()
    {
        TaggedString message = "FH_HiveGroup_OpenPageFailed".Translate();
        Messages.Message(message, MessageTypeDefOf.RejectInput, false);
        Log.Error($"[FleshHive] {message}");
    }

    private const string FleshHiveDefName = "FH_FleshHive";
    private const string FleshHiveCategoryDefName = "FH_Category_FleshHive";
    private const string FleshbeastGestationOptionDefName = "FH_CategoryOption_FleshbeastGestation";
    private const string ItemProductionOptionDefName = "FH_CategoryOption_ItemProduction";
}
