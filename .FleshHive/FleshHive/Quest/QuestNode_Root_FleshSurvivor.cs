using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace FleshHive;

public class QuestNode_Root_FleshSurvivor : QuestNode
{
    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        Map map = slate.Get<Map>("map") ?? QuestGen_Get.GetMap();
        slate.Set("map", map);
        Pawn hela = CreateHela(map);
        List<Pawn> pursuers = CreatePursuers();

        slate.Set("hela", hela);
        slate.Set("resolvedQuestName", "FH_Quest_FleshSurvivor_Name".Translate().ToString());
        slate.Set("resolvedQuestDescription", "FH_Quest_FleshSurvivor_Description".Translate().ToString());

        QuestGen.AddToGeneratedPawns(hela);
        Find.WorldPawns.PassToWorld(hela);
        foreach (Pawn pursuer in pursuers)
        {
            QuestGen.AddToGeneratedPawns(pursuer);
            Find.WorldPawns.PassToWorld(pursuer);
        }

        quest.PawnsArrive(Gen.YieldSingle(hela), mapParent: map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
            joinPlayer: true, sendStandardLetter: false);
        quest.Delay(AttackDelayTicks, delegate
        {
            quest.PawnsArrive(pursuers, mapParent: map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
                sendStandardLetter: false);
            quest.AssaultColony(Faction.OfEntities, map.Parent, pursuers);
            quest.Letter(LetterDefOf.ThreatBig, lookTargets: pursuers,
                text: "FH_Quest_FleshSurvivor_AttackText".Translate().ToString(),
                label: "FH_Quest_FleshSurvivor_AttackLabel".Translate().ToString());
            QuestGen_End.End(quest, QuestEndOutcome.Success);
        }, inspectStringTargets: new ISelectable[] { hela },
            inspectString: "FH_Quest_FleshSurvivor_AttackIn".Translate().ToString(),
            expiryInfoPart: "FH_Quest_FleshSurvivor_AttackIn".Translate().ToString(),
            debugLabel: "FleshSurvivorAttack");
    }

    protected override bool TestRunInt(Slate slate)
    {
        return (slate.Get<Map>("map") ?? QuestGen_Get.GetMap()) != null && Faction.OfEntities != null;
    }

    private Pawn CreateHela(Map map)
    {
        PawnGenerationRequest request = new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            null,
            PawnGenerationContext.NonPlayer,
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
        Pawn pawn = PawnGenerator.GeneratePawn(request);

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

        Thing dreadmeldSeed = ThingMaker.MakeThing(FleshHiveDefOf.FH_DreadmeldSeed);
        pawn.inventory.innerContainer.TryAdd(dreadmeldSeed);

        AddInjury(pawn, HediffDefOf.Cut, BodyPartDefOf.Head, false, 4f);
        HediffDef scratch = DefDatabase<HediffDef>.GetNamed("Scratch");
        AddInjury(pawn, scratch, BodyPartDefOf.Torso, false, 2f);
        AddInjury(pawn, scratch, BodyPartDefOf.Shoulder, true, 2f);
        AddInjury(pawn, scratch, BodyPartDefOf.Leg, true, 2f);

        pawn.health.AddHediff(FleshHiveDefOf.FH_Hela);
        ParasitismSystem system = (ParasitismSystem)pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
        Pawn parasite = PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Whipspike, Faction.OfPlayer);
        if (!system.Parasite(parasite) && !parasite.Destroyed)
        {
            parasite.Destroy();
        }

        pawn.Drawer.renderer.SetAllGraphicsDirty();
        return pawn;
    }

    private List<Pawn> CreatePursuers()
    {
        List<PawnKindDef> smallKinds = new List<PawnKindDef>
        {
            FleshHiveDefOf.FH_Fingerspike,
            FleshHiveDefOf.FH_Puffspike,
            FleshHiveDefOf.FH_Whipspike,
            FleshHiveDefOf.FH_Gutspike,
            FleshHiveDefOf.FH_Paraspike
        };
        List<PawnKindDef> mediumKinds = new List<PawnKindDef>
        {
            FleshHiveDefOf.FH_Shatterspike,
            FleshHiveDefOf.FH_Toughspike
        };
        int count = Rand.RangeInclusive(4, 6);
        List<Pawn> pursuers = new List<Pawn>(count)
        {
            PawnGenerator.GeneratePawn(mediumKinds.RandomElement(), Faction.OfEntities)
        };
        for (int i = 1; i < count; i++)
        {
            pursuers.Add(PawnGenerator.GeneratePawn(smallKinds.RandomElement(), Faction.OfEntities));
        }
        return pursuers;
    }

    private void SetSkill(Pawn pawn, string defName, int level, Passion passion)
    {
        SkillRecord skill = pawn.skills.GetSkill(DefDatabase<SkillDef>.GetNamed(defName));
        skill.Level = level;
        skill.passion = passion;
        skill.xpSinceLastLevel = 0f;
    }

    private void AddApparel(Pawn pawn, string defName, string stuffDefName, QualityCategory quality)
    {
        ThingDef apparelDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
        if (apparelDef == null)
        {
            return;
        }

        ThingDef stuff = stuffDefName.NullOrEmpty() ? null : DefDatabase<ThingDef>.GetNamed(stuffDefName);
        Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);
        apparel.TryGetComp<CompQuality>()?.SetQuality(quality, null);
        pawn.apparel.Wear(apparel, false);
    }

    private void AddInjury(Pawn pawn, HediffDef injuryDef, BodyPartDef partDef, bool leftSide, float severity)
    {
        BodyPartRecord part = pawn.health.hediffSet.GetNotMissingParts()
            .First(record => record.def == partDef && (!leftSide || record.flipGraphic));
        Hediff injury = pawn.health.AddHediff(injuryDef, part);
        injury.Severity = severity;
    }

    private const int AttackDelayTicks = 2500;
}
