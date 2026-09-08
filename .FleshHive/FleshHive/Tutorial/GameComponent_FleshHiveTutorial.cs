using Verse;

namespace FleshHive;

public class GameComponent_FleshHiveTutorial : GameComponent
{
    public GameComponent_FleshHiveTutorial(Game game)
    {
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref tutorialShown, "fleshHiveTutorialShown", false);
    }

    public override void StartedNewGame()
    {
        pending = !tutorialShown;
    }

    public override void LoadedGame()
    {
        pending = !tutorialShown;
    }

    public override void GameComponentUpdate()
    {
        if (!pending || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || LongEventHandler.AnyEventNowOrWaiting)
        {
            return;
        }

        Find.WindowStack.Add(new Window_FleshHiveTutorial());
        tutorialShown = true;
        pending = false;
    }

    private bool tutorialShown;
    private bool pending;
}
