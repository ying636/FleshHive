using System.Reflection;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ITab_FleshHive : ITab
{
    public ITab_FleshHive()
    {
        labelKey = "ITab_HCF_HiveSystem";
    }

    public override bool IsVisible => SelThing.Faction == Faction.OfPlayer;

    public override void OnOpen()
    {
        base.OnOpen();
        if (Find.WindowStack.Windows.ToList().Find(window => window is Window_Hive) is Window_Hive hiveWindow)
        {
            Find.WindowStack.TryRemove(hiveWindow);
        }

        if (Find.WindowStack.Windows.ToList().Find(window => window is Window_FleshHive) is Window_FleshHive fleshHiveWindow)
        {
            Find.WindowStack.TryRemove(fleshHiveWindow);
        }

        if (SelThing is Building_FleshHopper hopper)
        {
            OpenItemProduction(hopper);
            CloseTab();
            return;
        }

        Find.WindowStack.Add(new Window_FleshHive(SelThing));
        CloseTab();
    }

    protected override void FillTab()
    {
        CloseTab();
    }

    private void OpenItemProduction(Building_FleshHopper hopper)
    {
        HiveRaceCategoryDef category = DefDatabase<HiveRaceCategoryDef>.GetNamedSilentFail("FH_Category_FleshHive");
        HiveTabOptionDef optionDef = DefDatabase<HiveTabOptionDef>.GetNamedSilentFail("FH_CategoryOption_ItemProduction");
        if (category == null || optionDef?.option is not HiveTabOption_ItemProduction itemProduction)
        {
            NotifyItemProductionOpenFailed();
            return;
        }

        itemProduction.SelectProducer(hopper);
        Find.MainTabsRoot.SetCurrentTab(HCFDefOf.HCF_MainButton_HiveGroup);

        MainTabWindow_HiveGroup mainWindow = Find.WindowStack.WindowOfType<MainTabWindow_HiveGroup>();
        FieldInfo curOptionField = typeof(MainTabWindow_HiveGroup).GetField("curOption",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (mainWindow == null || curOptionField == null)
        {
            NotifyItemProductionOpenFailed();
            return;
        }

        mainWindow.SelectCategory(category);
        curOptionField.SetValue(mainWindow, optionDef);
    }

    private void NotifyItemProductionOpenFailed()
    {
        TaggedString message = "FH_ItemProduction_OpenPageFailed".Translate();
        Messages.Message(message, MessageTypeDefOf.RejectInput, false);
        Log.Error($"[FleshHive] {message}");
    }
}

public class Window_FleshHive : Window_Hive
{
    public Window_FleshHive(Thing hive) : base(hive)
    {
    }

    public override Vector2 InitialSize
    {
        get
        {
            Vector2 initialSize = base.InitialSize;
            initialSize.x *= 1.5f;
            return initialSize;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (!FleshHiveHungerUtility.IsHungry(hive))
        {
            base.DoWindowContents(inRect);
            return;
        }

        DrawHungryContents(inRect, hive.Label);
    }

    public static void DrawHungryContents(Rect inRect, string? title = null)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;

        float titleHeight = 0f;
        if (!title.NullOrEmpty())
        {
            Text.Font = GameFont.Medium;
            Vector2 titleSize = Text.CalcSize(title);
            Widgets.Label(new Rect((inRect.width - titleSize.x) / 2f, inRect.y, titleSize.x, titleSize.y), title);
            titleHeight = titleSize.y;
        }

        Rect hungryRect = new Rect(inRect.x + 18f, inRect.y + titleHeight + 18f,
            inRect.width - 36f, inRect.height - titleHeight - 36f);
        Widgets.DrawBoxSolid(hungryRect, HungryBackground);

        GUI.color = HungryBorder;
        Widgets.DrawBox(hungryRect, 2);

        DrawHungryTexts(hungryRect);

        GUI.color = oldColor;
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
    }

    private static void DrawHungryTexts(Rect rect)
    {
        for (int i = 0; i < HungryTextLayouts.Length; i++)
        {
            HungryTextLayout layout = HungryTextLayouts[i];
            float alpha = GetHungryTextAlpha(layout);
            if (alpha <= 0.01f)
            {
                continue;
            }

            Text.Font = layout.font;
            Text.Anchor = TextAnchor.MiddleCenter;
            Color color = layout.color;
            color.a *= alpha;
            GUI.color = color;
            Vector2 center = new Vector2(rect.x + rect.width * layout.x, rect.y + rect.height * layout.y);
            Widgets.Label(new Rect(center.x - layout.width / 2f, center.y - 18f, layout.width, 36f), layout.key.Translate());
        }
    }

    private static float GetHungryTextAlpha(HungryTextLayout layout)
    {
        if (layout.alwaysVisible)
        {
            return 0.65f + Mathf.Sin((Time.realtimeSinceStartup + layout.offsetSeconds) * 2.2f) * 0.35f;
        }

        float phase = Mathf.Repeat(Time.realtimeSinceStartup + layout.offsetSeconds, layout.durationSeconds) / layout.durationSeconds;
        if (phase < 0.2f)
        {
            return Mathf.InverseLerp(0f, 0.2f, phase);
        }

        if (phase < 0.48f)
        {
            return 1f;
        }

        if (phase < 0.82f)
        {
            return 1f - Mathf.InverseLerp(0.48f, 0.82f, phase);
        }

        return 0f;
    }

    private static readonly Color HungryBackground = new(0.18f, 0f, 0f, 0.86f);
    private static readonly Color HungryBorder = new(0.85f, 0f, 0f, 1f);
    private static readonly Color HungryTextStrong = new(1f, 0.05f, 0.03f, 1f);
    private static readonly Color HungryTextMid = new(1f, 0.05f, 0.03f, 0.72f);
    private static readonly Color HungryTextFaint = new(1f, 0.05f, 0.03f, 0.42f);

    private static readonly HungryTextLayout[] HungryTextLayouts =
    [
        new("FH_HiveHungryBanner", 0.52f, 0.50f, 220f, GameFont.Medium, HungryTextStrong, 5.5f, 0.0f, true),
        new("FH_HiveHungryWhisper1", 0.50f, 0.46f, 360f, GameFont.Small, HungryTextMid, 5.8f, 0.2f),
        new("FH_HiveHungryWhisper2", 0.54f, 0.54f, 320f, GameFont.Small, HungryTextMid, 6.4f, 1.4f),
        new("FH_HiveHungryWhisper3", 0.47f, 0.51f, 320f, GameFont.Small, HungryTextFaint, 5.2f, 2.6f),
        new("FH_HiveHungryWhisper4", 0.57f, 0.48f, 300f, GameFont.Small, HungryTextFaint, 6.8f, 3.5f),
        new("FH_HiveHungryWhisper1", 0.23f, 0.22f, 360f, GameFont.Small, HungryTextFaint, 7.1f, 4.2f),
        new("FH_HiveHungryWhisper2", 0.77f, 0.24f, 320f, GameFont.Small, HungryTextFaint, 6.0f, 5.0f),
        new("FH_HiveHungryWhisper3", 0.18f, 0.76f, 300f, GameFont.Small, HungryTextFaint, 7.6f, 0.9f),
        new("FH_HiveHungryWhisper4", 0.82f, 0.72f, 300f, GameFont.Small, HungryTextFaint, 5.7f, 2.0f),
        new("FH_HiveHungryWhisper3", 0.50f, 0.32f, 320f, GameFont.Small, HungryTextFaint, 6.6f, 3.0f),
        new("FH_HiveHungryWhisper1", 0.42f, 0.63f, 360f, GameFont.Small, HungryTextFaint, 7.3f, 4.7f),
        new("FH_HiveHungryWhisper2", 0.60f, 0.64f, 320f, GameFont.Small, HungryTextFaint, 6.2f, 5.8f)
    ];

    private readonly struct HungryTextLayout
    {
        public HungryTextLayout(string key, float x, float y, float width, GameFont font, Color color, float durationSeconds, float offsetSeconds, bool alwaysVisible = false)
        {
            this.key = key;
            this.x = x;
            this.y = y;
            this.width = width;
            this.font = font;
            this.color = color;
            this.durationSeconds = durationSeconds;
            this.offsetSeconds = offsetSeconds;
            this.alwaysVisible = alwaysVisible;
        }

        public readonly string key;
        public readonly float x;
        public readonly float y;
        public readonly float width;
        public readonly GameFont font;
        public readonly Color color;
        public readonly float durationSeconds;
        public readonly float offsetSeconds;
        public readonly bool alwaysVisible;
    }
}
