using UnityEngine;
using Verse;

namespace FleshHive;

public class Window_Parasitism : Window
{
    public override Vector2 InitialSize => new Vector2(800, 600);
    public Window_Parasitism(FleshParasitePod pod)
    {
        this.doCloseX = true;
        this.pod = pod;
    }

    public override void DoWindowContents(Rect inRect)
    {
        GameFont font = Text.Font;
        Text.Font = GameFont.Small;
        try
        {
            pod.Draw(inRect);
        }
        finally
        {
            Text.Font = font;
        }
    }


    public FleshParasitePod pod;
}
