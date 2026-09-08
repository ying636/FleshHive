using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Window_FleshHiveTutorial : Window
{
    public Window_FleshHiveTutorial()
    {
        doWindowBackground = false;
        drawShadow = false;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        for (int i = 0; i < ImagePaths.Length; i++)
        {
            images[i] = ContentFinder<Texture2D>.Get(ImagePaths[i]);
        }
    }

    public override Vector2 InitialSize
    {
        get
        {
            Texture2D? texture = images[pageIndex];
            Vector2 size = texture != null ? new Vector2(texture.width, texture.height) : new Vector2(600f, 400f);
            float scale = Mathf.Min(1f, (UI.screenWidth - 40f) / size.x, (UI.screenHeight - 40f) / size.y);
            return size * scale;
        }
    }

    protected override float Margin => 0f;

    public override void DoWindowContents(Rect inRect)
    {
        Texture2D? texture = images[pageIndex];
        if (texture != null)
        {
            GUI.DrawTexture(inRect, texture, ScaleMode.ScaleToFit);
        }
        else
        {
            Widgets.Label(inRect, ImagePaths[pageIndex]);
        }

        const float buttonSize = 40f;
        const float inset = 8f;
        Rect previousRect = new Rect(inRect.x + inset, inRect.yMax - inset - buttonSize, buttonSize, buttonSize);
        Rect nextRect = new Rect(inRect.xMax - inset - buttonSize, previousRect.y, buttonSize, buttonSize);
        Rect closeRect = new Rect(nextRect.x, inRect.y + inset, buttonSize, buttonSize);
        if (DrawArrow(previousRect, Window_Hive.LefttArrow, pageIndex > 0, "FH_Tutorial_Previous".Translate()))
        {
            pageIndex--;
            SetInitialSizeAndPosition();
            return;
        }

        if (DrawArrow(nextRect, Window_Hive.RightArrow, pageIndex < images.Length - 1, "FH_Tutorial_Next".Translate()))
        {
            pageIndex++;
            SetInitialSizeAndPosition();
            return;
        }

        Widgets.DrawBoxSolid(closeRect, new Color(0f, 0f, 0f, 0.65f));
        if (Widgets.ButtonImage(closeRect.ContractedBy(8f), TexButton.CloseXSmall, tooltip: "CloseButton".Translate()))
        {
            Close();
        }
    }

    private bool DrawArrow(Rect rect, Texture2D arrow, bool enabled, string tooltip)
    {
        Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.65f));
        Color previousColor = GUI.color;
        GUI.color = !enabled ? Color.gray : Mouse.IsOver(rect) ? GenUI.MouseoverColor : Color.white;
        GUI.DrawTexture(rect.ContractedBy(6f), arrow, ScaleMode.ScaleToFit);
        GUI.color = previousColor;
        TooltipHandler.TipRegion(rect, tooltip);
        return enabled && Widgets.ButtonInvisible(rect);
    }

    private static readonly string[] ImagePaths =
    {
        "UI/Tutorial/info_c2",
        "UI/Tutorial/info_c3",
        "UI/Tutorial/info_c24",
        "UI/Tutorial/info_c26",
        "UI/Tutorial/info_c25",
        "UI/Tutorial/info_c27",
        "UI/Tutorial/info_c29",
        "UI/Tutorial/info_c30",
        "UI/Tutorial/info_c31"
    };

    private readonly Texture2D?[] images = new Texture2D?[ImagePaths.Length];
    private int pageIndex;
}
