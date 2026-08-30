using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

[DefOf]
[StaticConstructorOnStartup]
public static class FleshHiveDefOf
{
    public static HediffDef FH_Spike_Paraspike;
    public static HediffDef FH_LostSpike_Paraspike;
    public static HediffDef FH_ParasitismSystem;
    public static HediffDef FH_FleshAdaptation;
    public static HediffDef FH_FleshSymbiosis;
    public static HediffDef FH_Parasitism_Paraspike;
    public static HediffDef FH_Unification;
    public static HediffDef FH_Berserk;
    public static HediffDef FH_FleshUndying;
    public static HediffDef FH_Trait_BoneSpurGrowth;
    public static HediffDef FH_Hela;
    public static HediffDef FH_HelaShardComa;
    public static MutantDef FH_HelaSubhuman;
    public static HediffDef FH_MeldGrowth;
    public static HediffDef FH_Hediff_Upgrade_Reactivation;
    public static HediffDef FH_Hediff_Upgrade_Agility;
    public static HediffDef FH_Hediff_Upgrade_BoneSpikePenetration;
    public static HediffDef FH_Hediff_Upgrade_ParasiticSpace;
    public static HediffDef FH_Hediff_Upgrade_NestMasterCarapace;
    public static HediffDef FH_Hediff_Upgrade_FastHealing;
    public static HediffDef FH_Hediff_Upgrade_FleshbeastTaming;
    public static HediffDef FH_Hediff_Upgrade_Robust;

    public static AbilityDef FH_FleshSpread;
    public static AbilityDef FH_SpikeLaunch_Fingerspike;
    public static AbilityDef FH_SpikeLaunch_Toughspike;
    public static AbilityDef FH_SpikeLaunch_Whipspike;
    public static AbilityDef FH_SpikeLaunch_Paraspike;
    public static AbilityDef FH_SpikeLaunch_Shatterspike;
    public static AbilityDef FH_SpikeLaunch_Synbulb;

    public static JobDef FH_FishAnimal;

    public static HairDef FH_Hair_Victoria;

    public static PawnKindDef FH_Whipspike;
    public static PawnKindDef FH_Gutspike;
    public static PawnKindDef FH_Paraspike;
    public static PawnKindDef FH_Shatterspike;
    public static PawnKindDef FH_Puffspike;
    public static PawnKindDef FH_Fingerspike;
    public static PawnKindDef FH_Toughspike;
    public static PawnKindDef FH_Shieldspike;
    public static PawnKindDef FH_Trispike;
    public static PawnKindDef FH_Fleshwind;
    public static PawnKindDef FH_Bulbfreak;
    public static PawnKindDef FH_Acidbulb;
    public static PawnKindDef FH_Spitbulb;
    public static PawnKindDef FH_Nexusmeld;
    public static PawnKindDef FH_Furiousmeld;
    public static PawnKindDef FH_Bastionmeld;
    public static PawnKindDef FH_Fissionmeld;
    public static PawnKindDef FH_Fleshtitan;
    public static PawnKindDef FH_Dreadmeld;

    public static ThingDef FH_FleshParasiteVat;
    public static ThingDef FH_FleshSack;
    public static ThingDef FH_FleshBlock;
    public static ThingDef FH_ChitinFleshBlock;
    public static ThingDef FH_FleshBarricade;
    public static ThingDef FH_ChitinFleshBarricade;
    public static ThingDef FH_FleshHopper;
    public static ThingDef FH_FleshBox;
    public static ThingDef FH_NerveFlesh;
    public static ThingDef FH_FleshCarapace;
    public static ThingDef FH_FleshTree;
    public static ThingDef FH_FleshBush;
    public static ThingDef FH_FleshBerry;
    public static ThingDef FH_FleshAdaptationModule;
    public static ThingDef FH_FleshSymbiosisModule;
    public static ThingDef FH_FleshSymbiosisSerum;
    public static ThingDef FH_DreadmeldSeed;
    public static ThingDef FH_SynbulbSmoke;
    public static ThingDef FH_FleshHive;
    public static ThingDef FH_FleshPrimaryNest;
    public static ThingDef FH_FleshDigester;
    public static ThingDef FH_Bullet_Shell_AcidSpit;
    public static ThingDef FH_Spike_Fingerspike;
    public static ThingDef FH_Spike_Toughspike;
    public static ThingDef FH_Spike_Whipspike;
    public static ThingDef FH_Projectile_Spike_Paraspike;
    public static ThingDef FH_Spike_Shatterspike;
    public static ThingDef FH_FuriousmeldPitBurrowSpawner;
    public static ThingDef FH_FuriousmeldPitBurrow;
    public static ThingDef FH_GiantFleshbeastPitBurrowSpawner;
    public static ThingDef FH_GiantFleshbeastPitBurrow;
    public static ThingDef Meat_Twisted;

    public static TerrainDef FH_FleshCarapaceFloor;

