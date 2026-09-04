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

    public bool TryQueueTargetPawn(Pawn pawn)
    {
        if (pawn == null || !pawn.Spawned || pawn.Dead || !pawn.Downed
            || !pawn.RaceProps.Animal || this.curQuest != null || this.start || this.targetUI != null
            || this.target.Any || this.flesh.Any)
        {
            return false;
        }

        this.targetUI = pawn;
        this.system = pawn.health?.hediffSet?.GetFirstHediff<ParasitismSystem>();
        return true;
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
        SyncUiState();
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
            spaceRect.y += 9f;
            spaceRect.width = 12f;
            spaceRect.height = 12f;
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

                spaceRect.x += 14f;
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
        bool canQueueBase = this.curQuest == null && !this.start
                                                  && this.targetUI != null && comp != null;
        bool canStartBase = this.curQuest is not ParasiteQuest_Remove && !this.start
                                                  && this.target.Any && this.flesh.Any
                                                  && this.targetUI != null && comp != null;
        bool canRemoveBase = this.curQuest is ParasiteQuest_Remove && !this.start && this.target.Any;
        bool spaceOk = false;
        string reason = null;
        if (canQueueBase || canStartBase)
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
        bool canQueue = canQueueBase && spaceOk && HasEnoughNutrition();
        bool canStart = canStartBase && spaceOk && HasEnoughNutrition();
        bool canRemove = canRemoveBase && HasEnoughNutrition();
        if (canRemoveBase && !HasEnoughNutrition())
        {
            reason = "FleshParasitePod_InsufficientNutrition".Translate(ParasitismNutritionCost);
        }
        if (Widgets.ButtonText(cancelRect, "FleshParasitePod_CancelInsert".Translate()))
        {
            CancelInsert();
        }
        string taskStatus = GetTaskStatus();
        if (!taskStatus.NullOrEmpty())
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inRect.x, buttonsY, cancelRect.x - inRect.x - buttonGap, buttonH), taskStatus);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        if (ButtonTextPressedWhenDisabled(startRect, "FleshParasitePod_Start".Translate(), true, true, canQueue || canStart || canRemove))
        {
            if (this.curQuest == null && canQueue)
            {
                this.curQuest = new ParasiteQuest(this.targetUI, this.fleshUI);
            }
            if (this.curQuest != null && (canStart || canRemove))
            {
                this.curQuest.TryStart(this);
            }
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

    private void SyncUiState()
    {
        if (this.targetUI == null && this.target.Any)
        {
            this.targetUI = this.target[0];
        }
        if (this.fleshUI == null && this.flesh.Any)
        {
            this.fleshUI = this.flesh[0];
        }
        if (this.targetUI != null)
        {
            this.system = this.targetUI.health?.hediffSet?.GetFirstHediff<ParasitismSystem>();
        }
        if (this.fleshUI != null && this.cachedComp == null)
        {
            this.cachedComp = this.fleshUI.TryGetComp<ParasitismComp>();
        }
    }

    private string GetTaskStatus()
    {
        if (this.curQuest is ParasiteQuest_Remove removeQuest)
        {
            Pawn targetPawn = removeQuest.target ?? this.targetUI ?? (this.target.Any ? this.target[0] : null);
            Pawn fleshPawn = removeQuest.hd?.flesh;
            if (targetPawn == null || fleshPawn == null)
            {
                return "FleshParasitePod_CurQuest".Translate();
            }
            string key = this.start
                ? "FleshParasitePod_ExecutingRemoveTask"
                : "FleshParasitePod_SelectedRemoveTask";
            return key.Translate(targetPawn.LabelCap, fleshPawn.LabelCap);
        }

        if (this.curQuest is ParasiteQuest parasiteQuest)
        {
            Pawn targetPawn = parasiteQuest.target ?? this.targetUI ?? (this.target.Any ? this.target[0] : null);
            Pawn fleshPawn = parasiteQuest.flesh ?? this.fleshUI ?? (this.flesh.Any ? this.flesh[0] : null);
            if (targetPawn == null || fleshPawn == null)
            {
                return "FleshParasitePod_CurQuest".Translate();
            }
            string key = this.start
                ? "FleshParasitePod_ExecutingParasitismTask"
                : "FleshParasitePod_SelectedParasitismTask";
            return key.Translate(targetPawn.LabelCap, fleshPawn.LabelCap);
        }

        return null;
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
                int cost = comp.Props.cost;
                float blockGap = 2f;
                float blockSize = 8f;
                float blocksWidth = cost > 0 ? cost * blockSize + (cost - 1) * blockGap : 0f;
                Rect p = new Rect(costRect.x + (costRect.width - blocksWidth) / 2f,
                    costRect.y + (costRect.height - blockSize) / 2f, blockSize, blockSize);
                for (int i = 0; i < cost; i++)
                {
                    Widgets.DrawBoxSolid(p, Color.red);
                    Widgets.DrawBox(p, 1, BaseContent.BlackTex);
                    p.x += blockSize + blockGap;
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
        List<ParasitismDisplayEntry> entries = new List<ParasitismDisplayEntry>();
        foreach (var hd in this.system.ParasitismHediffs)
        {
            AddRuntimeParasitismEntries(entries, hd);
        }
        DrawParasitismEntries(rowRect, entries);
        Text.Font = GameFont.Small;
    }

    private void OnRemoveParasitismRequested(ParasitismHediff hediff)
    {
        if (this.curQuest == null)
        {
            Pawn targetPawn = this.targetUI ?? (this.target.Any ? this.target[0] : null);
            if (targetPawn == null)
            {
                Messages.Message("FleshParasitePod_CurQuest".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            this.targetUI = targetPawn;
            this.curQuest = new ParasiteQuest_Remove(targetPawn,hediff);
            this.curQuest.TryStart(this);
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
        rect.width = 12f;
        rect.height = 12f;
        rect.y += 9f;
        for (int i = 0; i < comp.Props.cost; i++)
        {
            Widgets.DrawBoxSolid(rect,Color.red);
            Widgets.DrawBox(rect,1,BaseContent.BlackTex);
            rect.x += rect.width + 4f;
        }

        list.Label("ParasitismAbility".Translate());
        Rect abilityRect = list.GetRect(72f);
        List<ParasitismDisplayEntry> entries = new List<ParasitismDisplayEntry>();
        AddDefParasitismEntries(entries, comp.Props.hediff, comp);
        DrawParasitismEntries(abilityRect, entries);
        list.End();
        Text.Font = GameFont.Small;
    }

    private void AddRuntimeParasitismEntries(List<ParasitismDisplayEntry> entries, ParasitismHediff hediff)
    {
        if (hediff?.comps == null)
        {
            return;
        }

        foreach (HediffComp hediffComp in hediff.comps)
        {
            if (hediffComp is HediffComp_Parasitism parasitismComp)
            {
                AddTentacleEntries(entries, parasitismComp.Tentacles, parasitismComp.Hediff?.Comp);
            }
            else if (hediffComp is HediffComp_GiveAbility abilityComp)
            {
                AddAbilityEntries(entries, abilityComp);
            }
        }
    }

    private void AddDefParasitismEntries(List<ParasitismDisplayEntry> entries, HediffDef hediffDef, ParasitismComp sourceComp)
    {
        if (hediffDef?.comps == null)
        {
            return;
        }

        foreach (HediffCompProperties hediffComp in hediffDef.comps)
        {
            if (hediffComp is HediffCompProperties_Parasitism parasitismProps)
            {
                AddTentacleEntries(entries, parasitismProps.tentacles, sourceComp);
            }
            else if (hediffComp is HediffCompProperties_GiveAbility abilityProps)
            {
                if (abilityProps.abilityDef != null)
                {
                    entries.Add(CreateAbilityEntry(abilityProps.abilityDef));
                }
                if (!abilityProps.abilityDefs.NullOrEmpty())
                {
                    foreach (AbilityDef abilityDef in abilityProps.abilityDefs)
                    {
                        if (abilityDef != null)
                        {
                            entries.Add(CreateAbilityEntry(abilityDef));
                        }
                    }
                }
            }
        }
    }

    private void AddTentacleEntries(List<ParasitismDisplayEntry> entries, IEnumerable<Tentacle> tentacles,
        ParasitismComp sourceComp)
    {
        List<Tentacle> tentacleList = tentacles?.ToList() ?? new List<Tentacle>();
        string label = sourceComp?.Props.abilityLabel;
        if (label.NullOrEmpty())
        {
            label = "FH_ParasiticTentacle".Translate();
        }
        string description = sourceComp?.Props.abilityDescription;
        for (int i = 0; i < tentacleList.Count; i++)
        {
            Tentacle tentacle = tentacleList[i];
            Texture2D icon = BaseContent.BadTex;
            if (tentacle?.Prop != null && !tentacle.Prop.iconPath.NullOrEmpty())
            {
                icon = tentacle.Icon ?? icon;
            }
            entries.Add(new ParasitismDisplayEntry(icon, label, description));
        }
    }

    private void AddTentacleEntries(List<ParasitismDisplayEntry> entries, IEnumerable<TentacleProperties> tentacleProperties,
        ParasitismComp sourceComp)
    {
        List<TentacleProperties> properties = tentacleProperties?.ToList() ?? new List<TentacleProperties>();
        string label = sourceComp?.Props.abilityLabel;
        if (label.NullOrEmpty())
        {
            label = "FH_ParasiticTentacle".Translate();
        }
        string description = sourceComp?.Props.abilityDescription;
        for (int i = 0; i < properties.Count; i++)
        {
            TentacleProperties property = properties[i];
            Texture2D icon = BaseContent.BadTex;
            if (property != null && !property.iconPath.NullOrEmpty())
            {
                icon = ContentFinder<Texture2D>.Get(property.iconPath, false) ?? icon;
            }
            entries.Add(new ParasitismDisplayEntry(icon, label, description));
        }
    }

    private void AddAbilityEntries(List<ParasitismDisplayEntry> entries, HediffComp_GiveAbility abilityComp)
    {
        HediffCompProperties_GiveAbility props = abilityComp?.props as HediffCompProperties_GiveAbility;
        if (props == null)
        {
            return;
        }
        if (props.abilityDef != null)
        {
            entries.Add(CreateAbilityEntry(props.abilityDef));
        }
        if (!props.abilityDefs.NullOrEmpty())
        {
            foreach (AbilityDef abilityDef in props.abilityDefs)
            {
                if (abilityDef != null)
                {
                    entries.Add(CreateAbilityEntry(abilityDef));
                }
            }
        }
    }

    private ParasitismDisplayEntry CreateAbilityEntry(AbilityDef abilityDef)
    {
        Texture2D icon = abilityDef.uiIcon;
        if ((icon == null || icon == BaseContent.BadTex) && !abilityDef.iconPath.NullOrEmpty())
        {
            icon = ContentFinder<Texture2D>.Get(abilityDef.iconPath, false) ?? BaseContent.BadTex;
        }
        return new ParasitismDisplayEntry(icon, abilityDef.LabelCap, abilityDef.description);
    }

    private void DrawParasitismEntries(Rect rect, List<ParasitismDisplayEntry> entries)
    {
        if (entries.NullOrEmpty())
        {
            return;
        }

        const float itemSize = 56f;
        const float iconSize = 48f;
        const float gap = 8f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width + gap) / (itemSize + gap)));
        GameFont font = Text.Font;
        Text.Font = GameFont.Tiny;
        for (int i = 0; i < entries.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rect itemRect = new Rect(rect.x + column * (itemSize + gap), rect.y + row * (itemSize + gap),
                itemSize, itemSize);
            Rect iconRect = new Rect(itemRect.x + (itemSize - iconSize) / 2f,
                itemRect.y + (itemSize - iconSize) / 2f, iconSize, iconSize);
            Widgets.DrawBoxSolid(iconRect, Color.black);
            Widgets.DrawBox(iconRect, 1, BaseContent.GreyTex);
            Widgets.DrawTextureFitted(iconRect.ContractedBy(3f), entries[i].Icon, 1f);
            TooltipHandler.TipRegion(itemRect, entries[i].Label + "\n" + entries[i].Description);
        }
        Text.Font = font;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private readonly struct ParasitismDisplayEntry
    {
        public ParasitismDisplayEntry(Texture2D icon, string label, string description)
        {
            Icon = icon ?? BaseContent.BadTex;
            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public readonly Texture2D Icon;
        public readonly string Label;
        public readonly string Description;
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
                            if (this.curQuest is ParasiteQuest quest && this.curQuest is not ParasiteQuest_Remove)
                            {
                                quest.flesh = pawn;
                            }
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
        TextAnchor anchor = Text.Anchor;
        bool wordWrap = Text.WordWrap;
        Text.Anchor = TextAnchor.UpperCenter;
        Text.WordWrap = true;
        Widgets.Label(new Rect(targetRect.x, targetRect.y + targetRect.height + 5f, targetRect.width, 38f),
            this.fleshUI == null ? "FleshParasitePod_Flesh".Translate().ToString() : this.fleshUI.LabelCap);
        Text.WordWrap = wordWrap;
        Text.Anchor = anchor;
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
        TextAnchor anchor = Text.Anchor;
        bool wordWrap = Text.WordWrap;
        Text.Anchor = TextAnchor.UpperCenter;
        Text.WordWrap = true;
        Widgets.Label(new Rect(targetRect.x, targetRect.y + targetRect.height + 5f, targetRect.width, 38f),
            this.targetUI == null ? "FleshParasitePod_Target".Translate().ToString() : this.targetUI.LabelCap);
        Text.WordWrap = wordWrap;
        Text.Anchor = anchor;
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

    protected bool TryConsumeNutrition(FleshParasitePod pod)
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
        if (pod.target.Any && TryConsumeNutrition(pod))
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
