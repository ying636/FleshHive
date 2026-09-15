using System.Reflection;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class Gizmo_Group_FleshHive : Gizmo_Group
{
    public Gizmo_Group_FleshHive(UnitGroup group) : base(group)
    {
    }

    private GUIStyle StatusLabelStyle => statusLabelStyle ??= new GUIStyle(Text.fontStyles[(int)GameFont.Small])
    {
        fontSize = 10,
        alignment = TextAnchor.UpperLeft,
        wordWrap = false,
        padding = new RectOffset(0, 0, 0, 0),
        contentOffset = Vector2.zero
    };

    public override float GetWidth(float maxWidth)
    {
        GameFont previousFont = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float textWidth = Text.CalcSize(group.name ?? string.Empty).x;
            string statusLabel = (group as UnitGroup_FleshHive)?.StatusLabel ?? string.Empty;
            if (!string.IsNullOrEmpty(statusLabel))
            {
                textWidth = Mathf.Max(textWidth, StatusLabelStyle.CalcSize(new GUIContent(statusLabel)).x);
            }
            return Mathf.Max(base.GetWidth(maxWidth),
                Mathf.Ceil(textWidth) + Padding * 2f + (IconButtonSize + IconGap) * 2f + 4f);
        }
        finally
        {
            Text.Font = previousFont;
        }
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        string statusLabel = (group as UnitGroup_FleshHive)?.StatusLabel ?? string.Empty;
        Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
        Rect inRect = rect.ContractedBy(Padding);
        Color previousColor = GUI.color;
        GUI.color = parms.lowLight
            ? this.group.gizmoBackgroundColor * Command.LowLightBgColor
            : this.group.gizmoBackgroundColor;
        GenUI.DrawTextureWithMaterial(rect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex,
            parms.lowLight ? TexUI.GrayscaleGUI : null);
        GUI.color = previousColor;

        Rect maintenanceRect = new Rect(inRect.xMax - IconButtonSize, inRect.y, IconButtonSize, IconButtonSize);
        Rect modeRect = new Rect(maintenanceRect.x - IconButtonSize - IconGap, inRect.y,
            IconButtonSize, IconButtonSize);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Rect nameRect = new Rect(inRect.x, rect.y, modeRect.x - inRect.x - IconGap, Text.LineHeight);
        Widgets.Label(nameRect, this.group.name);
        if (Mouse.IsOver(nameRect))
        {
            Widgets.DrawHighlight(nameRect);
            MouseoverSounds.DoRegion(nameRect, SoundDefOf.Mouseover_Command);
            if (Widgets.ButtonInvisible(nameRect))
            {
                CameraJumper.TryJump(this.group.Target);
                if (Event.current.shift || Event.current.control)
                {
                    GameComponent_UnitGroup.Instance.ToggleGroupSelection(this.group);
                }
                else
                {
                    GameComponent_UnitGroup.Instance.SelectGroup(this.group);
                }
            }
        }

        Widgets.DrawTextureFitted(modeRect, this.group.WorkModeDef.Icon, 1f);
        TooltipHandler.TipRegion(modeRect,
            this.group.WorkModeDef.LabelCap + "\n\n" + this.group.WorkModeDef.description);
        if (Mouse.IsOver(modeRect))
        {
            Widgets.DrawHighlight(modeRect);
            MouseoverSounds.DoRegion(modeRect, SoundDefOf.Mouseover_Command);
            if (Widgets.ButtonInvisible(modeRect))
            {
                Find.WindowStack.Add(new FloatMenu(GetModeOptions().ToList()));
            }
        }

        Widgets.DrawTextureFitted(maintenanceRect, maintenanceSettingsIcon.Texture, 1f);
        TooltipHandler.TipRegion(maintenanceRect, "HCF_GroupMaintenanceSettingsTip".Translate());
        if (Mouse.IsOver(maintenanceRect))
        {
            Widgets.DrawHighlight(maintenanceRect);
            MouseoverSounds.DoRegion(maintenanceRect, SoundDefOf.Mouseover_Command);
            if (Widgets.ButtonInvisible(maintenanceRect))
            {
                Find.WindowStack.Add(new Window_GroupMaintenanceSettings(this.group));
            }
        }

        Rect unitsRect = new Rect(inRect.x, inRect.y + IconButtonSize + IconGap, inRect.width,
            inRect.height - IconButtonSize - IconGap);
        if (!string.IsNullOrEmpty(statusLabel))
        {
            GUIContent statusContent = new GUIContent(statusLabel);
            float statusHeight = Mathf.Max(18f, Mathf.Ceil(StatusLabelStyle.CalcSize(statusContent).y) + 4f);
            Rect statusRect = new Rect(nameRect.x, nameRect.yMax - 2f, nameRect.width, statusHeight);
            GUI.Label(statusRect, statusContent, StatusLabelStyle);
            TooltipHandler.TipRegion(statusRect, statusLabel);
            unitsRect.yMin = Mathf.Max(unitsRect.yMin, statusRect.yMax + 2f);
        }
        drawUnits(unitsRect, this.group.GetDisplayUnits());

        return new GizmoResult(Mouse.IsOver(inRect) ? GizmoState.Mouseover : GizmoState.Clear);
    }

    private IEnumerable<FloatMenuOption> GetModeOptions()
    {
        foreach (GroupWorkModeDef modeDef in this.group.GetModeDefs())
        {
            GroupWorkModeDef selectedMode = modeDef;
            FloatMenuOption option = new FloatMenuOption(
                selectedMode.LabelCap,
                () => this.group.SetMode(selectedMode),
                selectedMode.Icon,
                Color.white);
            option.tooltip = selectedMode.description;
            yield return option;
        }
    }

    private const float GizmoHeight = 75f;
    private const float Padding = 6f;
    private const float IconButtonSize = 26f;
    private const float IconGap = 4f;

    private static readonly CachedTexture maintenanceSettingsIcon =
        new CachedTexture("UI/Group/WorkMode/MechRechargeSettings");

    private static readonly Action<Rect, List<Pawn>> drawUnits =
        (Action<Rect, List<Pawn>>)Delegate.CreateDelegate(typeof(Action<Rect, List<Pawn>>),
            typeof(Gizmo_Group).GetMethod("DrawUnits", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Rect), typeof(List<Pawn>) }, null)
            ?? throw new MissingMethodException(typeof(Gizmo_Group).FullName, "DrawUnits"));

    private GUIStyle? statusLabelStyle;
}
