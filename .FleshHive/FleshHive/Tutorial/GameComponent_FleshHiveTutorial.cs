using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class GameComponent_FleshHiveTutorial : GameComponent
{
    public GameComponent_FleshHiveTutorial(Game game)
    {
    }

    public override void StartedNewGame()
    {
        tutorialPending = !FleshHiveMod.Settings.disableTutorial;
        fusionRecipeRemodelingVersion = CurrentFusionRecipeRemodelingVersion;
    }

    public override void LoadedGame()
    {
        tutorialPending = !FleshHiveMod.Settings.disableTutorial;
        if (fusionRecipeRemodelingVersion < CurrentFusionRecipeRemodelingVersion)
        {
            if (MigrateDiscoveredLargeFusionRecipes())
            {
                fusionRecipeRemodelingVersion = CurrentFusionRecipeRemodelingVersion;
                fusionRecipeRemodelingLetterPending = true;
            }
        }
    }

    public override void GameComponentUpdate()
    {
        if (Current.ProgramState != ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)
        {
            return;
        }

        if (fusionRecipeRemodelingLetterPending)
        {
            fusionRecipeRemodelingLetterPending = false;
            Find.LetterStack.ReceiveLetter(
                "FH_FusionRemodelingLetterLabel".Translate(),
                "FH_FusionRemodelingLetterText".Translate(),
                LetterDefOf.NeutralEvent);
        }

        if (!tutorialPending || Find.CurrentMap == null)
        {
            return;
        }

        tutorialPending = false;
        if (!FleshHiveMod.Settings.disableTutorial && !Find.WindowStack.IsOpen<Window_FleshHiveTutorial>())
        {
            Find.WindowStack.Add(new Window_FleshHiveTutorial());
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref fusionRecipeRemodelingVersion, "fusionRecipeRemodelingVersion", 0);
    }

    private bool MigrateDiscoveredLargeFusionRecipes()
    {
        GameComponent_UnitGroup unitGroup = GameComponent_UnitGroup.Instance;
        if (unitGroup?.fusionDatas == null)
        {
            Log.Error("[FleshHive] Could not migrate discovered large fusion recipes because the HCF unit group component is unavailable.");
            return false;
        }

        List<FusionDef> largeFusions = DefDatabase<FusionDef>.AllDefsListForReading
            .Where(def => def.category == LargeFusionCategory)
            .ToList();
        if (largeFusions.Count != ExpectedLargeFusionCount)
        {
            Log.Error($"[FleshHive] Expected {ExpectedLargeFusionCount} large fusion recipes during save migration, but found {largeFusions.Count}.");
            return false;
        }

        List<FusionRecipe> discoveredRecipes = unitGroup.fusionDatas
            .Where(data => data?.def?.isDefault == true)
            .SelectMany(data => data.Recipes)
            .Where(recipe => !recipe.materials.NullOrEmpty())
            .ToList();

        foreach (FusionRecipe recipe in discoveredRecipes)
        {
            FusionDef fusion = largeFusions.FirstOrDefault(candidate => MaterialsMatch(candidate, recipe.materials));
            if (fusion == null)
            {
                continue;
            }

            FusionDefData data = unitGroup.fusionDatas.FirstOrDefault(existing => existing?.def == fusion);
            if (data == null)
            {
                unitGroup.fusionDatas.Add(new FusionDefData
                {
                    def = fusion,
                    unlocked = true
                });
            }
            else
            {
                data.unlocked = true;
            }
        }

        return true;
    }

    private bool MaterialsMatch(FusionDef fusion, IReadOnlyList<ThingDef> discoveredMaterials)
    {
        if (fusion.materials.Count != discoveredMaterials.Count)
        {
            return false;
        }

        List<ThingDef> remaining = discoveredMaterials.Where(def => def != null).ToList();
        if (remaining.Count != discoveredMaterials.Count)
        {
            return false;
        }

        foreach (FusionMaterial material in fusion.materials)
        {
            if (material is not FusionMaterial_Def fixedMaterial || fixedMaterial.def == null)
            {
                return false;
            }

            int index = remaining.IndexOf(fixedMaterial.def);
            if (index < 0)
            {
                return false;
            }

            remaining.RemoveAt(index);
        }

        return remaining.Count == 0;
    }

    private bool tutorialPending;
    private bool fusionRecipeRemodelingLetterPending;
    private int fusionRecipeRemodelingVersion;

    private const int CurrentFusionRecipeRemodelingVersion = 1;
    private const int ExpectedLargeFusionCount = 75;
    private const string LargeFusionCategory = "LargeFlesh";
}
