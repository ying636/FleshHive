using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;


public class HediffCompProperties_Parasitism : HediffCompProperties
{
    public HediffCompProperties_Parasitism()
    {
        this.compClass = typeof(HediffComp_Parasitism);
    }
    
    public List<TentacleProperties> tentacles = new List<TentacleProperties>();
}

public class HediffComp_Parasitism : HediffComp
{
    public HediffCompProperties_Parasitism Props => (HediffCompProperties_Parasitism)this.props;
    public ParasitismHediff Hediff => (ParasitismHediff)this.parent;
    public virtual int TentacleCount => this.tentacles.Count;
    public virtual bool ShowAttackGizmo => true;
    public IEnumerable<Tentacle> AttackTentacles => ActiveTentacles.Where(tentacle => tentacle.CanAutoAttack);
    protected virtual IEnumerable<Tentacle> ActiveTentacles => this.tentacles;

    public override void CompPostMake()
    {
        base.CompPostMake(); 
        this.MakeTentacles();
    }

    public virtual void MakeTentacles()
    {
        int desiredCount = this.Props.tentacles?.Count ?? 0;
        while (this.tentacles.Count > desiredCount)
        {
            int lastIndex = this.tentacles.Count - 1;
            DropMountedWeaponIfNeeded(this.tentacles[lastIndex]);
            this.tentacles.RemoveAt(lastIndex);
        }

        for (int i = 0; i < this.tentacles.Count; i++)
        {
            Tentacle t = this.tentacles[i];
            t.Comp = this;
            if (t.prop == null)
            {
                t.prop = this.Props.tentacles[i];
            }
        }

        for (int i = this.tentacles.Count; i < desiredCount; i++)
        {
            TentacleProperties tentaclePropertiese = this.Props.tentacles[i];
            Tentacle t = (Tentacle)Activator.CreateInstance(tentaclePropertiese.tentacleClass, [tentaclePropertiese]);
            t.Comp = this;
            this.tentacles.Add(t);   
        }

        this.makeTentacles = true;
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        this.Pawn.Drawer.renderer.renderTree.SetDirty();
        this.Pawn.Drawer.renderer.EnsureGraphicsInitialized();
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        this.Pawn.Drawer.renderer.renderTree.SetDirty();
        this.Pawn.Drawer.renderer.EnsureGraphicsInitialized();
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        bool rare = this.Pawn.IsHashIntervalTick(25); 
        foreach (var tentacle in this.tentacles)
        { 
            tentacle.Tick();
            if (rare)
            { 
                tentacle.RareTick();
            }
        }
    }

    public virtual void GetAngle(ref int index, int count)
    {
        Vector2 drawSize = Vector2.one;
        if (!this.Pawn.RaceProps.Humanlike)
        {
            drawSize = this.Pawn.ageTracker.CurKindLifeStage.bodyGraphicData?.drawSize ?? Vector2.one;
        }

        float x = drawSize.x / 2.5f;
        float y = drawSize.y / 2.5f;
        float baseX = 0.55f * x;
        float z1 = 0;
        float z2 = 0.30f * y;
        float z3 = 0.50f * y;
        float ringStep = 0.18f * x;

        foreach (var t in ActiveTentacles)
        {
            int localIndex = index;
            int ring = (localIndex - 1) / 12;
            int slot = ((localIndex - 1) % 12) + 1;

            bool right = slot is 1 or 3 or 5 or 7 or 9 or 11;
            bool top = slot is 1 or 2 or 3 or 6 or 7 or 8;
            float tierZ = slot is 1 or 2 or 4 or 5 ? z1 : slot is 3 or 6 or 9 or 10 ? z2 : z3;

            float px = (baseX + ring * ringStep) * (right ? 1f : -1f);
            float pz = top ? tierZ : -tierZ;

            t.drawPosOffset = new Vector3(px, 0f, pz);
            float angleFlat = Mathf.Atan2(right ? pz : -pz, px) * Mathf.Rad2Deg;
            t.angle = 90f - angleFlat;
            t.isRight = right;
            
            index++;
        }
    }
    public virtual List<PawnRenderNode> CompRenderNodes()
    {
        var result = new List<PawnRenderNode>();
        Pawn pawn = this.Pawn; 
        foreach (var tentacle in ActiveTentacles)
        { 
            PawnNodeRender_Tentacle node = new PawnNodeRender_Tentacle(tentacle, pawn,
                tentacle.Prop.renderNode, pawn.Drawer.renderer.renderTree);
            result.Add(node);
        }
        return result;
    }

    public override void CompExposeData()
    {
        base.CompExposeData(); 
        if (Scribe.mode == LoadSaveMode.LoadingVars && this.tentacles.Count == 0)
        {
            this.MakeTentacles();
        }
        for (int i = 0; i < this.tentacles.Count; i++)
        {
            if (Scribe.EnterNode("tentacle" + i))
            {
                try
                {
                    this.tentacles[i].ExposeData();
                }
                finally
                {
                    Scribe.ExitNode();
                }
            }
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            NotifyAngleAssignmentAfterLoad();
        }
    }

    public bool makeTentacles;
    protected List<Tentacle> tentacles = new List<Tentacle>();

    private void DropMountedWeaponIfNeeded(Tentacle tentacle)
    {
        if (tentacle is not Tentacle_WeaponMount weaponMount)
        {
            return;
        }
        if (weaponMount.TryUnmountWeapon(out ThingWithComps weapon) && weapon != null && Pawn?.MapHeld != null && !weapon.Spawned)
        {
            GenPlace.TryPlaceThing(weapon, Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near);
        }
    }

    private void NotifyAngleAssignmentAfterLoad()
    {
        if (Pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is ParasitismSystem system)
        {
            system.NotifyAngleAssignmentAfterLoad();
        }
    }
}

 
