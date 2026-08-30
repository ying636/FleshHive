using System.Linq;
using System.Text;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace FleshHive;

public class FleshParasitePod : Building, IThingHolder, IThingHolderWithDrawnPawn, IRenameable
{
    public FleshParasitePod()
    {
        this.flesh = new ThingOwner<Pawn>(this,LookMode.Deep);
        this.target = new ThingOwner<Pawn>(this,LookMode.Deep); 
    }

    public string RenamableLabel
    {
        get => customName ?? BaseLabel;
        set => customName = value;
    }

    public string BaseLabel => def.LabelCap;

    public string InspectLabel => RenamableLabel;

    public override string Label => RenamableLabel;

    public float HeldPawnDrawPos_Y => DrawPos.y + 0.03658537f;

    public float HeldPawnBodyAngle => base.Rotation.AsAngle;

    public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

    public ParasitismComp CachedComp
    {
        get
        {
            if (this.cachedComp == null)
            {
                if (this.fleshUI != null)
                {
                    this.cachedComp = this.fleshUI.TryGetComp<ParasitismComp>();
                }
            }
            return this.cachedComp;
        }
    }
    public int TickToParasite => GenDate.TicksPerHour;

    public void Start()
    {
        this.start = true;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (start)
        {
            this.progress += delta;
            if (this.progress >= this.TickToParasite)
            { 
                this.curQuest.Do(this);
            }
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (Prefs.DevMode)
        {
            if (this.start)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "Finish Parasitism",
                    action = () => this.curQuest.Do(this)
                };
            }
        }
    }

    public void FinishParasitism()
    {
        this.progress = 0;
        this.start = false;
        Pawn pawn = this.target[0];
        ParasitismSystem system = pawn.health.hediffSet.GetFirstHediff<ParasitismSystem>();
        if (system == null)
        {
            system = (ParasitismSystem?)pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
        }

        bool fail = false;
        if (!this.flesh.Any)
        {
            fail = true;
        }
        else
        {
            var fleshTarget = this.flesh[0];
            if (fleshTarget != null && system.Parasite(fleshTarget))
            {
                this.flesh.Remove(fleshTarget);
                if (fleshTarget.TryGetComp<ParasitismComp>()?.Props.synchronizeHost == true)
                {
                    system.EnsureSynchronizedReplicaSpawned(fleshTarget);
                }
                Find.LetterStack.ReceiveLetter("ParasitismSucceed".Translate(pawn.Label,fleshTarget.Label),
                    "ParasitismSucceedDesc".Translate(pawn.Label,fleshTarget.Label),LetterDefOf.PositiveEvent,this);
            }
            else
            {
                fail = true;
                this.flesh.TryDropAll(this.Position,this.Map, ThingPlaceMode.Near);
            }   
        }

        if (fail)
        {
            Find.LetterStack.ReceiveLetter("ParasitismFail".Translate(pawn.Label),
                "ParasitismFailDesc".Translate(pawn.Label),LetterDefOf.NegativeEvent,this);
        } 
        this.target.TryDrop(pawn, ThingPlaceMode.Near, out _);
        this.fleshUI = null;
        this.targetUI = null;
        this.system = null;
        this.cachedComp = null;
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        string baseStr = base.GetInspectString();
        if (!baseStr.NullOrEmpty())
        {
            sb.AppendLine(baseStr);
        }
        sb.AppendLine("FleshParasitePod_NutritionCost".Translate(ParasitismNutritionCost));

        string fleshName = "None".Translate();
        if (flesh.Any)
        {
            fleshName = flesh[0].Label;
        }
        string targetName = "None".Translate();
        if (target.Any)
        {
            targetName = target[0].Label;
        }
        sb.AppendLine("FleshParasitePod_Inner".Translate(fleshName,targetName));
        if (this.start)
        {
            sb.AppendLine("FleshParasitePod_Progress".Translate(((float)this.progress / (float)this.TickToParasite).ToStringPercent()));
        }
        return sb.ToString().TrimEnd();
    }

    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        if (!target.Any)
        {
            return;
        }

        Pawn pawn = target[0];
        pawn.Drawer.renderer.DynamicDrawPhaseAt(
            phase,
            DrawPos + Altitudes.AltIncVect * PawnDrawAltitudeOffset,
            null,
            neverAimWeapon: true);
    }

    public void Draw(Rect inRect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width,40f);
        Widgets.Label(titleRect,"FleshParasitePod_Title".Translate()); 
        Rect targetRect = new Rect(inRect.x + 10f, inRect.y + 45f, 100f, 100f);
        Widgets.DrawBoxSolid(targetRect,Color.black);
        Widgets.DrawBox(targetRect);

        if (this.system != null)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            string text = "ParasitismCapacity".Translate(this.system.Count, this.system.Limit);
            Rect systemRect = new Rect(inRect.x + 10f, targetRect.yMax + 50f, text.GetWidthCached(), 30f);
            Widgets.Label(systemRect, text);
            systemRect.x += systemRect.width + 5f;
            Rect spaceRect = systemRect;
            spaceRect.y += 7f;
            spaceRect.width = 15f;
            spaceRect.height = 15f;
            var count = system.Count;
            for (int i = 0; i < this.system.Limit; i++)
            {
                if (count > 0)
                {
                    Widgets.DrawBoxSolid(spaceRect, Color.red);
                    Widgets.DrawBox(spaceRect,1,BaseContent.BlackTex);
                }
                else
                {
                    Widgets.DrawBoxSolid(spaceRect, Color.gray); 
                }

                spaceRect.x += 17f;
                count--;
            }

            string hungerText = "FH_HungerRate".Translate(this.system.HungerRate.ToStringPercent());
            Widgets.Label(new Rect(inRect.x + 10f, systemRect.y + 25f,hungerText.GetWidthCached() ,30f),hungerText );
            Widgets.DrawLineHorizontal(inRect.x, systemRect.y + 50f, inRect.width,Color.gray);
            Rect parasitizedRect = new Rect(inRect.x + 10f, systemRect.y + 55f, inRect.width - 20f, 120f);
            DrawParasitizedFleshBeasts(parasitizedRect);
            Widgets.DrawLineHorizontal(inRect.x, parasitizedRect.yMax + 10f, inRect.width,Color.gray);
            Rect abilityRect = new Rect(inRect.x + 10f, parasitizedRect.yMax + 15f, inRect.width - 20f, 90f);
            DrawParasitismAbilities(abilityRect);
            Text.Anchor = TextAnchor.MiddleCenter;
        }
        else
        {
            string text = "Unparasitized".Translate();
            Rect systemRect = new Rect(inRect.x + 20f, inRect.y + 180f, text.GetWidthCached(), 30f);
            Widgets.Label(systemRect, text);
        }

        Rect t1Rect = new Rect(targetRect.x + 10f, targetRect.y + 10f
            , targetRect.width - 20f, targetRect.height - 15f);
        DrawTarget(t1Rect, targetRect);
        Widgets.DrawTextureFitted(new Rect(targetRect.x + targetRect.width + 25f, targetRect.y + targetRect.height /2 - 10f, 
            20f,20f),FHUtitly.right,1);
        targetRect.x += targetRect.width + 70f;
        DrawFlesh(targetRect);
        Rect infoRect = new Rect(targetRect.x + targetRect.width + 20f, targetRect.y, 
            inRect.x + inRect.width - (targetRect.x + targetRect.width + 30f), 
            Mathf.Max(targetRect.height + 50f, 150f));
        DrawFleshInfo(infoRect);
 

        float buttonW = 140f;
        float buttonH = 38f;
        float buttonGap = 12f;
        float warnH = 22f;
        float buttonsY = inRect.yMax - 10f - buttonH - warnH;
        Rect startRect = new Rect(inRect.xMax - 10f - buttonW, buttonsY, buttonW, buttonH);
        Rect cancelRect = new Rect(startRect.x - buttonGap - buttonW, startRect.y, buttonW, buttonH);
        var comp = this.fleshUI?.TryGetComp<ParasitismComp>();
        bool canStartBase = this.curQuest == null && !this.start
                                                  && this.targetUI != null && comp != null;
        bool spaceOk = false;
        string reason = null;
        if (canStartBase)
        {
            int need = comp.Props.cost;
            int capacity = 0;
            if (this.system == null)
            {
                capacity = Mathf.FloorToInt((this.targetUI ?? this.target[0]).GetStatValue(FleshHiveDefOf.FH_Stat_ParasitismCapacity));
            }
            else
            {
                capacity = this.system.Limit - this.system.Count;
            }
            spaceOk = capacity >= need;
            if (!spaceOk)
            {
                reason = "FleshParasitePod_InsufficientCapacity".Translate();   
            }
            else if (!HasEnoughNutrition())
            {
                reason = "FleshParasitePod_InsufficientNutrition".Translate(ParasitismNutritionCost);
            }
        }
        bool canStart = canStartBase && spaceOk && HasEnoughNutrition();
        if (Widgets.ButtonText(cancelRect, "FleshParasitePod_CancelInsert".Translate()))
        {
            CancelInsert();
        }
        if (ButtonTextPressedWhenDisabled(startRect, "FleshParasitePod_Start".Translate(), true, true, canStart))
        {
            this.curQuest = new ParasiteQuest(this.targetUI, this.fleshUI);
        }

        if (this.curQuest != null)
        {
            reason = "FleshParasitePod_CurQuest".Translate();  
        }
        if (this.start)
        {
            reason = "FleshParasitePod_Starting".Translate();  
        }
        if (reason != null)
        {
            var font = Text.Font;
            var anchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(startRect.x, startRect.yMax + 2f, startRect.width, warnH), 
                reason);
            Text.Font = font;
            Text.Anchor = anchor;
        }

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private bool ButtonTextPressedWhenDisabled(Rect rect, string label, bool drawBackground = true, bool doMouseoverSound = true, bool active = true, TextAnchor? overrideTextAnchor = null)
    {
        TextAnchor anchor = Text.Anchor;
        Color color = GUI.color;
        if (drawBackground)
        {
            if (!active)
            {
                Widgets.DrawAtlas(rect, Widgets.ButtonBGAtlasClick);
            }
            else
            {
                Widgets.DrawButtonGraphic(rect);
            }
        }
        if (doMouseoverSound)
        {
            MouseoverSounds.DoRegion(rect);
        }
        if (overrideTextAnchor != null)
        {
            Text.Anchor = overrideTextAnchor.Value;
        }
        else if (drawBackground)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
        }
        else
        {
            Text.Anchor = TextAnchor.MiddleLeft;
        }
        bool wordWrap = Text.WordWrap;
        if (rect.height < Text.LineHeight * 2f)
        {
            Text.WordWrap = false;
        }
        Widgets.Label(rect, label);
        Text.Anchor = anchor;
        GUI.color = color;
        Text.WordWrap = wordWrap;
        if (!active)
        {
            return false;
        }
        return Widgets.ButtonInvisible(rect, false);
    }

    private bool HasEnoughNutrition()
    {
        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(Map);
        return fleshHive != null && fleshHive.nutrition >= ParasitismNutritionCost;
    }

    private void CancelInsert()
    {
        this.curQuest = null;
        this.start = false;
        this.progress = 0;
        this.targetUI = null;
        this.fleshUI = null;
        this.system = null;
        this.cachedComp = null;
        if (this.target.Any)
        {
            Pawn pawn = this.target[0];
            this.target.TryDrop(pawn, ThingPlaceMode.Near, out _);
        }
        if (this.flesh.Any)
        {
            Pawn pawn = this.flesh[0];
            this.flesh.TryDrop(pawn, ThingPlaceMode.Near, out _);
        }
    }

    private void DrawParasitizedFleshBeasts(Rect rect)
    {
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "FleshParasitePod_ParasitizedFleshBeasts".Translate());
        Rect rowRect = new Rect(rect.x, rect.y + 28f, rect.width, rect.height - 28f);
        float cellW = 70f;
        float iconSize = 50f;
        float gap = 5f;
        float x = rowRect.x;
        float y = rowRect.y;
        foreach (var hd in this.system.ParasitismHediffs)
        {
            if (hd?.flesh == null) continue;
            var comp = hd.flesh.TryGetComp<ParasitismComp>();
            Rect cell = new Rect(x, y, cellW, rowRect.height);
            Rect iconRect = new Rect(cell.x, cell.y, iconSize, iconSize);
            Widgets.DrawBoxSolid(iconRect, Color.black);
            Widgets.DrawBox(iconRect,1,BaseContent.GreyTex);
            Widgets.ThingIcon(iconRect.ContractedBy(4f), hd.flesh, 1f, null, false, 0.8f);
            if (comp != null)
            {
                Rect costRect = new Rect(iconRect.x, iconRect.yMax + 2f, iconRect.width, 12f);
                Rect p = new Rect(costRect.x, costRect.y, 10f, 10f);
                for (int i = 0; i < comp.Props.cost; i++)
                {
                    Widgets.DrawBoxSolid(p, Color.red);
                    Widgets.DrawBox(p, 1, BaseContent.BlackTex);
                    p.x += p.width + 2f;
                }
                TooltipHandler.TipRegion(iconRect, hd.flesh.LabelCap);
            }
            Rect btnRect = new Rect(cell.x, iconRect.yMax + 18f, iconSize, 26f);
            if (Widgets.ButtonText(btnRect, "FleshParasitePod_Remove".Translate()))
            {
                OnRemoveParasitismRequested(hd);
            }
            x += cellW + gap;
            if (x + cellW > rowRect.xMax)
            {
                x = rowRect.x;
                y += iconSize + 40f;
            }
        }
        Text.Font = GameFont.Small;
    }

    private void DrawParasitismAbilities(Rect rect)
    {
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "ParasitismAbility".Translate());
        Rect rowRect = new Rect(rect.x, rect.y + 28f, rect.width, rect.height - 28f);
        float size = 40f;
        float gap = 6f;
        float x = rowRect.x;
        float y = rowRect.y;
        foreach (var hd in this.system.ParasitismHediffs)
        {
            if (hd?.flesh == null) continue;
            var comp = hd.flesh.TryGetComp<ParasitismComp>();
            Rect box = new Rect(x, y, size, size);
            Widgets.DrawBoxSolid(box, Color.black);
            Widgets.DrawBox(box,1,BaseContent.GreyTex);
            if (comp != null)
            {
                Widgets.DrawTextureFitted(box.ContractedBy(4f),comp.Icon,1f);
                if (!comp.Props.abilityLabel.NullOrEmpty() || !comp.Props.abilityDescription.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(box, comp.Props.abilityLabel + "\n" + comp.Props.abilityDescription);
                }
            }
            x += size + gap;
            if (x + size > rowRect.xMax)
            {
                x = rowRect.x;
                y += size + gap;
            }
        }
        Text.Font = GameFont.Small;
    }

    private void OnRemoveParasitismRequested(ParasitismHediff hediff)
    {
        if (this.curQuest == null)
        {
            this.curQuest = new ParasiteQuest_Remove(this.targetUI,hediff);
        }
        else
        {
            Messages.Message("FleshParasitePod_CurQuest".Translate(), MessageTypeDefOf.RejectInput, false);
        } 
    }

    private void DrawFleshInfo(Rect infoRect)
    {
        Color bg = new Color32(70, 70, 70, 255);
        Widgets.DrawBoxSolid(infoRect, bg);
        infoRect.width -= 5f; 
        var comp = this.CachedComp;
        if (comp == null)
        {
            return;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        Listing_Standard list = new Listing_Standard();
        infoRect.x += 5f;
        list.Begin(infoRect);
        Text.Font = GameFont.Small;
        list.Label("ParasitismFleshBeast".Translate(fleshUI.LabelCap));
        Text.Font = GameFont.Tiny;
        var rect =list.GetRect(30f);
        var text = "ParasitismCapacityCost".Translate();
        Widgets.Label(rect,text);
        rect.x += text.GetWidthCached();
        rect.width = 15f;
        rect.height = 15f;
        rect.y += 7.5f;
        for (int i = 0; i < comp.Props.cost; i++)
        {
            Widgets.DrawBoxSolid(rect,Color.red);
            Widgets.DrawBox(rect,1,BaseContent.BlackTex);
            rect.x += rect.width + 5f; 
        }

        list.Label(comp.Props.effect);
        list.Label("ParasitismAbility".Translate());
        var iconRect =list.GetRect(30f);
        iconRect.width = 30f;
        iconRect.x += 5f;
        iconRect.y += 5f;
        Widgets.DrawTextureFitted(iconRect,comp.Icon,1.5f);
        TooltipHandler.TipRegion(iconRect,comp.Props.abilityLabel + "\n" +comp.Props.abilityDescription); 
        list.End();
        Text.Font = GameFont.Small;
    }

    private void DrawFlesh(Rect targetRect)
    {
        Rect t1Rect;
        Widgets.DrawBoxSolid(targetRect,Color.black);
        Widgets.DrawBox(targetRect);
        t1Rect = new Rect(targetRect.x + 10f, targetRect.y + 10f
            , targetRect.width - 20f, targetRect.height - 15f);
        if (fleshUI != null)
        {
            Widgets.ThingIcon(t1Rect,fleshUI,1,null,false,0.85f);
        }
        else
        {
            Widgets.DrawTextureFitted(t1Rect,FHUtitly.RemoveFromPlatform,1f);
        }
        if (Widgets.ButtonInvisible(targetRect))
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (var pawn in this.Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Faction?.IsPlayer == true &&
                    pawn.TryGetComp<ParasitismComp>() is {} comp
                    && pawn.CanReserveAndReach(this,PathEndMode.Touch,Danger.Deadly))
                {
                    options.Add(new FloatMenuOption(pawn.Label,() =>
                        {
                            this.fleshUI = pawn;
                            this.cachedComp = null;
                        },
                            pawn.def.uiIcon,Color.white,MenuOptionPriority.Default
                        ,null,null,28f,r =>
                        {
                            Widgets.InfoCardButton(
                                r.x + (r.width - 24f) / 2f,
                                r.y + (r.height - 24f) / 2f,
                                pawn);
                            return false; 
                        }));
                }
            }
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        Widgets.Label(new Rect(targetRect.x,targetRect.y + targetRect.height + 5f,targetRect.width,20f),
            this.fleshUI == null ? "FleshParasitePod_Flesh".Translate().ToString() : this.fleshUI.Label);
    }

    private void DrawTarget(Rect t1Rect, Rect targetRect)
    {
        if (targetUI != null)
        {
            Rect iconRect = new Rect(targetRect.x + 5f, targetRect.y + 4f,
                targetRect.width - 10f, targetRect.height);
            Widgets.ThingIcon(iconRect ,targetUI,1,null,false,0.8f);
        }
        else
        {
            Widgets.DrawTextureFitted(t1Rect,FHUtitly.InsertPersonSubcoreScanner,1f);
        }
        
        if (Widgets.ButtonInvisible(targetRect))
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (var pawn in this.Map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.CanReserveAndReach(this,PathEndMode.Touch,Danger.Deadly))
                {
                    options.Add(new FloatMenuOption(pawn.Label,() => SelectTarget(pawn)));
                }
            }
            if (options.Any())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
        Widgets.Label(new Rect(targetRect.x,targetRect.y + targetRect.height + 5f,targetRect.width,20f),
            this.targetUI == null ? "FleshParasitePod_Target".Translate().ToString() : this.targetUI.Label);
    }

    public void SelectTarget(Pawn pawn)
    {
        this.targetUI = pawn;
        this.system = pawn.health.hediffSet.GetFirstHediff<ParasitismSystem>();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref start,"start");
        Scribe_Values.Look(ref progress,"progress");
        Scribe_Values.Look(ref customName, "customName");
        Scribe_Deep.Look(ref curQuest, "curQuest");
        Scribe_Deep.Look(ref flesh,"flesh",new object[]
        {
            this
        });
        Scribe_Deep.Look(ref target,"target",new object[]
        {
            this
        });
    }
    
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, flesh);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return target;
    }
    
    public int progress;
    public bool start;
    public ThingOwner<Pawn> flesh;
    public ThingOwner<Pawn> target;
    public ParasiteQuest curQuest = null;


    public ParasitismSystem system;
    public Pawn targetUI;
    public Pawn fleshUI;
    public ParasitismComp cachedComp;

    public const float ParasitismNutritionCost = 1f;

    private const float PawnDrawAltitudeOffset = 0.5f;

    private string? customName;
    
}


