using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class TentacleProperties
{
    public Type tentacleClass = typeof(Tentacle);
    public float rotatingTime = 15;
    public float rotatingAngle = 60;
    public float damageAmount = 5;
    public DamageDef damageDef;
    public float armorPenetration;
    public float cooldown = 2f;
    public PawnRenderNodeProperties renderNode;
    [NoTranslate]
    public string iconPath;
}

public class Tentacle : IExposable
{
    public Tentacle()
    {
    }

    public Tentacle(TentacleProperties Prop)
    {
        this.prop = Prop;
    }

    public TentacleProperties Prop => this.prop;
    public virtual bool CanAutoAttack => false;
    public Texture2D Icon
    {
        get
        {
            if (!iconResolved)
            {
                iconResolved = true;
                if (!Prop.iconPath.NullOrEmpty())
                {
                    icon = ContentFinder<Texture2D>.Get(Prop.iconPath, false);
                }
                if (icon == null)
                {
                    Log.Error($"[FleshHive] Missing tentacle gizmo icon: {Prop.iconPath ?? "null"}");
                    icon = BaseContent.BadTex;
                }
            }
            return icon;
        }
    }
    public bool AutoAttackEnabled
    {
        get => autoAttackEnabled;
        set
        {
            if (autoAttackEnabled == value)
            {
                return;
            }
            autoAttackEnabled = value;
            if (!autoAttackEnabled)
            {
                NotifyAutoAttackDisabled();
            }
        }
    }

    public virtual void Tick()
    {
        if (this.rotateTime > 0)
        {
            this.rotateTime--;
            this.extraAngle = Mathf.Lerp(Prop.rotatingAngle, -this.Prop.rotatingAngle, this.rotateTime / this.Prop.rotatingTime);
            if (this.rotateTime <= 0)
            {
                this.targetAngle = -1f;
                this.extraAngle = -1f;
            }
        }
        else
        {
            this.idleTime += this.idle_Positive ? 1 : -1;
            this.extraAngle = Mathf.Lerp(-5, 5, (float)this.idleTime / 100f);
            if (this.idleTime > 100)
            {
                this.idle_Positive = false;
            }
            if (this.idleTime <= 0)
            {
                this.idle_Positive = true;
            }
        }
    }

    public virtual void RareTick()
    {
    }

    public virtual void Attack(Thing t)
    {
    }

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref this.rotateTime, "rotateTime");
        Scribe_Values.Look(ref this.targetAngle, "targetAngle");
        Scribe_Values.Look(ref this.extraAngle, "extraAngle");
        Scribe_Values.Look(ref autoAttackEnabled, "autoAttackEnabled", true);
    }

    protected virtual void NotifyAutoAttackDisabled()
    {
    }

    public int idleTime;
    public bool idle_Positive;
    public float extraAngle;
    public float rotateTime;
    public float angle;
    public bool isRight;
    public Vector3 drawPosOffset;
    public float targetAngle = -1f;
    public HediffComp_Parasitism Comp;
    public TentacleProperties prop;
    private Texture2D icon;
    private bool iconResolved;
    private bool autoAttackEnabled = true;
}