    public static DutyDef FH_FuriousmeldEscort;
    public static DutyDef FH_FuriousmeldSapper;
    public static DutyDef FH_FleshtitanSapper;
    public static DutyDef FH_GroupHuntGather;
    public static DutyDef FH_GroupHuntExecute;
    public static DutyDef FH_Attack_Ranged;
    public static DutyDef FH_Attack_RangedDistant;
    public static DutyDef FH_Defend_Ranged;

    public static EffecterDef FH_Effect_TitanDevastatingStrikeLightning;

    public static HiveResourceDef FH_Resource_Nutrition;
    public static HiveResourceDef FH_Resource_TwistedFlesh;

    public static FleshHiveUpgradeDef FH_Upgrade_FleshExpansion1;
    public static FleshHiveUpgradeDef FH_Upgrade_FleshExpansion2;
    public static FleshHiveUpgradeDef FH_Upgrade_NutritionAbsorption;
    public static FleshHiveUpgradeDef FH_Upgrade_SelfRepair1;
    public static FleshHiveUpgradeDef FH_Upgrade_SelfRepair2;
    public static FleshHiveUpgradeDef FH_Upgrade_CellDivision;
    public static FleshHiveUpgradeDef FH_Upgrade_Reactivation;
    public static FleshHiveUpgradeDef FH_Upgrade_Agility;
    public static FleshHiveUpgradeDef FH_Upgrade_BoneSpikePenetration;
    public static FleshHiveUpgradeDef FH_Upgrade_GiantFleshExpansion;
    public static FleshHiveUpgradeDef FH_Upgrade_NestMasterCarapace;
    public static FleshHiveUpgradeDef FH_Upgrade_NestTaming1;
    public static FleshHiveUpgradeDef FH_Upgrade_NestTaming2;
    public static FleshHiveUpgradeDef FH_Upgrade_FastHealing;
    public static FleshHiveUpgradeDef FH_Upgrade_FleshbeastTaming1;
    public static FleshHiveUpgradeDef FH_Upgrade_FleshbeastTaming2;
    public static FleshHiveUpgradeDef FH_Upgrade_FleshShaping1;
    public static FleshHiveUpgradeDef FH_Upgrade_FleshShaping2;
    public static FleshHiveUpgradeDef FH_Upgrade_Robust;

    public static HiveBuildingDef FH_Building_FleshBlock;
    public static HiveBuildingDef FH_Building_ChitinFleshBlock;
    public static HiveBuildingDef FH_Building_FleshBarricade;
    public static HiveBuildingDef FH_Building_ChitinFleshBarricade;
    public static HiveBuildingDef FH_Building_FleshHopper;
    public static HiveBuildingDef FH_Building_FleshBox;
    public static HiveBuildingDef FH_Building_FleshParasiteVat;
    public static HiveBuildingDef FH_Building_FleshDigester;

    public static ResearchProjectDef FH_Research_BasicFleshHive;
    public static ResearchProjectDef FH_Research_NutritionCycle;
    public static ResearchProjectDef FH_Research_ImmuneSystem;
    public static ResearchProjectDef FH_Research_FleshFurniture;
    public static ResearchProjectDef FH_Research_FleshbeastCoexistence;
    public static ResearchProjectDef FH_Research_ComplexFleshHive;
    public static ResearchProjectDef FH_Research_FleshFusion;
    public static ResearchProjectDef FH_Research_FleshReplica;
    public static ResearchProjectDef FH_Research_BonePlatingFleshBlock;
    public static ResearchProjectDef FH_Research_ComplexImmuneSystem;
    public static ResearchProjectDef FH_Research_FleshPrimaryHive;
    public static ResearchProjectDef FH_Research_ComplexFleshFusion;
    public static ResearchProjectDef FH_Research_MotherBeastControl;
    public static ResearchTabDef FH_ResearchTab_FleshHive; 

    public static QuestScriptDef FH_Quest_FleshSurvivor;

    public static ThoughtDef FH_Thought_FleshParasitism;
    public static ThoughtDef FH_Thought_FleshNutrition;

    public static StatDef FH_Stat_ParasitismCapacity;
    public static JobDef FH_Job_PutPawnInParasitePod;
    public static JobDef FH_Job_EnterParasitePod;
    public static JobDef FH_Job_FillTrispikeCharge;
    public static JobDef FH_Job_RefillTwistedFlesh;
    public static JobDef FH_Job_RefillTwistedFlesh_Help;
    public static JobDef FH_Job_FillFleshSack;
    public static JobDef FH_Job_InfectHarbingerTree;
    public static JobDef FH_Job_SuppressFleshHiveActivity;
    public static JobDef FH_Job_MountParasiticWeapon;
    public static JobDef FH_Job_UseShardOnHela;
    public static JobDef FH_Job_ConsumeMeldSeed;
    public static JobDef FH_Job_HuntExecution;

    public static DesignationDef FH_MarkPrey;
    public static DesignationDef FH_InfectHarbingerTree;
}