public class ParasiteQuest : IExposable
{
    public ParasiteQuest()
    {
    }

    public ParasiteQuest(Pawn target,Pawn flesh)
    {
        this.target = target;
        this.flesh = flesh;
    }

    public virtual void TryStart(FleshParasitePod pod)
    {
        if (pod.flesh.Any && pod.target.Any && TryConsumeNutrition(pod))
        {
            pod.Start();
        }
    }

    public virtual void Do(FleshParasitePod pod)
    {
        pod.FinishParasitism();
        pod.curQuest = null;
    }

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref target, "target");
        Scribe_References.Look(ref flesh, "flesh");
    }

    private bool TryConsumeNutrition(FleshParasitePod pod)
    {
        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(pod.Map);
        if (fleshHive == null || fleshHive.nutrition < FleshParasitePod.ParasitismNutritionCost)
        {
            Messages.Message("FleshParasitePod_InsufficientNutrition".Translate(FleshParasitePod.ParasitismNutritionCost), MessageTypeDefOf.RejectInput, false);
            return false;
        }
        fleshHive.nutrition = Mathf.Max(0f, fleshHive.nutrition - FleshParasitePod.ParasitismNutritionCost);
        return true;
    }

    public Pawn target;
    public Pawn flesh;
}

public class ParasiteQuest_Remove : ParasiteQuest
{
    public ParasiteQuest_Remove()
    {
    }

    public ParasiteQuest_Remove(Pawn target,ParasitismHediff hd)
    {
        this.target = target;
        this.hd = hd;
    }
    public override void TryStart(FleshParasitePod pod)
    { 
        if (pod.target.Any)
        {
            pod.Start();
        }
    }

    public override void Do(FleshParasitePod pod)
    {
        if (pod.target.Any)
        {
            Pawn pawn = pod.target[0];
            if (pawn.health.hediffSet.GetFirstHediff<ParasitismSystem>() is {} system)
            {
                system.RemoveFlesh(this.hd,pod);
            }
            pod.target.TryDropAll(pod.Position,pod.Map,ThingPlaceMode.Near);
        }

        pod.curQuest = null; 
        pod.progress = 0;
        pod.start = false;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref hd,"hd");
    }

    public ParasitismHediff hd;
}
