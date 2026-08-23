using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(MainTabWindow_Research), "DrawProjectInfo", typeof(Rect))]
public static class Patch_MainTabWindowResearchProgress
{
    public static void Prefix(
        MainTabWindow_Research __instance,
        ResearchProjectDef? ___selectedProject,
        out ResearchProjectDrawState __state)
    {
        __state = default;
        if (__instance.CurTab != FleshHiveDefOf.FH_ResearchTab_FleshHive)
        {
            return;
        }

        ResearchManager researchManager = Find.ResearchManager;
        ResearchProjectDef? currentProject = researchManager.CurrentAnomalyKnowledgeProjects
            .Select(categoryProject => categoryProject.project)
            .FirstOrDefault(project => project == ___selectedProject
                && project?.tab == FleshHiveDefOf.FH_ResearchTab_FleshHive)
            ?? researchManager.CurrentAnomalyKnowledgeProjects
                .Select(categoryProject => categoryProject.project)
                .FirstOrDefault(project => project?.tab == FleshHiveDefOf.FH_ResearchTab_FleshHive);
        if (currentProject == null)
        {
            return;
        }

        __state = new ResearchProjectDrawState(true, CurrentProjectField(researchManager));
        CurrentProjectField(researchManager) = currentProject;
    }

    public static Exception? Finalizer(Exception? __exception, ResearchProjectDrawState __state)
    {
        if (__state.applied)
        {
            CurrentProjectField(Find.ResearchManager) = __state.originalProject;
        }

        return __exception;
    }

    public readonly struct ResearchProjectDrawState
    {
        public ResearchProjectDrawState(bool applied, ResearchProjectDef? originalProject)
        {
            this.applied = applied;
            this.originalProject = originalProject;
        }

        public readonly bool applied;
        public readonly ResearchProjectDef? originalProject;
    }

    private static readonly AccessTools.FieldRef<ResearchManager, ResearchProjectDef?> CurrentProjectField =
        AccessTools.FieldRefAccess<ResearchManager, ResearchProjectDef?>("currentProj");
}
