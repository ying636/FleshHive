using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public static class FleshSurvivorHelaGenerator
{
    public static Pawn Generate(Map map, PawnGenerationContext context = PawnGenerationContext.NonPlayer,
        Faction? faction = null, bool includeDreadmeldSeed = true)
    {
        PawnGenerationRequest request = new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            faction,
            context,
            map.Tile,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            allowPregnant: false,
            allowFood: false,
            allowAddictions: false,
            fixedBiologicalAge: 32f,
            fixedChronologicalAge: 32f,
            fixedGender: Gender.Female,
            forceRecruitable: true,
            dontGiveWeapon: true,
            maximumAgeTraits: 2,
            minimumAgeTraits: 2,
            forceNoGear: true);
        request.ForcedMutant = FleshHiveDefOf.FH_HelaSubhuman;
        Pawn pawn = PawnGenerator.GeneratePawn(request);

        Configure(pawn, includeDreadmeldSeed);
        return pawn;
    }

    public static void Configure(Pawn pawn, bool includeDreadmeldSeed = true)
    {
        pawn.gender = Gender.Female;

        string firstName = "FH_Hela_FirstName".Translate();
        string lastName = "FH_Hela_LastName".Translate();
        pawn.Name = new NameTriple(firstName, firstName, lastName);
        pawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamed("CaravanChild53");
        pawn.story.Adulthood = DefDatabase<BackstoryDef>.GetNamed("NeuroScientist19");
        pawn.story.bodyType = BodyTypeDefOf.Female;
        pawn.story.headType = DefDatabase<HeadTypeDef>.GetNamed("Female_NarrowNormal");
        pawn.story.hairDef = FleshHiveDefOf.FH_Hair_Victoria;
        pawn.story.HairColor = new Color(69f / 255f, 49f / 255f, 30f / 255f);
        pawn.story.skinColorOverride = PawnSkinColors.GetSkinColor(0f);
        pawn.style.FaceTattoo = TattooDefOf.NoTattoo_Face;

        pawn.story.traits.allTraits.Clear();
        pawn.story.traits.GainTrait(new Trait(DefDatabase<TraitDef>.GetNamed("Nerves"), 1));
        pawn.story.traits.GainTrait(new Trait(DefDatabase<TraitDef>.GetNamed("FastLearner")));

        SetSkill(pawn, "Shooting", 5, Passion.Minor);
        SetSkill(pawn, "Melee", 0, Passion.None);
        SetSkill(pawn, "Construction", 2, Passion.None);
        SetSkill(pawn, "Mining", 3, Passion.Minor);
        SetSkill(pawn, "Cooking", 0, Passion.None);
        SetSkill(pawn, "Plants", 4, Passion.Minor);
        SetSkill(pawn, "Animals", 1, Passion.None);
        SetSkill(pawn, "Crafting", 0, Passion.None);
        SetSkill(pawn, "Artistic", 2, Passion.None);
        SetSkill(pawn, "Medicine", 6, Passion.None);
        SetSkill(pawn, "Social", 7, Passion.Minor);
        SetSkill(pawn, "Intellectual", 15, Passion.Major);

        AddApparel(pawn, "Apparel_CollarShirt", "Synthread", QualityCategory.Excellent);
        AddApparel(pawn, "Apparel_FlakVest", null, QualityCategory.Excellent);
        AddApparel(pawn, "Apparel_LabCoat", "Synthread", QualityCategory.Masterwork);
        AddApparel(pawn, "Apparel_FlakPants", null, QualityCategory.Good);

        Thing meals = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
        meals.stackCount = 3;
        pawn.inventory.innerContainer.TryAdd(meals);

        if (includeDreadmeldSeed)
        {
            Thing dreadmeldSeed = ThingMaker.MakeThing(FleshHiveDefOf.FH_DreadmeldSeed);
            pawn.inventory.innerContainer.TryAdd(dreadmeldSeed);
        }

        AddInjury(pawn, HediffDefOf.Cut, BodyPartDefOf.Head, false, 4f);
        HediffDef scratch = DefDatabase<HediffDef>.GetNamed("Scratch");
        AddInjury(pawn, scratch, BodyPartDefOf.Torso, false, 2f);
        AddInjury(pawn, scratch, BodyPartDefOf.Shoulder, true, 2f);
        AddInjury(pawn, scratch, BodyPartDefOf.Leg, true, 2f);

        ParasitismSystem system = (ParasitismSystem)pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
        Pawn parasite = PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Whipspike, Faction.OfPlayer);
        if (!system.Parasite(parasite) && !parasite.Destroyed)
        {
            parasite.Destroy();
        }

        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }

    private static void SetSkill(Pawn pawn, string defName, int level, Passion passion)
    {
        SkillRecord skill = pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed(defName));
        skill.Level = level;
        skill.passion = passion;
        skill.xpSinceLastLevel = 0f;
    }

    private static void AddApparel(Pawn pawn, string defName, string? stuffDefName, QualityCategory quality)
    {
        ThingDef? apparelDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        if (apparelDef == null)
        {
            return;
        }

        ThingDef? stuff = stuffDefName.NullOrEmpty() ? null : DefDatabase<ThingDef>.GetNamed(stuffDefName);
        Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);
        apparel.TryGetComp<CompQuality>()?.SetQuality(quality, null);
        pawn.apparel.Wear(apparel, false);
    }

    private static void AddInjury(Pawn pawn, HediffDef injuryDef, BodyPartDef partDef, bool leftSide, float severity)
    {
        BodyPartRecord part = pawn.health.hediffSet.GetNotMissingParts()
            .First(record => record.def == partDef && (!leftSide || record.flipGraphic));
        Hediff injury = pawn.health.AddHediff(injuryDef, part);
        injury.Severity = severity;
    }
}
