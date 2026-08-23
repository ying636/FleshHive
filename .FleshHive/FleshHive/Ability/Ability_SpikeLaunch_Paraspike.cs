using RimWorld;
using Verse;

namespace FleshHive;

public class Ability_SpikeLaunch_Paraspike : Ability
{
    public Ability_SpikeLaunch_Paraspike(Pawn pawn) : base(pawn)
    { 
    } 
    public Ability_SpikeLaunch_Paraspike(Pawn pawn, AbilityDef def) : base(pawn, def)
    {  
    }
    public override AcceptanceReport CanCast
        => base.CanCast && !this.pawn.health.hediffSet.HasHediff(FleshHiveDefOf.FH_LostSpike_Paraspike);
}