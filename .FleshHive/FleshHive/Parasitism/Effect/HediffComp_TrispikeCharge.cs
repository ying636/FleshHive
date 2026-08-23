using Verse;

namespace FleshHive.Effect;

public class HediffCompProperties_TrispikeCharge : HediffCompProperties
{
    public HediffCompProperties_TrispikeCharge()
    {
        this.compClass = typeof(HediffComp_TrispikeCharge);
    }

    public bool active;
    public int fillCount = 10;
}

public class HediffComp_TrispikeCharge : HediffComp
{
    public HediffCompProperties_TrispikeCharge Props => (HediffCompProperties_TrispikeCharge)this.props;

    public bool Active => this.active;

    public override void CompPostMake()
    {
        base.CompPostMake();
        if (!this.initialized)
        {
            this.active = this.Props.active;
            this.initialized = true;
        }
    }

    public void SetActive(bool value)
    {
        this.active = value;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref this.active, "active");
        Scribe_Values.Look(ref this.initialized, "initialized");
    }

    private bool active;
    private bool initialized;
}

