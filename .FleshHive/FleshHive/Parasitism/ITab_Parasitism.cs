using RimWorld;
using Verse;

namespace FleshHive;

public class ITab_Parasitism : ITab
{
    public ITab_Parasitism()
    {
        this.labelKey = "Tab_Parasitism";
    }

    public override void OnOpen()
    {
        base.OnOpen();
        
        FleshParasitePod pod = this.SelThing as FleshParasitePod;
        if (pod != null)
        {
            Find.WindowStack.Add(new Window_Parasitism(pod));
        }
    }

    protected override void FillTab()
    {
        this.CloseTab();
    } 
}