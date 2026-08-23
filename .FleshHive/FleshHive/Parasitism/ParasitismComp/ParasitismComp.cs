using UnityEngine;
using Verse;

namespace FleshHive;

public class ParasitismCompProperties : CompProperties
{
    public ParasitismCompProperties()
    {
        this.compClass = typeof(ParasitismComp);
    }

    public int cost = 1;
    [NoTranslate]
    public string iconPath;
    public HediffDef hediff;
    [MustTranslate]
    public string effect;
    [MustTranslate]
    public string abilityLabel;
    [MustTranslate]
    public string abilityDescription;

    public bool drawIcon = true;

    public int twistedFleshCapacity;

    public bool synchronizeHost;
}

public class ParasitismComp : ThingComp
{
    public ParasitismCompProperties Props => (ParasitismCompProperties)this.props;
    public Texture2D Icon
    {
        get
        {
            if (icon == null)
            {
                icon = ContentFinder<Texture2D>.Get(Props.iconPath);
            }
            return icon;
        }
    }
 

    Texture2D icon;  
}
