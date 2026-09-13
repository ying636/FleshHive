using Verse;

namespace FleshHive;

public class GameComponent_FleshHiveTutorial : GameComponent
{
    public GameComponent_FleshHiveTutorial(Game game)
    {
    }

    public override void StartedNewGame()
    {
        pending = !FleshHiveMod.Settings.disableTutorial;
    }

    public override void LoadedGame()
    {
        pending = !FleshHiveMod.Settings.disableTutorial;
    }

    public override void GameComponentUpdate()
    {
        if (!pending || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || LongEventHandler.AnyEventNowOrWaiting)
        {
            return;
        }

        pending = false;
        if (!FleshHiveMod.Settings.disableTutorial && !Find.WindowStack.IsOpen<Window_FleshHiveTutorial>())
        {
            Find.WindowStack.Add(new Window_FleshHiveTutorial());
        }
    }

    private bool pending;
}
