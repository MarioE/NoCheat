using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using NoCheat.ItemSpawning.Accounting;
using Terraria.ID;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents an item conversion. An instance of this describes the possible conversion of, e.g., a treasure bag into
    ///     its loot components.
    /// </summary>
    public abstract class ItemConversion
    {
        /// <summary>
        ///     The possible developer sets obtainable from a hardmode treasure bag.
        /// </summary>
        private static readonly ItemConversion DeveloperSets = Either(
            All(Of(ItemID.RedsHelmet), Of(ItemID.RedsBreastplate), Of(ItemID.RedsLeggings), Of(ItemID.RedsWings),
                Of(ItemID.RedsYoyo)),
            All(Of(ItemID.CenxsTiara), Of(ItemID.CenxsBreastplate), Of(ItemID.CenxsLeggings), Of(ItemID.CenxsWings)),
            All(Of(ItemID.CenxsDress), Of(ItemID.CenxsDressPants), Of(ItemID.CenxsWings)),
            All(Of(ItemID.CrownosMask), Of(ItemID.CrownosBreastplate), Of(ItemID.CrownosLeggings),
                Of(ItemID.CrownosWings)),
            All(Of(ItemID.WillsHelmet), Of(ItemID.WillsBreastplate), Of(ItemID.WillsLeggings), Of(ItemID.WillsWings)),
            All(Of(ItemID.JimsHelmet), Of(ItemID.JimsBreastplate), Of(ItemID.JimsLeggings), Of(ItemID.JimsWings)),
            All(Of(ItemID.AaronsHelmet), Of(ItemID.AaronsBreastplate), Of(ItemID.AaronsLeggings)),
            All(Of(ItemID.DTownsHelmet), Of(ItemID.DTownsBreastplate), Of(ItemID.DTownsLeggings),
                Of(ItemID.DTownsWings)),
            All(Of(ItemID.BejeweledValkyrieHead), Of(ItemID.BejeweledValkyrieBody), Of(ItemID.BejeweledValkyrieWing),
                Of(ItemID.ValkyrieYoyo)),
            All(Of(ItemID.Yoraiz0rHead), Of(ItemID.Yoraiz0rShirt), Of(ItemID.Yoraiz0rPants), Of(ItemID.Yoraiz0rWings),
                Of(ItemID.Yoraiz0rDarkness)),
            All(Of(ItemID.SkiphsHelm), Of(ItemID.SkiphsShirt), Of(ItemID.SkiphsPants), Of(ItemID.SkiphsWings),
                Of(ItemID.DevDye, 4, 4)),
            All(Of(ItemID.LokisHelm), Of(ItemID.LokisShirt), Of(ItemID.LokisPants), Of(ItemID.LokisWings)),
            All(Of(ItemID.ArkhalisHat), Of(ItemID.ArkhalisShirt), Of(ItemID.ArkhalisPants), Of(ItemID.ArkhalisWings)),
            All(Of(ItemID.LeinforsHat), Of(ItemID.LeinforsShirt), Of(ItemID.LeinforsPants), Of(ItemID.LeinforsWings),
                Of(ItemID.LeinforsAccessory, 4, 4)));

        /// <summary>
        ///     The possible drops from an herb bag.
        /// </summary>
        private static readonly ItemConversion HerbBagDrops = Either(
            // Herbs
            Of(ItemID.Daybloom, 2, 8), Of(ItemID.Moonglow, 2, 8), Of(ItemID.Blinkroot, 2, 8),
            Of(ItemID.Deathweed, 2, 8), Of(ItemID.Waterleaf, 2, 8), Of(ItemID.Fireblossom, 2, 8),
            Of(ItemID.Shiverthorn, 2, 8),

            // Herb seeds
            Of(ItemID.DaybloomSeeds, 2, 8), Of(ItemID.MoonglowSeeds, 2, 8), Of(ItemID.BlinkrootSeeds, 2, 8),
            Of(ItemID.DeathweedSeeds, 2, 8), Of(ItemID.WaterleafSeeds, 2, 8), Of(ItemID.FireblossomSeeds, 2, 8),
            Of(ItemID.ShiverthornSeeds, 2, 8));

        /// <summary>
        ///     The drops shared by all biome crates.
        /// </summary>
        private static readonly ItemConversion SharedBiomeCrateDrops = All(
            Maybe(Either(Of(ItemID.IronBar, 10, 20), Of(ItemID.LeadBar, 10, 20), Of(ItemID.SilverBar, 10, 20),
                         Of(ItemID.TungstenBar, 10, 20), Of(ItemID.GoldBar, 10, 20), Of(ItemID.PlatinumBar, 10, 20))),
            Maybe(Either(Of(ItemID.CobaltBar, 8, 20), Of(ItemID.MythrilBar, 8, 20), Of(ItemID.AdamantiteBar, 8, 20),
                         Of(ItemID.PalladiumBar, 8, 20), Of(ItemID.OrichalcumBar, 8, 20),
                         Of(ItemID.TitaniumBar, 8, 20))),
            Maybe(Either(Of(ItemID.ObsidianSkinPotion, 2, 4), Of(ItemID.SpelunkerPotion, 2, 4),
                         Of(ItemID.HunterPotion, 2, 4), Of(ItemID.GravitationPotion, 2, 4),
                         Of(ItemID.MiningPotion, 2, 4), Of(ItemID.HeartreachPotion, 2, 4))),
            Maybe(Either(Of(ItemID.LesserHealingPotion, 5, 17), Of(ItemID.LesserManaPotion, 5, 17))),
            Maybe(Either(Of(ItemID.JourneymanBait, 1, 4), Of(ItemID.MasterBait, 1, 4))),
            Maybe(Of(ItemID.CopperCoin, 1, 120000)));

        /// <summary>
        ///     The rewards available from the angler.
        /// </summary>
        public static readonly ItemConversion AnglerRewards = All(
            Either(Of(ItemID.FuzzyCarrot), Of(ItemID.AnglerHat), Of(ItemID.AnglerVest), Of(ItemID.AnglerPants),
                   Of(ItemID.GoldenFishingRod), Of(ItemID.GoldenBugNet), Of(ItemID.FishHook),
                   Of(ItemID.HighTestFishingLine), Of(ItemID.AnglerEarring), Of(ItemID.TackleBox),
                   Of(ItemID.FishermansGuide), Of(ItemID.WeatherRadio), Of(ItemID.Sextant), Of(ItemID.FinWings),
                   Of(ItemID.BottomlessBucket), Of(ItemID.SuperAbsorbantSponge), Of(ItemID.HotlineFishingHook),
                   Of(ItemID.SonarPotion, 2, 5), Of(ItemID.FishingPotion, 2, 5), Of(ItemID.CratePotion, 2, 5),
                   Of(ItemID.CoralstoneBlock, 50, 150), Of(ItemID.BunnyfishTrophy), Of(ItemID.GoldfishTrophy),
                   Of(ItemID.SharkteethTrophy), Of(ItemID.SwordfishTrophy), Of(ItemID.TreasureMap),
                   Of(ItemID.SeaweedPlanter), Of(ItemID.PillaginMePixels), Of(ItemID.CompassRose),
                   Of(ItemID.ShipsWheel),
                   Of(ItemID.LifePreserver), Of(ItemID.WallAnchor), Of(ItemID.ShipInABottle),
                   All(Of(ItemID.SeashellHairpin), Of(ItemID.MermaidAdornment), Of(ItemID.MermaidTail)),
                   All(Of(ItemID.FishCostumeMask), Of(ItemID.FishCostumeShirt), Of(ItemID.FishCostumeFinskirt))),
            Of(ItemID.CopperCoin, 1, 100000),
            Maybe(Either(Of(ItemID.ApprenticeBait, 1, 7), Of(ItemID.JourneymanBait, 1, 7),
                         Of(ItemID.MasterBait, 1, 7))));

        /// <summary>
        ///     The drops available from extractinators.
        /// </summary>
        public static readonly ItemConversion ExtractinatorDrops = Either(
            Of(ItemID.AmberMosquito),

            // Gems
            Of(ItemID.Diamond, 1, 16), Of(ItemID.Ruby, 1, 16), Of(ItemID.Emerald, 1, 16), Of(ItemID.Sapphire, 1, 16),
            Of(ItemID.Topaz, 1, 16), Of(ItemID.Amethyst, 1, 16), Of(ItemID.Amber, 1, 16),

            // Ores
            Of(ItemID.FossilOre, 1, 7), Of(ItemID.CopperOre, 1, 16), Of(ItemID.IronOre, 1, 16),
            Of(ItemID.SilverOre, 1, 16), Of(ItemID.GoldOre, 1, 16), Of(ItemID.TinOre, 1, 16), Of(ItemID.LeadOre, 1, 16),
            Of(ItemID.TungstenOre, 1, 16), Of(ItemID.PlatinumOre, 1, 16),

            // Coins
            Of(ItemID.CopperCoin, 1, 11_00_00_00), Of(ItemID.CopperCoin, 1, 1_00_00_00),
            Of(ItemID.CopperCoin, 1, 1_00_00), Of(ItemID.CopperCoin, 1, 1_00));

        /// <summary>
        ///     The mapping from item IDs to conversions.
        /// </summary>
        public static readonly IDictionary<int, ItemConversion> ItemIdToConversion = new Dictionary<int, ItemConversion>
        {
            [ItemID.EmptyBucket] = Either(
                Of(ItemID.WaterBucket),
                Of(ItemID.LavaBucket),
                Of(ItemID.HoneyBucket)),

            [ItemID.WaterBucket] = Of(ItemID.EmptyBucket),
            [ItemID.LavaBucket] = Of(ItemID.EmptyBucket),
            [ItemID.HoneyBucket] = Of(ItemID.EmptyBucket),

            [ItemID.GoodieBag] = Either(
                Of(ItemID.UnluckyYarn),
                Of(ItemID.BatHook),
                Of(ItemID.RottenEgg, 10, 40),
                Of(ItemID.BitterHarvest),
                Of(ItemID.BloodMoonCountess),
                Of(ItemID.HallowsEve),
                Of(ItemID.JackingSkeletron),
                Of(ItemID.MorbidCuriosity),
                Of(ItemID.CatEars),
                All(Of(ItemID.CreeperMask), Of(ItemID.CreeperShirt), Of(ItemID.CreeperPants)),
                All(Of(ItemID.PumpkinMask), Of(ItemID.PumpkinShirt), Of(ItemID.PumpkinPants)),
                All(Of(ItemID.SpaceCreatureMask), Of(ItemID.SpaceCreatureShirt), Of(ItemID.SpaceCreaturePants)),
                All(Of(ItemID.CatMask), Of(ItemID.CatShirt), Of(ItemID.CatPants)),
                All(Of(ItemID.KarateTortoiseMask), Of(ItemID.KarateTortoiseShirt), Of(ItemID.KarateTortoisePants)),
                All(Of(ItemID.FoxMask), Of(ItemID.FoxShirt), Of(ItemID.FoxPants)),
                All(Of(ItemID.WitchHat), Of(ItemID.WitchDress), Of(ItemID.WitchBoots)),
                All(Of(ItemID.VampireMask), Of(ItemID.VampireShirt), Of(ItemID.VampirePants)),
                All(Of(ItemID.LeprechaunHat), Of(ItemID.LeprechaunShirt), Of(ItemID.LeprechaunPants)),
                All(Of(ItemID.RobotMask), Of(ItemID.RobotShirt), Of(ItemID.RobotPants)),
                All(Of(ItemID.PrincessHat), Of(ItemID.PrincessDressNew)),
                All(Of(ItemID.TreasureHunterShirt), Of(ItemID.TreasureHunterPants)),
                All(Of(ItemID.WolfMask), Of(ItemID.WolfShirt), Of(ItemID.WolfPants)),
                All(Of(ItemID.UnicornMask), Of(ItemID.UnicornShirt), Of(ItemID.UnicornPants)),
                All(Of(ItemID.ReaperHood), Of(ItemID.ReaperRobe)),
                All(Of(ItemID.PixieShirt), Of(ItemID.PixiePants)),
                All(Of(ItemID.BrideofFrankensteinMask), Of(ItemID.BrideofFrankensteinDress)),
                All(Of(ItemID.GhostMask), Of(ItemID.GhostShirt))),

            [ItemID.Present] = Either(
                Of(ItemID.Coal),
                Of(ItemID.DogWhistle),
                All(Of(ItemID.RedRyder), Of(ItemID.MusketBall, 30, 60)),
                Of(ItemID.CandyCaneSword),
                Of(ItemID.CnadyCanePickaxe),
                Of(ItemID.CandyCaneHook),
                Of(ItemID.FruitcakeChakram),
                Of(ItemID.HandWarmer),
                Of(ItemID.Toolbox),
                Of(ItemID.ReindeerAntlers),
                Of(ItemID.Holly),
                All(Of(ItemID.MrsClauseHat), Of(ItemID.MrsClauseShirt), Of(ItemID.MrsClauseHeels)),
                All(Of(ItemID.ParkaHood), Of(ItemID.ParkaCoat), Of(ItemID.ParkaPants)),
                All(Of(ItemID.TreeMask), Of(ItemID.TreeShirt), Of(ItemID.TreeTrunks)),
                Of(ItemID.SnowHat),
                Of(ItemID.UglySweater),
                Of(ItemID.ChristmasPudding),
                Of(ItemID.SugarCookie),
                Of(ItemID.GingerbreadCookie),
                Of(ItemID.Eggnog, 1, 3),
                Of(ItemID.StarAnise, 20, 40),
                Of(ItemID.PineTreeBlock, 20, 49),
                Of(ItemID.CandyCaneBlock, 20, 49),
                Of(ItemID.GreenCandyCaneBlock, 20, 49),
                Of(ItemID.SnowGlobe)),

            [ItemID.WoodenCrate] = All(
                Maybe(Of(ItemID.Sundial)),
                Maybe(Of(ItemID.SailfishBoots)),
                Maybe(Of(ItemID.TsunamiInABottle)),
                Maybe(Of(ItemID.Anchor)),
                Maybe(Either(Of(ItemID.Aglet), Of(ItemID.Umbrella), Of(ItemID.ClimbingClaws), Of(ItemID.CordageGuide),
                             Of(ItemID.Radar))),
                Maybe(Either(Of(ItemID.CopperOre, 8, 20), Of(ItemID.IronOre, 8, 20), Of(ItemID.SilverOre, 8, 20),
                             Of(ItemID.GoldOre, 8, 20), Of(ItemID.TinOre, 8, 20), Of(ItemID.LeadOre, 8, 20),
                             Of(ItemID.TungstenOre, 8, 20), Of(ItemID.PlatinumOre, 8, 20))),
                Maybe(Either(Of(ItemID.CobaltOre, 8, 20), Of(ItemID.MythrilOre, 8, 20), Of(ItemID.AdamantiteOre, 8, 20),
                             Of(ItemID.PalladiumOre, 8, 20), Of(ItemID.OrichalcumOre, 8, 20),
                             Of(ItemID.TitaniumOre, 8, 20))),
                Maybe(Either(Of(ItemID.CopperBar, 8, 20), Of(ItemID.IronBar, 8, 20), Of(ItemID.SilverBar, 8, 20),
                             Of(ItemID.GoldBar, 8, 20), Of(ItemID.TinBar, 8, 20), Of(ItemID.LeadBar, 8, 20),
                             Of(ItemID.TungstenBar, 8, 20), Of(ItemID.PlatinumBar, 8, 20))),
                Maybe(Either(Of(ItemID.CobaltBar, 1, 7), Of(ItemID.MythrilBar, 1, 7), Of(ItemID.AdamantiteBar, 1, 7),
                             Of(ItemID.PalladiumBar, 1, 7), Of(ItemID.OrichalcumBar, 1, 7),
                             Of(ItemID.TitaniumBar, 1, 7))),
                Maybe(Either(Of(ItemID.ObsidianSkinPotion, 1, 3), Of(ItemID.SwiftnessPotion, 1, 3),
                             Of(ItemID.IronskinPotion, 1, 3), Of(ItemID.NightOwlPotion, 1, 3),
                             Of(ItemID.ShinePotion, 1, 3), Of(ItemID.HunterPotion, 1, 3),
                             Of(ItemID.GillsPotion, 1, 3), Of(ItemID.MiningPotion, 1, 3),
                             Of(ItemID.HeartreachPotion, 1, 3), Of(ItemID.TrapsightPotion, 1, 3))),
                Maybe(Either(Of(ItemID.LesserHealingPotion, 5, 15), Of(ItemID.LesserManaPotion, 5, 15))),
                Maybe(Either(Of(ItemID.ApprenticeBait, 1, 4), Of(ItemID.JourneymanBait, 1, 4))),
                Maybe(Of(ItemID.CopperCoin, 1, 5_90_00))),

            [ItemID.IronCrate] = All(
                Maybe(Of(ItemID.Sundial)),
                Maybe(Of(ItemID.SailfishBoots)),
                Maybe(Of(ItemID.TsunamiInABottle)),
                Maybe(Of(ItemID.GingerBeard)),
                Maybe(Of(ItemID.TartarSauce)),
                Maybe(Of(ItemID.FalconBlade)),
                Maybe(Either(Of(ItemID.CopperBar, 6, 14), Of(ItemID.IronBar, 6, 14), Of(ItemID.SilverBar, 6, 14),
                             Of(ItemID.GoldBar, 6, 14), Of(ItemID.TinBar, 6, 14), Of(ItemID.LeadBar, 6, 14),
                             Of(ItemID.TungstenBar, 6, 14), Of(ItemID.PlatinumBar, 6, 14))),
                Maybe(Either(Of(ItemID.CobaltBar, 5, 14), Of(ItemID.MythrilBar, 5, 14), Of(ItemID.AdamantiteBar, 5, 14),
                             Of(ItemID.PalladiumBar, 5, 14), Of(ItemID.OrichalcumBar, 5, 14),
                             Of(ItemID.TitaniumBar, 5, 14))),
                Maybe(Either(Of(ItemID.ObsidianSkinPotion, 1, 3), Of(ItemID.SpelunkerPotion, 1, 3),
                             Of(ItemID.HunterPotion, 1, 3), Of(ItemID.GravitationPotion, 1, 3),
                             Of(ItemID.MiningPotion, 1, 3), Of(ItemID.HeartreachPotion, 1, 3),
                             Of(ItemID.CalmingPotion, 1, 3), Of(ItemID.FlipperPotion, 1, 3))),
                Maybe(Either(Of(ItemID.HealingPotion, 5, 15), Of(ItemID.ManaPotion, 5, 15))),
                Maybe(Either(Of(ItemID.JourneymanBait, 1, 4), Of(ItemID.MasterBait, 1, 4))),
                Maybe(Of(ItemID.CopperCoin, 1, 10_00_00))),

            [ItemID.GoldenCrate] = All(
                Maybe(Of(ItemID.Sundial)),
                Maybe(Of(ItemID.HardySaddle)),
                Maybe(Either(Of(ItemID.SilverBar, 15, 30), Of(ItemID.GoldBar, 15, 30), Of(ItemID.TungstenBar, 15, 30),
                             Of(ItemID.PlatinumBar, 15, 30))),
                Maybe(Either(Of(ItemID.MythrilBar, 15, 30), Of(ItemID.AdamantiteBar, 15, 30),
                             Of(ItemID.OrichalcumBar, 15, 30), Of(ItemID.TitaniumBar, 15, 30))),
                Maybe(Either(Of(ItemID.ObsidianSkinPotion, 2, 5), Of(ItemID.SpelunkerPotion, 2, 5),
                             Of(ItemID.GravitationPotion, 2, 5), Of(ItemID.MiningPotion, 2, 5),
                             Of(ItemID.HeartreachPotion, 2, 5))),
                Maybe(Either(Of(ItemID.GreaterHealingPotion, 5, 20), Of(ItemID.GreaterManaPotion, 5, 20))),
                Maybe(Of(ItemID.MasterBait, 3, 7)),
                Maybe(Of(ItemID.CopperCoin, 1, 20_00_00))),

            [ItemID.LockBox] = All(
                Either(Of(ItemID.MagicMissile), Of(ItemID.Muramasa), Of(ItemID.CobaltShield), Of(ItemID.AquaScepter),
                       Of(ItemID.BlueMoon), Of(ItemID.Handgun), Of(ItemID.ShadowKey)),
                Maybe(Either(Of(ItemID.SpelunkerPotion), Of(ItemID.EndurancePotion), Of(ItemID.GravitationPotion),
                             Of(ItemID.HeartreachPotion), Of(ItemID.IronskinPotion), Of(ItemID.MagicPowerPotion),
                             Of(ItemID.ObsidianSkinPotion), Of(ItemID.WormholePotion))),
                Maybe(Of(ItemID.HealingPotion)),
                Maybe(Of(ItemID.CopperCoin, 1, 6_99_99))),

            // Herb bags can run their drops either 3 or 4 times.
            [ItemID.HerbBag] = All(
                HerbBagDrops,
                HerbBagDrops,
                HerbBagDrops,
                Maybe(HerbBagDrops)),

            [ItemID.CorruptFishingCrate] = All(
                Maybe(Either(Of(ItemID.BallOHurt), Of(ItemID.BandofStarpower), Of(ItemID.Musket), Of(ItemID.ShadowOrb),
                             Of(ItemID.Vilethorn))),
                Maybe(Of(ItemID.SoulofNight, 2, 5)),
                Maybe(Of(ItemID.CursedFlame, 2, 5)),
                SharedBiomeCrateDrops),

            [ItemID.CrimsonFishingCrate] = All(
                Maybe(Either(Of(ItemID.TheUndertaker), Of(ItemID.TheRottedFork), Of(ItemID.CrimsonRod),
                             Of(ItemID.PanicNecklace), Of(ItemID.CrimsonHeart))),
                Maybe(Of(ItemID.SoulofNight, 2, 5)),
                Maybe(Of(ItemID.Ichor, 2, 5)),
                SharedBiomeCrateDrops),

            [ItemID.DungeonFishingCrate] = All(
                Maybe(Of(ItemID.LockBox)),
                SharedBiomeCrateDrops),

            [ItemID.FloatingIslandFishingCrate] = All(
                Maybe(Either(Of(ItemID.LuckyHorseshoe), Of(ItemID.Starfury), Of(ItemID.ShinyRedBalloon))),
                SharedBiomeCrateDrops),

            [ItemID.HallowedFishingCrate] = All(
                Maybe(Of(ItemID.SoulofLight, 2, 5)),
                Maybe(Of(ItemID.CrystalShard, 4, 10)),
                SharedBiomeCrateDrops),

            [ItemID.JungleFishingCrate] = All(
                Maybe(Either(Of(ItemID.AnkletoftheWind), Of(ItemID.Boomstick), Of(ItemID.FeralClaws),
                             Of(ItemID.StaffofRegrowth), Of(ItemID.FiberglassFishingPole))),
                SharedBiomeCrateDrops),

            [ItemID.KingSlimeBossBag] = All(
                Of(ItemID.RoyalGel),
                Either(Of(ItemID.NinjaHood), Of(ItemID.NinjaShirt), Of(ItemID.NinjaPants)),
                Either(Of(ItemID.NinjaHood), Of(ItemID.NinjaShirt), Of(ItemID.NinjaPants)),
                Either(Of(ItemID.SlimeGun), Of(ItemID.SlimeHook)),
                Of(ItemID.Solidifier),
                Of(ItemID.CopperCoin, 1, 14_41_44),
                Maybe(Of(ItemID.KingSlimeMask)),
                Maybe(Of(ItemID.SlimySaddle))),

            [ItemID.EyeOfCthulhuBossBag] = All(
                Of(ItemID.EoCShield),
                Either(All(Of(ItemID.CrimtaneOre, 30, 87), Of(ItemID.CrimsonSeeds, 1, 3)),
                       All(Of(ItemID.DemoniteOre, 30, 87), Of(ItemID.CorruptSeeds, 1, 3),
                           Of(ItemID.UnholyArrow, 20, 49))),
                Of(ItemID.CopperCoin, 1, 8_64_86),
                Maybe(Of(ItemID.EyeMask)),
                Maybe(Of(ItemID.Binoculars)),
                Maybe(Of(ItemID.AviatorSunglasses))),

            [ItemID.EaterOfWorldsBossBag] = All(
                Of(ItemID.WormScarf),
                Of(ItemID.DemoniteOre, 30, 59),
                Of(ItemID.ShadowScale, 10, 19),
                Of(ItemID.CopperCoin, 1, 8_65),
                Maybe(Of(ItemID.EaterMask)),
                Maybe(Of(ItemID.EatersBone))),

            [ItemID.BrainOfCthulhuBossBag] = All(
                Of(ItemID.BrainOfConfusion),
                Of(ItemID.CrimtaneOre, 40, 90),
                Of(ItemID.TissueSample, 10, 19),
                Of(ItemID.CopperCoin, 1, 14_41_44),
                Maybe(Of(ItemID.BrainMask)),
                Maybe(Of(ItemID.BoneRattle))),

            [ItemID.QueenBeeBossBag] = All(
                Of(ItemID.HiveBackpack),
                Either(Of(ItemID.BeeGun), Of(ItemID.BeeKeeper), Of(ItemID.BeesKnees)),
                Of(ItemID.HiveWand),
                Either(Of(ItemID.BeeHat), Of(ItemID.BeeShirt), Of(ItemID.BeePants)),
                Of(ItemID.Beenade, 10, 29),
                Of(ItemID.BeeWax, 17, 29),
                Of(ItemID.CopperCoin, 1, 28_82_88),
                Maybe(Of(ItemID.BeeMask)),
                Maybe(Of(ItemID.HoneyComb)),
                Maybe(Of(ItemID.Nectar)),
                Maybe(Of(ItemID.HoneyedGoggles))),

            [ItemID.SkeletronBossBag] = All(
                Of(ItemID.BoneGlove),
                Either(Of(ItemID.SkeletronMask), Of(ItemID.SkeletronHand), Of(ItemID.BookofSkulls)),
                Of(ItemID.CopperCoin, 1, 14_41_44)),

            // The demon heart is a guaranteed drop if the player has not already used one. So we use Maybe for
            // simplicity.
            [ItemID.WallOfFleshBossBag] = All(
                Of(ItemID.Pwnhammer),
                Either(Of(ItemID.SummonerEmblem), Of(ItemID.SorcererEmblem), Of(ItemID.WarriorEmblem),
                       Of(ItemID.RangerEmblem)),
                Either(Of(ItemID.LaserRifle), Of(ItemID.BreakerBlade), Of(ItemID.ClockworkAssaultRifle)),
                Maybe(Of(ItemID.FleshMask)),
                Maybe(Of(ItemID.DemonHeart)),
                Of(ItemID.CopperCoin, 1, 23_06_30)),

            [ItemID.DestroyerBossBag] = All(
                Of(ItemID.MechanicalWagonPiece),
                Of(ItemID.SoulofMight, 25, 40),
                Of(ItemID.HallowedBar, 20, 35),
                Of(ItemID.CopperCoin, 1, 34_59_46),
                Maybe(Of(ItemID.DestroyerMask)),
                Maybe(DeveloperSets)),

            [ItemID.TwinsBossBag] = All(
                Of(ItemID.MechanicalWheelPiece),
                Of(ItemID.SoulofSight, 25, 40),
                Of(ItemID.HallowedBar, 20, 35),
                Of(ItemID.CopperCoin, 1, 34_59_46),
                Maybe(Of(ItemID.TwinMask)),
                Maybe(DeveloperSets)),

            [ItemID.SkeletronPrimeBossBag] = All(
                Of(ItemID.MechanicalBatteryPiece),
                Of(ItemID.SoulofFright, 25, 40),
                Of(ItemID.HallowedBar, 20, 35),
                Of(ItemID.CopperCoin, 1, 34_59_46),
                Maybe(Of(ItemID.SkeletronPrimeMask)),
                Maybe(DeveloperSets)),

            [ItemID.PlanteraBossBag] = All(
                Of(ItemID.SporeSac),
                Of(ItemID.TempleKey),
                Either(All(Of(ItemID.GrenadeLauncher),
                           Of(ItemID.RocketI, 20, 49)), Of(ItemID.VenusMagnum), Of(ItemID.NettleBurst),
                       Of(ItemID.LeafBlower), Of(ItemID.Seedler), Of(ItemID.FlowerPow), Of(ItemID.WaspGun)),
                Of(ItemID.CopperCoin, 1, 43_24_32),
                Maybe(Of(ItemID.PlanteraMask)),
                Maybe(Of(ItemID.Seedling)),
                Maybe(Of(ItemID.TheAxe)),
                Maybe(Of(ItemID.PygmyStaff)),
                Maybe(Of(ItemID.ThornHook)),
                Maybe(DeveloperSets)),

            [ItemID.GolemBossBag] = All(
                Of(ItemID.ShinyStone),
                Either(All(Of(ItemID.Stynger),
                           Of(ItemID.StyngerBolt, 60, 99)), Of(ItemID.PossessedHatchet), Of(ItemID.SunStone),
                       Of(ItemID.EyeoftheGolem), Of(ItemID.Picksaw), Of(ItemID.HeatRay), Of(ItemID.StaffofEarth),
                       Of(ItemID.GolemFist)),
                Of(ItemID.BeetleHusk, 18, 23),
                Of(ItemID.CopperCoin, 1, 43_24_32),
                Maybe(Of(ItemID.GolemMask)),
                Maybe(DeveloperSets)),

            [ItemID.FishronBossBag] = All(
                Of(ItemID.ShrimpyTruffle),
                Either(Of(ItemID.Flairon), Of(ItemID.Tsunami), Of(ItemID.RazorbladeTyphoon), Of(ItemID.TempestStaff),
                       Of(ItemID.BubbleGun)),
                Of(ItemID.CopperCoin, 1, 2_88_28),
                Maybe(Of(ItemID.DukeFishronMask)),
                Maybe(Of(ItemID.FishronWings)),
                Maybe(DeveloperSets)),

            // The portal gun is only a guaranteed drop if the player does not already have one in their inventory. So
            // we use Maybe for simplicity.
            [ItemID.MoonLordBossBag] = All(
                Of(ItemID.GravityGlobe),
                Of(ItemID.SuspiciousLookingTentacle),
                Of(ItemID.LunarOre, 90, 110),
                Either(Of(ItemID.Meowmere), Of(ItemID.Terrarian), Of(ItemID.StarWrath), Of(ItemID.SDMG),
                       Of(ItemID.FireworksLauncher), Of(ItemID.LastPrism), Of(ItemID.LunarFlareBook),
                       Of(ItemID.RainbowCrystalStaff), Of(ItemID.MoonlordTurretStaff)),
                Maybe(Of(ItemID.BossMaskMoonlord)),
                Maybe(Of(ItemID.PortalGun)),
                Maybe(DeveloperSets)),

            [ItemID.BossBagBetsy] = All(
                Either(Of(ItemID.DD2BetsyBow), Of(ItemID.DD2SquireBetsySword), Of(ItemID.ApprenticeStaffT3),
                       Of(ItemID.MonkStaffT3)),
                Of(ItemID.DefenderMedal, 30, 49),
                Maybe(Of(ItemID.BossMaskBetsy)),
                Maybe(Of(ItemID.BetsyWings)))
        };


        private static ItemConversion All(params ItemConversion[] itemConversions) =>
            new AllItemConversion(itemConversions);

        private static ItemConversion Either(params ItemConversion[] itemConversions) =>
            new EitherItemConversion(itemConversions);

        private static ItemConversion Maybe([NotNull] ItemConversion itemConversion) =>
            new MaybeItemConversion(itemConversion);

        private static ItemConversion Of(int itemId, int minStackSize = 1, int maxStackSize = 1) =>
            new OfItemConversion(itemId, minStackSize, maxStackSize);

        /// <summary>
        ///     Applies the item conversion instance to the specified debits, attempting to clear them out if possible.
        /// </summary>
        /// <param name="debits">The debits, which must not be <c>null</c> or contain <c>null</c>.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        public abstract bool Apply([ItemNotNull] [NotNull] IList<Transaction> debits);

        /// <summary>
        ///     Checks if the item conversion is contained in the specified debits.
        /// </summary>
        /// <param name="debits">The debits, which must not be <c>null</c> or contain <c>null</c>.</param>
        /// <returns><c>true</c> if the item conversion is contained; otherwise, <c>false</c>.</returns>
        public abstract bool Check([ItemNotNull] [NotNull] IList<Transaction> debits);

        private sealed class AllItemConversion : ItemConversion
        {
            private readonly ItemConversion[] _itemConversions;

            public AllItemConversion(params ItemConversion[] itemConversions)
            {
                Debug.Assert(itemConversions != null, "Item conversions must not be null.");
                Debug.Assert(!itemConversions.Contains(null), "Item conversions must not contain null.");

                _itemConversions = itemConversions;
            }

            public override bool Apply(IList<Transaction> debits)
            {
                var result = true;
                foreach (var itemConversion in _itemConversions)
                {
                    result &= itemConversion.Apply(debits);
                }
                return result;
            }

            public override bool Check(IList<Transaction> debits) => _itemConversions.All(ld => ld.Check(debits));
        }

        private sealed class EitherItemConversion : ItemConversion
        {
            private readonly ItemConversion[] _itemConversions;

            public EitherItemConversion(params ItemConversion[] itemConversions)
            {
                Debug.Assert(itemConversions != null, "Item conversions must not be null.");
                Debug.Assert(!itemConversions.Contains(null), "Item conversions must not contain null.");

                _itemConversions = itemConversions;
            }

            public override bool Apply(IList<Transaction> debits)
            {
                // We incrementally take slices of the debits. This is because we want to use as few of the oldest
                // debits as possible in matching any of the conversions.
                for (var i = 0; i < debits.Count; ++i)
                {
                    var subDebits = debits.Take(i + 1).ToList();
                    foreach (var itemConversion in _itemConversions)
                    {
                        if (itemConversion.Check(subDebits))
                        {
                            return itemConversion.Apply(subDebits);
                        }
                    }
                }
                return false;
            }

            public override bool Check(IList<Transaction> debits) => _itemConversions.Any(ld => ld.Check(debits));
        }

        private sealed class MaybeItemConversion : ItemConversion
        {
            private readonly ItemConversion _itemConversion;

            public MaybeItemConversion(ItemConversion itemConversion)
            {
                Debug.Assert(itemConversion != null, "Item conversion must not be null.");

                _itemConversion = itemConversion;
            }

            public override bool Apply(IList<Transaction> debits) => _itemConversion.Apply(debits);
            public override bool Check(IList<Transaction> debits) => true;
        }

        private sealed class OfItemConversion : ItemConversion
        {
            private readonly int _itemId;
            private readonly int _maxStackSize;
            private readonly int _minStackSize;

            public OfItemConversion(int itemId, int minStackSize, int maxStackSize)
            {
                Debug.Assert(itemId > 0, "Item ID must be positive.");
                Debug.Assert(itemId < ItemID.Count, "Item ID must in range.");
                Debug.Assert(minStackSize > 0, "Minimum stack size must be positive.");
                Debug.Assert(maxStackSize >= minStackSize, "Maximum stack size must be at least the minimum.");

                _itemId = itemId;
                _minStackSize = minStackSize;
                _maxStackSize = maxStackSize;
            }

            public override bool Apply(IList<Transaction> debits)
            {
                var stackLeft = _maxStackSize;
                var succeeded = false;
                foreach (var debit in debits.Where(d => d.ItemId == _itemId && d.StackSize < 0))
                {
                    var payment = Math.Min(stackLeft, -debit.StackSize);
                    Debug.Assert(payment > 0, "Payment must be positive.");
                    stackLeft -= payment;
                    debit.StackSize += payment;
                    Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for by {payment}, item conversion");
                    succeeded = true;

                    if (stackLeft == 0)
                    {
                        break;
                    }
                }
                return succeeded;
            }

            public override bool Check(IList<Transaction> debits)
            {
                return debits.Where(d => d.ItemId == _itemId).Sum(d => d.StackSize) <= -_minStackSize;
            }
        }
    }
}
