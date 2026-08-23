using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class ITab_ParasiticWeapons : ITab
{
    private static readonly Color ActiveColor = new(0.55f, 1f, 0.55f, 1f);
    private static readonly Color InactiveColor = new(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color DisabledColor = new(0.45f, 0.45f, 0.45f, 0.9f);
    private const float ButtonSize = 28f;
    private const float ButtonGap = 4f;
    private const float RowHeight = 42f;
    private Vector2 scrollPosition;
    private float scrollViewHeight;

    public ITab_ParasiticWeapons()
    {
        size = new Vector2(620f, 220f);
        labelKey = "FH_TabParasiticWeapons";
    }

    public override bool IsVisible => WeaponMountComps.Any();

    protected override void FillTab()
    {
        Text.Font = GameFont.Small;
        Rect rect = new Rect(10f, 10f, size.x - 20f, size.y - 20f);
        List<HediffComp_ParasitismWeaponMounts> comps = WeaponMountComps.ToList();
        if (comps.NullOrEmpty())
        {
            return;
        }

        Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
        Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
        float curY = 0f;
        foreach (HediffComp_ParasitismWeaponMounts comp in comps)
        {
            foreach (Tentacle_WeaponMount mount in comp.WeaponMounts)
            {
                DrawWeaponMountRow(mount, 0f, ref curY, viewRect.width);
            }
        }
        if (Event.current.type == EventType.Layout)
        {
            scrollViewHeight = curY + 10f;
        }
        Widgets.EndScrollView();
    }

    private Pawn SelPawnForParasiticWeapons => SelThing as Pawn;

    private IEnumerable<HediffComp_ParasitismWeaponMounts> WeaponMountComps => HediffComp_ParasitismWeaponMounts.GetAll(SelPawnForParasiticWeapons);

    private void DrawWeaponMountRow(Tentacle_WeaponMount mount, float x, ref float y, float width)
    {
        Rect rowRect = new Rect(x, y, width, RowHeight);
        if (Mouse.IsOver(rowRect))
        {
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(rowRect, TexUI.HighlightTex);
            GUI.color = Color.white;
        }

        Rect slotRect = new Rect(rowRect.x, rowRect.y + 2f, 170f, 24f);
        Widgets.Label(slotRect, mount.SlotLabel);
        ThingWithComps weapon = mount.MountedWeapon;
        float buttonX = rowRect.xMax;
        if (weapon == null)
        {
            GUI.color = DisabledColor;
            Widgets.Label(new Rect(rowRect.x + 175f, rowRect.y + 2f, rowRect.width - 175f, 24f), "FH_ParasiticWeaponSlotEmpty".Translate());
            GUI.color = Color.white;
            y += RowHeight;
            return;
        }

        Rect weaponIconRect = new Rect(rowRect.x + 175f, rowRect.y + 2f, ButtonSize, ButtonSize);
        Widgets.ThingIcon(weaponIconRect, weapon);

        Rect weaponLabelRect = new Rect(weaponIconRect.xMax + 6f, rowRect.y + 2f, rowRect.width - 330f, 24f);
        GUI.color = ActiveColor;
        Widgets.Label(weaponLabelRect, weapon.LabelCap.Truncate(weaponLabelRect.width));
        GUI.color = Color.white;

        Rect statusRect = new Rect(weaponIconRect.xMax + 6f, rowRect.y + 22f, rowRect.width - 330f, 18f);
        GUI.color = InactiveColor;
        Text.Font = GameFont.Tiny;
        string statusText = weapon.def.Verbs.NullOrEmpty()
            ? "DisabledLower".Translate().CapitalizeFirst().ToString()
            : weapon.def.Verbs[0].label.CapitalizeFirst();
        Widgets.Label(statusRect, statusText);
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        buttonX -= ButtonSize;
        Rect dropRect = new Rect(buttonX, rowRect.y + 4f, ButtonSize, ButtonSize);
        TooltipHandler.TipRegion(dropRect, "FH_UnmountParasiticWeaponDesc".Translate(mount.SlotLabel, weapon.LabelShort));
        if (Widgets.ButtonImage(dropRect, TexButton.Drop))
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            if (mount.TryUnmountWeapon(out ThingWithComps unmountedWeapon))
            {
                GenPlace.TryPlaceThing(unmountedWeapon, SelPawnForParasiticWeapons.PositionHeld, SelPawnForParasiticWeapons.MapHeld, ThingPlaceMode.Near);
            }
        }

        buttonX -= ButtonSize + ButtonGap;
        if (Widgets.InfoCardButton(buttonX, rowRect.y + 4f, weapon))
        {
        }

        if (Mouse.IsOver(rowRect))
        {
            TooltipHandler.TipRegion(rowRect, weapon.GetTooltip());
        }
        y += RowHeight;
    }
}
