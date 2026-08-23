using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Window_FleshHiveFormula : Window
{
    public Window_FleshHiveFormula(ThingWithComps producer)
    {
        this.producer = producer;
        formulaSpawner = producer.TryGetComp<CompHiveFormulaSpawner>();
        doCloseX = true;
        forcePause = false;
        focusWhenOpened = false;
        preventCameraMotion = false;
        resizeable = true;
    }

    public override Vector2 InitialSize => new Vector2(760f, 680f);

    public override void DoWindowContents(Rect inRect)
    {
        if (producer == null || producer.Destroyed || formulaSpawner == null)
        {
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "FH_Gestation_CustomFormulaTitle".Translate());
        Text.Font = GameFont.Small;

        Rect contentRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, inRect.height - 36f);
        formulaSpawner.Draw(contentRect, out _);
    }

    private readonly ThingWithComps producer;
    private readonly CompHiveFormulaSpawner? formulaSpawner;
}
