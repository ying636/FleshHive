using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class PsychicRitualToil_SummonGiantFleshbeast : PsychicRitualToil
{
    public PsychicRitualToil_SummonGiantFleshbeast()
    {
    }

    public PsychicRitualToil_SummonGiantFleshbeast(PsychicRitualRoleDef invokerRole)
    {
        this.invokerRole = invokerRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        if (psychicRitual.assignments.FirstAssignedPawn(invokerRole) == null)
        {
            return;
        }

        PsychicRitualDef_SummonGiantFleshbeast ritualDef = (PsychicRitualDef_SummonGiantFleshbeast)psychicRitual.def;
        Slate slate = new Slate();
        slate.Set("motherKind", ritualDef.summonKind);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(psychicRitual.Map));
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(
            FleshHiveDefOf.FH_Quest_DistressCall_MotherBeast,
            slate);
        if (quest == null)
        {
            Log.Error($"[FleshHive] 母兽召唤仪式未能生成求救信号任务：母兽={ritualDef.summonKind?.defName ?? "null"}。");
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
    }

    private PsychicRitualRoleDef invokerRole = null!;
}
