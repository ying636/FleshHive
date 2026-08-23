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
        pod.Draw(inRect);
    }


    public FleshParasitePod pod;
}