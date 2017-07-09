using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents a loot drop. An instance of this class describes the possible loot drops from, e.g., a grab bag or
    ///     fishing.
    /// </summary>
    public abstract class LootDrop
    {
        /// <summary>
        ///     The possible developer sets obtainable from a hardmode treasure bag.
        /// </summary>
        private static readonly LootDrop DeveloperSets = One(
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
                Of(ItemID.DevDye, 4)),
            All(Of(ItemID.LokisHelm), Of(ItemID.LokisShirt), Of(ItemID.LokisPants), Of(ItemID.LokisWings)),
            All(Of(ItemID.ArkhalisHat), Of(ItemID.ArkhalisShirt), Of(ItemID.ArkhalisPants), Of(ItemID.ArkhalisWings)),
            All(Of(ItemID.LeinforsHat), Of(ItemID.LeinforsShirt), Of(ItemID.LeinforsPants), Of(ItemID.LeinforsWings),
                Of(ItemID.LeinforsAccessory, 4)));

        /// <summary>
        ///     The possible drops from an herb bag.
        /// </summary>
        private static readonly LootDrop HerbBagDrops = One(
            // Herbs
            Of(ItemID.Daybloom, 8), Of(ItemID.Moonglow, 8), Of(ItemID.Blinkroot, 8), Of(ItemID.Deathweed, 8),
            Of(ItemID.Waterleaf, 8), Of(ItemID.Fireblossom, 8), Of(ItemID.Shiverthorn, 8),

            // Herb seeds
            Of(ItemID.DaybloomSeeds, 8), Of(ItemID.MoonglowSeeds, 8), Of(ItemID.BlinkrootSeeds, 8),
            Of(ItemID.DeathweedSeeds, 8), Of(ItemID.WaterleafSeeds, 8), Of(ItemID.FireblossomSeeds, 8),
            Of(ItemID.ShiverthornSeeds, 8));

        /// <summary>
        ///     The drops shared by all biome crates.
        /// </summary>
        private static readonly LootDrop SharedBiomeCrateDrops = All(
            Maybe(One(Of(ItemID.IronBar, 20), Of(ItemID.LeadBar, 20), Of(ItemID.SilverBar, 20),
                Of(ItemID.TungstenBar, 20), Of(ItemID.GoldBar, 20), Of(ItemID.PlatinumBar, 20))),
            Maybe(One(Of(ItemID.CobaltBar, 20), Of(ItemID.MythrilBar, 20), Of(ItemID.AdamantiteBar, 20),
                Of(ItemID.PalladiumBar, 20), Of(ItemID.OrichalcumBar, 20), Of(ItemID.TitaniumBar, 20))),
            Maybe(One(Of(ItemID.ObsidianSkinPotion, 4), Of(ItemID.SpelunkerPotion, 4), Of(ItemID.HunterPotion, 4),
                Of(ItemID.GravitationPotion, 4), Of(ItemID.MiningPotion, 4), Of(ItemID.HeartreachPotion, 4))),
            Maybe(One(Of(ItemID.LesserHealingPotion, 17), Of(ItemID.LesserManaPotion, 17))),
            Maybe(One(Of(ItemID.JourneymanBait, 4), Of(ItemID.MasterBait, 4))),
            Maybe(Of(ItemID.CopperCoin, 120000)));

        /// <summary>
        ///     The rewards available from the angler.
        /// </summary>
        public static readonly LootDrop AnglerRewards = All(
            One(Of(ItemID.FuzzyCarrot), Of(ItemID.AnglerHat), Of(ItemID.AnglerVest), Of(ItemID.AnglerPants),
                Of(ItemID.GoldenFishingRod), Of(ItemID.GoldenBugNet), Of(ItemID.FishHook),
                Of(ItemID.HighTestFishingLine), Of(ItemID.AnglerEarring), Of(ItemID.TackleBox),
                Of(ItemID.FishermansGuide), Of(ItemID.WeatherRadio), Of(ItemID.Sextant), Of(ItemID.FinWings),
                Of(ItemID.BottomlessBucket), Of(ItemID.SuperAbsorbantSponge), Of(ItemID.HotlineFishingHook),
                Of(ItemID.SonarPotion, 5), Of(ItemID.FishingPotion, 5), Of(ItemID.CratePotion, 5),
                Of(ItemID.CoralstoneBlock, 150), Of(ItemID.BunnyfishTrophy), Of(ItemID.GoldfishTrophy),
                Of(ItemID.SharkteethTrophy), Of(ItemID.SwordfishTrophy), Of(ItemID.TreasureMap),
                Of(ItemID.SeaweedPlanter), Of(ItemID.PillaginMePixels), Of(ItemID.CompassRose), Of(ItemID.ShipsWheel),
                Of(ItemID.LifePreserver), Of(ItemID.WallAnchor), Of(ItemID.ShipInABottle),
                All(Of(ItemID.SeashellHairpin), Of(ItemID.MermaidAdornment), Of(ItemID.MermaidTail)),
                All(Of(ItemID.FishCostumeMask), Of(ItemID.FishCostumeShirt), Of(ItemID.FishCostumeFinskirt))),
            Of(ItemID.CopperCoin, 100000),
            Maybe(One(Of(ItemID.ApprenticeBait, 7), Of(ItemID.JourneymanBait, 7), Of(ItemID.MasterBait, 7))));

        /// <summary>
        ///     The rewards available from the dye trader.
        /// </summary>
        public static readonly LootDrop DyeTraderRewards = One(
            Of(ItemID.AcidDye, 3), Of(ItemID.RedAcidDye, 3), Of(ItemID.BlueAcidDye, 3), Of(ItemID.MushroomDye, 3),
            Of(ItemID.PurpleOozeDye, 3), Of(ItemID.ReflectiveDye, 3), Of(ItemID.ReflectiveGoldDye, 3),
            Of(ItemID.ReflectiveSilverDye, 3), Of(ItemID.ReflectiveObsidianDye, 3), Of(ItemID.ReflectiveCopperDye, 3),
            Of(ItemID.ReflectiveMetalDye, 3), Of(ItemID.NegativeDye, 3), Of(ItemID.ShadowDye, 3),
            Of(ItemID.MirageDye, 3), Of(ItemID.TwilightDye, 3), Of(ItemID.HadesDye, 3), Of(ItemID.BurningHadesDye, 3),
            Of(ItemID.ShadowflameHadesDye, 3), Of(ItemID.GrimDye, 3), Of(ItemID.PhaseDye, 3),
            Of(ItemID.ShiftingSandsDye, 3), Of(ItemID.GelDye, 3), Of(ItemID.ChlorophyteDye, 3),
            Of(ItemID.LivingFlameDye, 3), Of(ItemID.LivingRainbowDye, 3), Of(ItemID.LivingOceanDye, 3),
            Of(ItemID.WispDye, 3), Of(ItemID.PixieDye, 3), Of(ItemID.UnicornWispDye, 3), Of(ItemID.InfernalWispDye, 3),
            Of(ItemID.MartianArmorDye, 3), Of(ItemID.MidnightRainbowDye, 3), Of(ItemID.DevDye, 3));

        /// <summary>
        ///     The drops available from extractinators.
        /// </summary>
        public static readonly LootDrop ExtractinatorDrops = One(
            Of(ItemID.AmberMosquito),

            // Gems
            Of(ItemID.Diamond, 16), Of(ItemID.Ruby, 16), Of(ItemID.Emerald, 16), Of(ItemID.Sapphire, 16),
            Of(ItemID.Topaz, 16), Of(ItemID.Amethyst, 16), Of(ItemID.Amber, 16),

            // Ores
            Of(ItemID.FossilOre, 7), Of(ItemID.CopperOre, 16), Of(ItemID.IronOre, 16), Of(ItemID.SilverOre, 16),
            Of(ItemID.GoldOre, 16), Of(ItemID.TinOre, 16), Of(ItemID.LeadOre, 16), Of(ItemID.TungstenOre, 16),
            Of(ItemID.PlatinumOre, 16),

            // Coins
            Of(ItemID.CopperCoin, 11_00_00_00), Of(ItemID.CopperCoin, 1_00_00_00), Of(ItemID.CopperCoin, 1_00_00),
            Of(ItemID.CopperCoin, 1_00));

        /// <summary>
        ///     The drops available from fishing.
        /// </summary>
        public static readonly LootDrop FishingDrops = One(
            // Fish
            Of(ItemID.ArmoredCavefish), Of(ItemID.AtlanticCod), Of(ItemID.Bass), Of(ItemID.BlueJellyfish),
            Of(ItemID.ChaosFish), Of(ItemID.CrimsonTigerfish), Of(ItemID.Damselfish), Of(ItemID.DoubleCod),
            Of(ItemID.Ebonkoi), Of(ItemID.FlarefinKoi), Of(ItemID.FrostMinnow), Of(ItemID.GoldenCarp),
            Of(ItemID.GreenJellyfish), Of(ItemID.Hemopiranha), Of(ItemID.Honeyfin), Of(ItemID.NeonTetra),
            Of(ItemID.Obsidifish), Of(ItemID.PinkJellyfish), Of(ItemID.PrincessFish), Of(ItemID.Prismite),
            Of(ItemID.RedSnapper), Of(ItemID.Salmon), Of(ItemID.Shrimp), Of(ItemID.SpecularFish), Of(ItemID.Stinkfish),
            Of(ItemID.Trout), Of(ItemID.Tuna), Of(ItemID.VariegatedLardfish),

            // Quest fish
            // TODO: consider making this only accept the current quest fish? Probably not worth it?
            Of(ItemID.AmanitiaFungifin), Of(ItemID.Angelfish), Of(ItemID.Batfish), Of(ItemID.BloodyManowar),
            Of(ItemID.Bonefish), Of(ItemID.BumblebeeTuna), Of(ItemID.Bunnyfish), Of(ItemID.CapnTunabeard),
            Of(ItemID.Catfish), Of(ItemID.Cloudfish), Of(ItemID.Clownfish), Of(ItemID.Cursedfish),
            Of(ItemID.DemonicHellfish), Of(ItemID.Derpfish), Of(ItemID.Dirtfish), Of(ItemID.DynamiteFish),
            Of(ItemID.EaterofPlankton), Of(ItemID.FallenStarfish), Of(ItemID.TheFishofCthulu),
            Of(ItemID.Fishotron), Of(ItemID.Fishron), Of(ItemID.GuideVoodooFish), Of(ItemID.Harpyfish),
            Of(ItemID.Hungerfish), Of(ItemID.Ichorfish), Of(ItemID.InfectedScabbardfish), Of(ItemID.Jewelfish),
            Of(ItemID.MirageFish), Of(ItemID.Mudfish), Of(ItemID.MutantFlinxfin), Of(ItemID.Pengfish),
            Of(ItemID.Pixiefish), Of(ItemID.Slimefish), Of(ItemID.Spiderfish), Of(ItemID.TropicalBarracuda),
            Of(ItemID.TundraTrout), Of(ItemID.UnicornFish), Of(ItemID.ZombieFish),

            // Miscellaneous items
            Of(ItemID.FrogLeg), Of(ItemID.BalloonPufferfish), Of(ItemID.BombFish, 20), Of(ItemID.PurpleClubberfish),
            Of(ItemID.ReaverShark), Of(ItemID.Rockfish), Of(ItemID.SawtoothShark), Of(ItemID.FrostDaggerfish, 40),
            Of(ItemID.Swordfish), Of(ItemID.ZephyrFish), Of(ItemID.Honeyfin), Of(ItemID.Toxikarp),
            Of(ItemID.Bladetongue), Of(ItemID.CrystalSerpent), Of(ItemID.ScalyTruffle), Of(ItemID.ObsidianSwordfish),

            // Crates
            Of(ItemID.WoodenCrate), Of(ItemID.IronCrate), Of(ItemID.JungleFishingCrate),
            Of(ItemID.FloatingIslandFishingCrate), Of(ItemID.CorruptFishingCrate), Of(ItemID.CrimsonFishingCrate),
            Of(ItemID.HallowedFishingCrate), Of(ItemID.DungeonFishingCrate), Of(ItemID.GoldenCrate),

            // Junk
            Of(ItemID.OldShoe), Of(ItemID.FishingSeaweed), Of(ItemID.TinCan));

        /// <summary>
        ///     The mapping from grab bag IDs to loot drops.
        /// </summary>
        public static readonly IDictionary<int, LootDrop> GrabBags = new Dictionary<int, LootDrop>
        {
            [ItemID.GoodieBag] = One(
                Of(ItemID.UnluckyYarn),
                Of(ItemID.BatHook),
                Of(ItemID.RottenEgg, 40),
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

            [ItemID.Present] = One(
                Of(ItemID.Coal),
                Of(ItemID.DogWhistle),
                All(Of(ItemID.RedRyder), Of(ItemID.MusketBall, 60)),
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
                Of(ItemID.Eggnog, 3),
                Of(ItemID.StarAnise, 40),
                Of(ItemID.PineTreeBlock, 49),
                Of(ItemID.CandyCaneBlock, 49),
                Of(ItemID.GreenCandyCaneBlock, 49),
                Of(ItemID.SnowGlobe)),

            [ItemID.WoodenCrate] = All(
                Maybe(Of(ItemID.Sundial)),
                Maybe(Of(ItemID.SailfishBoots)),
                Maybe(Of(ItemID.TsunamiInABottle)),
                Maybe(Of(ItemID.Anchor)),
                Maybe(One(Of(ItemID.Aglet), Of(ItemID.Umbrella), Of(ItemID.ClimbingClaws), Of(ItemID.CordageGuide),
                    Of(ItemID.Radar))),
                Maybe(One(Of(ItemID.CopperOre, 20), Of(ItemID.IronOre, 20), Of(ItemID.SilverOre, 20),
                    Of(ItemID.GoldOre, 20), Of(ItemID.TinOre, 20), Of(ItemID.LeadOre, 20),
                    Of(ItemID.TungstenOre, 20), Of(ItemID.PlatinumOre, 20))),
                Maybe(One(Of(ItemID.CobaltOre, 20), Of(ItemID.MythrilOre, 20), Of(ItemID.AdamantiteOre, 20),
                    Of(ItemID.PalladiumOre, 20), Of(ItemID.OrichalcumOre, 20), Of(ItemID.TitaniumOre, 20))),
                Maybe(One(Of(ItemID.CopperBar, 20), Of(ItemID.IronBar, 20), Of(ItemID.SilverBar, 20),
                    Of(ItemID.GoldBar, 20), Of(ItemID.TinBar, 20), Of(ItemID.LeadBar, 20),
                    Of(ItemID.TungstenBar, 20), Of(ItemID.PlatinumBar, 20))),
                Maybe(One(Of(ItemID.CobaltBar, 7), Of(ItemID.MythrilBar, 7), Of(ItemID.AdamantiteBar, 7),
                    Of(ItemID.PalladiumBar, 7), Of(ItemID.OrichalcumBar, 7), Of(ItemID.TitaniumBar, 7))),
                Maybe(One(Of(ItemID.ObsidianSkinPotion, 3), Of(ItemID.SwiftnessPotion, 3), Of(ItemID.IronskinPotion, 3),
                    Of(ItemID.NightOwlPotion, 3), Of(ItemID.ShinePotion, 3), Of(ItemID.HunterPotion, 3),
                    Of(ItemID.GillsPotion, 3), Of(ItemID.MiningPotion, 3), Of(ItemID.HeartreachPotion, 3),
                    Of(ItemID.TrapsightPotion, 3))),
                Maybe(One(Of(ItemID.LesserHealingPotion, 15), Of(ItemID.LesserManaPotion, 15))),
                Maybe(One(Of(ItemID.ApprenticeBait, 4), Of(ItemID.JourneymanBait, 4))),
                Maybe(Of(ItemID.CopperCoin, 5_90_00))),

            [ItemID.IronCrate] = All(
                Maybe(Of(ItemID.Sundial)),
                Maybe(Of(ItemID.SailfishBoots)),
                Maybe(Of(ItemID.TsunamiInABottle)),
                Maybe(Of(ItemID.GingerBeard)),
                Maybe(Of(ItemID.TartarSauce)),
                Maybe(Of(ItemID.FalconBlade)),
                Maybe(One(Of(ItemID.CopperBar, 14), Of(ItemID.IronBar, 14), Of(ItemID.SilverBar, 14),
                    Of(ItemID.GoldBar, 14), Of(ItemID.TinBar, 14), Of(ItemID.LeadBar, 14),
                    Of(ItemID.TungstenBar, 14), Of(ItemID.PlatinumBar, 14))),
                Maybe(One(Of(ItemID.CobaltBar, 14), Of(ItemID.MythrilBar, 14), Of(ItemID.AdamantiteBar, 14),
                    Of(ItemID.PalladiumBar, 14), Of(ItemID.OrichalcumBar, 14), Of(ItemID.TitaniumBar, 14))),
                Maybe(One(Of(ItemID.ObsidianSkinPotion, 3), Of(ItemID.SpelunkerPotion, 3), Of(ItemID.HunterPotion, 3),
                    Of(ItemID.GravitationPotion, 3), Of(ItemID.MiningPotion, 3), Of(ItemID.HeartreachPotion, 3),
                    Of(ItemID.CalmingPotion, 3), Of(ItemID.FlipperPotion, 3))),
                Maybe(One(Of(ItemID.HealingPotion, 15), Of(ItemID.ManaPotion, 15))),
                Maybe(One(Of(ItemID.JourneymanBait, 4), Of(ItemID.MasterBait, 4))),
                Maybe(Of(ItemID.CopperCoin, 10_00_00))),

            [ItemID.GoldenCrate] = All(
                Maybe(Of(ItemID.Sundial)),
                Maybe(Of(ItemID.HardySaddle)),
                Maybe(One(Of(ItemID.SilverBar, 30), Of(ItemID.GoldBar, 30), Of(ItemID.TungstenBar, 30),
                    Of(ItemID.PlatinumBar, 30))),
                Maybe(One(Of(ItemID.MythrilBar, 30), Of(ItemID.AdamantiteBar, 30), Of(ItemID.OrichalcumBar, 30),
                    Of(ItemID.TitaniumBar, 30))),
                Maybe(One(Of(ItemID.ObsidianSkinPotion, 3), Of(ItemID.SpelunkerPotion, 3),
                    Of(ItemID.GravitationPotion, 3), Of(ItemID.MiningPotion, 3), Of(ItemID.HeartreachPotion, 3))),
                Maybe(One(Of(ItemID.GreaterHealingPotion, 20), Of(ItemID.GreaterManaPotion, 20))),
                Maybe(Of(ItemID.MasterBait, 7)),
                Maybe(Of(ItemID.CopperCoin, 20_00_00))),

            [ItemID.LockBox] = All(
                One(Of(ItemID.MagicMissile), Of(ItemID.Muramasa), Of(ItemID.CobaltShield), Of(ItemID.AquaScepter),
                    Of(ItemID.BlueMoon), Of(ItemID.Handgun), Of(ItemID.ShadowKey)),
                Maybe(One(Of(ItemID.SpelunkerPotion), Of(ItemID.EndurancePotion), Of(ItemID.GravitationPotion),
                    Of(ItemID.HeartreachPotion), Of(ItemID.IronskinPotion), Of(ItemID.MagicPowerPotion),
                    Of(ItemID.ObsidianSkinPotion), Of(ItemID.WormholePotion))),
                Maybe(Of(ItemID.HealingPotion)),
                Maybe(Of(ItemID.CopperCoin, 6_99_99))),

            // Herb bags can run their drops either 3 or 4 times.
            [ItemID.HerbBag] = All(
                HerbBagDrops,
                HerbBagDrops,
                HerbBagDrops,
                Maybe(HerbBagDrops)),

            [ItemID.CorruptFishingCrate] = All(
                Maybe(One(Of(ItemID.BallOHurt), Of(ItemID.BandofStarpower), Of(ItemID.Musket), Of(ItemID.ShadowOrb),
                    Of(ItemID.Vilethorn))),
                Maybe(Of(ItemID.SoulofNight, 5)),
                Maybe(Of(ItemID.CursedFlame, 5)),
                SharedBiomeCrateDrops),

            [ItemID.CrimsonFishingCrate] = All(
                Maybe(One(Of(ItemID.TheUndertaker), Of(ItemID.TheRottedFork), Of(ItemID.CrimsonRod),
                    Of(ItemID.PanicNecklace), Of(ItemID.CrimsonHeart))),
                Maybe(Of(ItemID.SoulofNight, 5)),
                Maybe(Of(ItemID.Ichor, 5)),
                SharedBiomeCrateDrops),

            [ItemID.DungeonFishingCrate] = All(
                Maybe(Of(ItemID.LockBox)),
                SharedBiomeCrateDrops),

            [ItemID.FloatingIslandFishingCrate] = All(
                Maybe(One(Of(ItemID.LuckyHorseshoe), Of(ItemID.Starfury), Of(ItemID.ShinyRedBalloon))),
                SharedBiomeCrateDrops),

            [ItemID.HallowedFishingCrate] = All(
                Maybe(Of(ItemID.SoulofLight, 5)),
                Maybe(Of(ItemID.CrystalShard, 10)),
                SharedBiomeCrateDrops),

            [ItemID.JungleFishingCrate] = All(
                Maybe(One(Of(ItemID.AnkletoftheWind), Of(ItemID.Boomstick), Of(ItemID.FeralClaws),
                    Of(ItemID.StaffofRegrowth), Of(ItemID.FiberglassFishingPole))),
                SharedBiomeCrateDrops),

            [ItemID.KingSlimeBossBag] = All(
                Of(ItemID.RoyalGel),
                One(Of(ItemID.NinjaHood), Of(ItemID.NinjaShirt), Of(ItemID.NinjaPants)),
                One(Of(ItemID.NinjaHood), Of(ItemID.NinjaShirt), Of(ItemID.NinjaPants)),
                One(Of(ItemID.SlimeGun), Of(ItemID.SlimeHook)),
                Of(ItemID.Solidifier),
                Of(ItemID.CopperCoin, 14_41_44),
                Maybe(Of(ItemID.KingSlimeMask)),
                Maybe(Of(ItemID.SlimySaddle))),

            [ItemID.EyeOfCthulhuBossBag] = All(
                Of(ItemID.EoCShield),
                One(All(Of(ItemID.CrimtaneOre, 87), Of(ItemID.CrimsonSeeds, 3)),
                    All(Of(ItemID.DemoniteOre, 87), Of(ItemID.CorruptSeeds, 3), Of(ItemID.UnholyArrow, 49))),
                Of(ItemID.CopperCoin, 8_64_86),
                Maybe(Of(ItemID.EyeMask)),
                Maybe(Of(ItemID.Binoculars)),
                Maybe(Of(ItemID.AviatorSunglasses))),

            [ItemID.EaterOfWorldsBossBag] = All(
                Of(ItemID.WormScarf),
                Of(ItemID.DemoniteOre, 59),
                Of(ItemID.ShadowScale, 19),
                Of(ItemID.CopperCoin, 8_65),
                Maybe(Of(ItemID.EaterMask)),
                Maybe(Of(ItemID.EatersBone))),

            [ItemID.BrainOfCthulhuBossBag] = All(
                Of(ItemID.BrainOfConfusion),
                Of(ItemID.CrimtaneOre, 90),
                Of(ItemID.TissueSample, 19),
                Of(ItemID.CopperCoin, 14_41_44),
                Maybe(Of(ItemID.BrainMask)),
                Maybe(Of(ItemID.BoneRattle))),

            [ItemID.QueenBeeBossBag] = All(
                Of(ItemID.HiveBackpack),
                One(Of(ItemID.BeeGun), Of(ItemID.BeeKeeper), Of(ItemID.BeesKnees)),
                Of(ItemID.HiveWand),
                One(Of(ItemID.BeeHat), Of(ItemID.BeeShirt), Of(ItemID.BeePants)),
                Of(ItemID.Beenade, 29),
                Of(ItemID.BeeWax, 29),
                Of(ItemID.CopperCoin, 28_82_88),
                Maybe(Of(ItemID.BeeMask)),
                Maybe(Of(ItemID.HoneyComb)),
                Maybe(Of(ItemID.Nectar)),
                Maybe(Of(ItemID.HoneyedGoggles))),

            [ItemID.SkeletronBossBag] = All(
                Of(ItemID.BoneGlove),
                One(Of(ItemID.SkeletronMask), Of(ItemID.SkeletronHand), Of(ItemID.BookofSkulls)),
                Of(ItemID.CopperCoin, 14_41_44)),

            // The demon heart is a guaranteed drop if the player has not already used one. So we use Maybe for
            // simplicity.
            [ItemID.WallOfFleshBossBag] = All(
                Of(ItemID.Pwnhammer),
                One(Of(ItemID.SummonerEmblem), Of(ItemID.SorcererEmblem), Of(ItemID.WarriorEmblem),
                    Of(ItemID.RangerEmblem)),
                One(Of(ItemID.LaserRifle), Of(ItemID.BreakerBlade), Of(ItemID.ClockworkAssaultRifle)),
                Maybe(Of(ItemID.FleshMask)),
                Maybe(Of(ItemID.DemonHeart)),
                Of(ItemID.CopperCoin, 23_06_30)),

            [ItemID.DestroyerBossBag] = All(
                Of(ItemID.MechanicalWagonPiece),
                Of(ItemID.SoulofMight, 40),
                Of(ItemID.HallowedBar, 35),
                Of(ItemID.CopperCoin, 34_59_46),
                Maybe(Of(ItemID.DestroyerMask)),
                Maybe(DeveloperSets)),

            [ItemID.TwinsBossBag] = All(
                Of(ItemID.MechanicalWheelPiece),
                Of(ItemID.SoulofSight, 40),
                Of(ItemID.HallowedBar, 35),
                Of(ItemID.CopperCoin, 34_59_46),
                Maybe(Of(ItemID.TwinMask)),
                Maybe(DeveloperSets)),

            [ItemID.SkeletronPrimeBossBag] = All(
                Of(ItemID.MechanicalBatteryPiece),
                Of(ItemID.SoulofFright, 40),
                Of(ItemID.HallowedBar, 35),
                Of(ItemID.CopperCoin, 34_59_46),
                Maybe(Of(ItemID.SkeletronPrimeMask)),
                Maybe(DeveloperSets)),

            [ItemID.PlanteraBossBag] = All(
                Of(ItemID.SporeSac),
                Of(ItemID.TempleKey),
                One(All(Of(ItemID.GrenadeLauncher),
                        Of(ItemID.RocketI, 49)), Of(ItemID.VenusMagnum), Of(ItemID.NettleBurst), Of(ItemID.LeafBlower),
                    Of(ItemID.Seedler), Of(ItemID.FlowerPow), Of(ItemID.WaspGun)),
                Of(ItemID.CopperCoin, 43_24_32),
                Maybe(Of(ItemID.PlanteraMask)),
                Maybe(Of(ItemID.Seedling)),
                Maybe(Of(ItemID.TheAxe)),
                Maybe(Of(ItemID.PygmyStaff)),
                Maybe(Of(ItemID.ThornHook)),
                Maybe(DeveloperSets)),

            [ItemID.GolemBossBag] = All(
                Of(ItemID.ShinyStone),
                One(All(Of(ItemID.Stynger),
                        Of(ItemID.StyngerBolt, 99)), Of(ItemID.PossessedHatchet), Of(ItemID.SunStone),
                    Of(ItemID.EyeoftheGolem), Of(ItemID.Picksaw), Of(ItemID.HeatRay), Of(ItemID.StaffofEarth),
                    Of(ItemID.GolemFist)),
                Of(ItemID.BeetleHusk, 23),
                Of(ItemID.CopperCoin, 43_24_32),
                Maybe(Of(ItemID.GolemMask)),
                Maybe(DeveloperSets)),

            [ItemID.FishronBossBag] = All(
                Of(ItemID.ShrimpyTruffle),
                One(Of(ItemID.Flairon), Of(ItemID.Tsunami), Of(ItemID.RazorbladeTyphoon), Of(ItemID.TempestStaff),
                    Of(ItemID.BubbleGun)),
                Of(ItemID.CopperCoin, 2_88_28),
                Maybe(Of(ItemID.DukeFishronMask)),
                Maybe(Of(ItemID.FishronWings)),
                Maybe(DeveloperSets)),

            // The portal gun is only a guaranteed drop if the player does not already have one in their inventory. So
            // we use Maybe for simplicity.
            [ItemID.MoonLordBossBag] = All(
                Of(ItemID.GravityGlobe),
                Of(ItemID.SuspiciousLookingTentacle),
                Of(ItemID.LunarOre, 110),
                One(Of(ItemID.Meowmere), Of(ItemID.Terrarian), Of(ItemID.StarWrath), Of(ItemID.SDMG),
                    Of(ItemID.FireworksLauncher), Of(ItemID.LastPrism), Of(ItemID.LunarFlareBook),
                    Of(ItemID.RainbowCrystalStaff), Of(ItemID.MoonlordTurretStaff)),
                Maybe(Of(ItemID.BossMaskMoonlord)),
                Maybe(Of(ItemID.PortalGun)),
                Maybe(DeveloperSets)),

            [ItemID.BossBagBetsy] = All(
                One(Of(ItemID.DD2BetsyBow), Of(ItemID.DD2SquireBetsySword), Of(ItemID.ApprenticeStaffT3),
                    Of(ItemID.MonkStaffT3)),
                Of(ItemID.DefenderMedal, 49),
                Maybe(Of(ItemID.BossMaskBetsy)),
                Maybe(Of(ItemID.BetsyWings)))
        };


        [NotNull]
        private static LootDrop All([ItemNotNull] [NotNull] params LootDrop[] lootDrops)
        {
            Debug.Assert(lootDrops != null, "Loot drops must not be null.");
            Debug.Assert(!lootDrops.Contains(null), "Loot drops must not contain null.");

            return new AllLootDrop(lootDrops);
        }

        [NotNull]
        private static LootDrop Maybe([NotNull] LootDrop lootDrop)
        {
            Debug.Assert(lootDrop != null, "Loot drop must not be null.");

            return new MaybeLootDrop(lootDrop);
        }

        [NotNull]
        private static LootDrop Of(int itemId, int maxStackSize = 1)
        {
            Debug.Assert(0 <= itemId && itemId < Main.maxItemTypes, "Item ID must be valid.");
            Debug.Assert(maxStackSize >= 0, "Maximum stack size must be non-negative.");

            return new SimpleLootDrop(itemId, maxStackSize);
        }

        [NotNull]
        private static LootDrop One(params LootDrop[] lootDrops)
        {
            Debug.Assert(lootDrops != null, "Loot drops must not be null.");
            Debug.Assert(!lootDrops.Contains(null), "Loot drops must not contain null.");

            return new OneLootDrop(lootDrops);
        }

        /// <summary>
        ///     Applies the loot drop to the specified debits, canceling out as many as possible.
        /// </summary>
        /// <param name="debits">The debits, which must not be <c>null</c> or contain <c>null</c>.</param>
        /// <returns><c>true</c> if any changes were made; otherwise, <c>false</c>.</returns>
        public abstract bool Apply([ItemNotNull] [NotNull] IList<Transaction> debits);

        /// <summary>
        ///     Determines if the loot drop is contained in the specified debits, requiring as few as possible.
        /// </summary>
        /// <param name="debits">The debits, which must not be <c>null</c> or contain <c>null</c>.</param>
        /// <returns></returns>
        /// <returns><c>true</c> if the loot drop is contained; otherwise, <c>false</c>.</returns>
        public abstract bool IsContainedIn([ItemNotNull] [NotNull] IList<Transaction> debits);

        private sealed class AllLootDrop : LootDrop
        {
            private readonly LootDrop[] _lootDrops;

            public AllLootDrop(params LootDrop[] lootDrops)
            {
                _lootDrops = lootDrops;
            }

            public override bool Apply(IList<Transaction> debits)
            {
                var result = true;
                foreach (var lootDrop in _lootDrops)
                {
                    result &= lootDrop.Apply(debits);
                }
                return result;
            }

            public override bool IsContainedIn(IList<Transaction> debits) =>
                _lootDrops.All(ld => ld.IsContainedIn(debits));
        }

        private sealed class MaybeLootDrop : LootDrop
        {
            private readonly LootDrop _lootDrop;

            public MaybeLootDrop(LootDrop lootDrop)
            {
                _lootDrop = lootDrop;
            }

            public override bool Apply(IList<Transaction> debits) => _lootDrop.Apply(debits);
            public override bool IsContainedIn(IList<Transaction> debits) => true;
        }

        private sealed class OneLootDrop : LootDrop
        {
            private readonly LootDrop[] _lootDrops;

            public OneLootDrop(params LootDrop[] lootDrops)
            {
                _lootDrops = lootDrops;
            }

            public override bool Apply(IList<Transaction> debits) => _lootDrops.Any(ld => ld.Apply(debits));

            public override bool IsContainedIn(IList<Transaction> debits) =>
                _lootDrops.Any(ld => ld.IsContainedIn(debits));
        }

        private sealed class SimpleLootDrop : LootDrop
        {
            private readonly int _itemId;
            private readonly int _maxStackSize;

            public SimpleLootDrop(int itemId, int maxStackSize)
            {
                _itemId = itemId;
                _maxStackSize = maxStackSize;
            }

            public override bool Apply(IList<Transaction> debits)
            {
                var stackLeft = _maxStackSize;
                var succeeded = false;
                foreach (var debit in debits.Where(d => d.ItemId == _itemId && d.StackSize < 0))
                {
                    var payment = Math.Min(stackLeft, -debit.StackSize);
                    stackLeft -= payment;
                    debit.StackSize += payment;
                    succeeded = true;

                    // Stop if the stack has been cleared out so we don't unnecessarily check more debits.
                    if (stackLeft <= 0)
                    {
                        break;
                    }
                }
                return succeeded;
            }

            public override bool IsContainedIn(IList<Transaction> debits) =>
                debits.Any(d => d.ItemId == _itemId);
        }
    }
}
