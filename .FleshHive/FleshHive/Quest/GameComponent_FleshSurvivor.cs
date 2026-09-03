using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace FleshHive;

public class GameComponent_FleshSurvivor : GameComponent
{
    public GameComponent_FleshSurvivor(Game game)
    {
    }

    public void DisableQuest()
    {
        questOffered = true;
    }

    public void DebugLogQuestOffered()
    {
        Log.Message("[FleshHive] Flesh survivor questOffered: " + questOffered);
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        ThoughtDef legacyThought = FleshHiveDefOf.FH_Thought_FleshParasitism;
        foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
        {
            List<Thought_Memory> memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
            if (memories == null)
            {
                continue;
            }

            for (int i = memories.Count - 1; i >= 0; i--)
            {
                if (memories[i].def == legacyThought)
                {
                    memories.RemoveAt(i);
                }
            }
        }
    }

    public override void GameComponentTick()
    {
        if (questOffered || Find.TickManager.TicksGame < TriggerTick || Find.TickManager.TicksGame % CheckInterval != 0)
        {
            return;
        }

        Map map = Find.AnyPlayerHomeMap;
        if (map == null || Faction.OfEntities == null)
        {
            return;
        }

        Slate slate = new Slate();
        slate.Set("map", map);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(FleshHiveDefOf.FH_Quest_FleshSurvivor, slate);
        if (quest == null)
        {
            return;
        }

        questOffered = true;
        QuestUtility.SendLetterQuestAvailable(quest);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref questOffered, "fleshSurvivorQuestOffered", false);
    }

    private bool questOffered;

    private const int TriggerTick = 600000;
    private const int CheckInterval = 250;
}
