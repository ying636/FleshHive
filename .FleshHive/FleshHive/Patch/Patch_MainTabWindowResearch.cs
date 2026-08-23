using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(MainTabWindow_Research), "UpdateSelectedProject", typeof(ResearchManager))]
public static class Patch_MainTabWindowResearch
{
    public static void Postfix(
        MainTabWindow_Research __instance,
        ResearchManager researchManager,
        ref ResearchProjectDef? ___selectedProject)
    {
        ResearchTabDef fleshHiveTab = FleshHiveDefOf.FH_ResearchTab_FleshHive;
        bool isFleshHiveTab = __instance.CurTab == fleshHiveTab;
        if (__instance.CurTab != null && !isFleshHiveTab)
        {
            return;
        }

        ResearchProjectDef? currentProject = researchManager.CurrentAnomalyKnowledgeProjects
            .Select(categoryProject => categoryProject.project)
            .FirstOrDefault(project => project?.tab == fleshHiveTab);
        if (currentProject != null)
        {
            ___selectedProject = currentProject;
        }
        else if (isFleshHiveTab)
        {
            ___selectedProject = null;
        }
    }
}
