using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Net;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents the module that prevents item spawning.
    /// </summary>
    // TODO: handle buffs
    // TODO: handle hp healing
    // TODO: handle mp healing
    public sealed class Module : NoCheatModule
    {
        private static readonly int[] GrassIds =
        {
            TileID.Plants, TileID.CorruptPlants, TileID.JunglePlants, TileID.HallowedPlants, TileID.FleshWeeds
        };

        private static readonly Dictionary<int, int> NebulaBuffIdToItemId = new Dictionary<int, int>
        {
            [BuffID.NebulaUpLife1] = ItemID.NebulaPickup2,
            [BuffID.NebulaUpMana1] = ItemID.NebulaPickup3,
            [BuffID.NebulaUpDmg1] = ItemID.NebulaPickup1
        };

        private static readonly Dictionary<int, int> NpcIdToShopId = new Dictionary<int, int>
        {
            [NPCID.Merchant] = 1,
            [NPCID.ArmsDealer] = 2,
            [NPCID.Dryad] = 3,
            [NPCID.Demolitionist] = 4,
            [NPCID.Clothier] = 5,
            [NPCID.GoblinTinkerer] = 6,
            [NPCID.Wizard] = 7,
            [NPCID.Mechanic] = 8,
            [NPCID.SantaClaus] = 9,
            [NPCID.Truffle] = 10,
            [NPCID.Steampunker] = 11,
            [NPCID.DyeTrader] = 12,
            [NPCID.PartyGirl] = 13,
            [NPCID.Cyborg] = 14,
            [NPCID.Painter] = 15,
            [NPCID.WitchDoctor] = 16,
            [NPCID.Pirate] = 17,
            [NPCID.Stylist] = 18,
            [NPCID.TravellingMerchant] = 19,
            [NPCID.SkeletonMerchant] = 20,
            [NPCID.DD2Bartender] = 21
        };

        private static readonly int[] ProjectileIdsDroppingWoodenArrows =
        {
            ProjectileID.FireArrow, ProjectileID.CursedArrow, ProjectileID.FrostburnArrow
        };

        private static readonly Dictionary<int, int> ProjectileIdToItemDropId = new Dictionary<int, int>
        {
            [ProjectileID.WoodenArrowFriendly] = ItemID.WoodenArrow,
            [ProjectileID.FireArrow] = ItemID.FlamingArrow,
            [ProjectileID.Shuriken] = ItemID.Shuriken,
            [ProjectileID.UnholyArrow] = ItemID.UnholyArrow,
            [ProjectileID.FallingStar] = ItemID.FallenStar,
            [ProjectileID.Bone] = ItemID.Bone,
            [ProjectileID.SandBallGun] = ItemID.SandBlock,
            [ProjectileID.ThrowingKnife] = ItemID.ThrowingKnife,
            [ProjectileID.Glowstick] = ItemID.Glowstick,
            [ProjectileID.StickyGlowstick] = ItemID.StickyGlowstick,
            [ProjectileID.PoisonedKnife] = ItemID.PoisonedKnife,
            [ProjectileID.EbonsandBallGun] = ItemID.EbonsandBlock,
            [ProjectileID.PearlSandBallGun] = ItemID.PearlsandBlock,
            [ProjectileID.HolyArrow] = ItemID.HolyArrow,
            [ProjectileID.CursedArrow] = ItemID.CursedArrow,
            [ProjectileID.BeachBall] = ItemID.BeachBall,
            [ProjectileID.RopeCoil] = ItemID.RopeCoil,
            [ProjectileID.FrostburnArrow] = ItemID.FrostburnArrow,
            [ProjectileID.CrimsandBallGun] = ItemID.CrimsandBlock,
            [ProjectileID.BoneArrowFromMerchant] = ItemID.BoneArrow,
            [ProjectileID.VineRopeCoil] = ItemID.VineRopeCoil,
            [ProjectileID.SilkRopeCoil] = ItemID.SilkRopeCoil,
            [ProjectileID.WebRopeCoil] = ItemID.WebRopeCoil,
            [ProjectileID.BouncyGlowstick] = ItemID.BouncyGlowstick,
            [ProjectileID.BoneJavelin] = ItemID.BoneJavelin,
            [ProjectileID.BoneDagger] = ItemID.BoneDagger
        };

        private static readonly int[] ReducedAmmoItemIds =
        {
            ItemID.Minishark, ItemID.ClockworkAssaultRifle, ItemID.Megashark, ItemID.SDMG, ItemID.CandyCornRifle,
            ItemID.ChainGun, ItemID.Gatligator, ItemID.VortexBeater, ItemID.Phantasm
        };

        private static readonly Dictionary<int, int> RopeCoilProjectileIdToRopeId = new Dictionary<int, int>
        {
            [ProjectileID.RopeCoil] = ItemID.Rope,
            [ProjectileID.VineRopeCoil] = ItemID.VineRope,
            [ProjectileID.SilkRopeCoil] = ItemID.SilkRope,
            [ProjectileID.WebRopeCoil] = ItemID.WebRope
        };

        private static readonly Dictionary<int, int> SummonIdToItemId = new Dictionary<int, int>
        {
            [-8] = ItemID.CelestialSigil,
            [-6] = ItemID.SolarTablet,
            [-5] = ItemID.NaughtyPresent,
            [-4] = ItemID.PumpkinMoonMedallion,
            [-3] = ItemID.PirateMap,
            [-2] = ItemID.SnowGlobe,
            [-1] = ItemID.GoblinBattleStandard,
            [NPCID.EyeofCthulhu] = ItemID.SuspiciousLookingEye,
            [NPCID.EaterofWorldsHead] = ItemID.WormFood,
            [NPCID.KingSlime] = ItemID.SlimeCrown,
            [NPCID.Retinazer] = ItemID.MechanicalEye,
            [NPCID.SkeletronPrime] = ItemID.MechanicalSkull,
            [NPCID.TheDestroyer] = ItemID.MechanicalWorm,
            [NPCID.QueenBee] = ItemID.Abeemination,
            [NPCID.BrainofCthulhu] = ItemID.BloodySpine,
            [NPCID.DukeFishron] = ItemID.TruffleWorm
        };

        private static readonly int[] TallGrassIds =
        {
            TileID.Plants2, TileID.JunglePlants2, TileID.HallowedPlants2
        };

        private ILookup<int, Item> _bodySlotLookup;
        private Commands _commands;
        private ILookup<short, Item> _createNpcLookup;
        private ILookup<int, Item> _createTileLookup;
        private ILookup<int, Item> _createWallLookup;
        private ILookup<int, Item> _headSlotLookup;
        private bool _infiniteChests;
        private ILookup<int, Item> _legSlotLookup;
        private ILookup<byte, Item> _paintLookup;
        private ILookup<int, Item> _shootLookup;

        public Module([NotNull] NoCheatPlugin plugin) : base(plugin)
        {
        }

        public override void Dispose()
        {
            _commands.Dispose();

            ServerApi.Hooks.GamePostInitialize.Deregister(Plugin, OnGamePostInitialize);
            ServerApi.Hooks.GameUpdate.Deregister(Plugin, OnGameUpdate);
            ServerApi.Hooks.NetGetData.Deregister(Plugin, OnNetGetData);
            ServerApi.Hooks.NetSendData.Deregister(Plugin, OnNetSendData);
            ServerApi.Hooks.ServerLeave.Deregister(Plugin, OnServerLeave);
        }

        public override void Initialize()
        {
            _commands = new Commands();
            _infiniteChests = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.Contains("InfiniteChests"));

            ServerApi.Hooks.GamePostInitialize.Register(Plugin, OnGamePostInitialize);
            ServerApi.Hooks.GameUpdate.Register(Plugin, OnGameUpdate);
            ServerApi.Hooks.NetGetData.Register(Plugin, OnNetGetData, int.MinValue);
            ServerApi.Hooks.NetSendData.Register(Plugin, OnNetSendData, int.MinValue);
            ServerApi.Hooks.ServerLeave.Register(Plugin, OnServerLeave);
        }

        private void OnGamePostInitialize(EventArgs args)
        {
            var items = new List<Item>();
            for (var i = 0; i < Main.maxItemTypes; ++i)
            {
                var item = new Item();
                item.SetDefaults(i);
                items.Add(item);
            }
            _bodySlotLookup = items.Where(i => i.bodySlot > 0).ToLookup(i => i.bodySlot);
            _createNpcLookup = items.Where(i => i.makeNPC > 0).ToLookup(i => i.makeNPC);
            _createTileLookup = items.Where(i => i.createTile >= 0).ToLookup(i => i.createTile);
            _createWallLookup = items.Where(i => i.createWall > 0).ToLookup(i => i.createWall);
            _headSlotLookup = items.Where(i => i.headSlot > 0).ToLookup(i => i.headSlot);
            _legSlotLookup = items.Where(i => i.legSlot > 0).ToLookup(i => i.legSlot);
            _paintLookup = items.Where(i => i.paint > 0).ToLookup(i => i.paint);
            _shootLookup = items.Where(i => i.shoot > 0).ToLookup(i => i.shoot);
        }

        private void OnGameUpdate(EventArgs args)
        {
            foreach (var player in TShock.Players.Where(p => p?.Active == true))
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.Update(Config.Instance);
            }
        }

        private void OnNetGetData(GetDataEventArgs args)
        {
            // Ignore args.Handled for ChestItem if InfiniteChests is active, as InfiniteChests will always handle it.
            if (args.Handled && (args.MsgID != PacketTypes.ChestItem || !_infiniteChests))
            {
                return;
            }

            var player = TShock.Players[args.Msg.whoAmI];
            // Ignore packets sent when the client is syncing.
            if (player.State < 10)
            {
                return;
            }

            using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                switch (args.MsgID)
                {
                    case PacketTypes.PlayerSlot:
                        OnUpdateInventory(player, reader);
                        return;
                    case PacketTypes.PlayerHp:
                        OnUpdatePlayerHp(player, reader);
                        return;
                    case PacketTypes.Tile:
                        OnUpdateTile(player, reader);
                        return;
                    case PacketTypes.ItemDrop:
                    case PacketTypes.UpdateItemDrop:
                        OnUpdateItem(player, reader);
                        return;
                    case PacketTypes.TileSendSquare:
                        OnSendTileSquare(player, reader);
                        return;
                    case PacketTypes.ProjectileNew:
                        OnUpdateProjectile(player, reader);
                        return;
                    case PacketTypes.NpcStrike:
                    case PacketTypes.NpcItemStrike:
                        OnStrikeNpc(player);
                        return;
                    case PacketTypes.ProjectileDestroy:
                        OnRemoveProjectile(player, reader);
                        return;
                    case PacketTypes.ChestItem:
                        OnUpdateChest(player, reader);
                        return;
                    case PacketTypes.NpcTalk:
                        args.Handled = OnTalkNpc(player, reader);
                        return;
                    case PacketTypes.PlayerMana:
                        OnUpdatePlayerMp(player, reader);
                        return;
                    case PacketTypes.ChestUnlock:
                        OnUnlockObject(player, reader);
                        return;
                    case PacketTypes.SpawnBossorInvasion:
                        OnSummon(player, reader);
                        return;
                    case PacketTypes.PaintTile:
                    case PacketTypes.PaintWall:
                        OnPaintTileOrWall(player, reader);
                        return;
                    case PacketTypes.ReleaseNPC:
                        OnReleaseNpc(player, reader);
                        return;
                    case PacketTypes.TeleportationPotion:
                        OnTeleportationPotion(player);
                        return;
                    case PacketTypes.PlaceObject:
                        OnPlaceObject(player, reader);
                        return;
                    case PacketTypes.PlaceItemFrame:
                        OnUpdateItemFrame(player, reader);
                        return;
                    case PacketTypes.NebulaLevelUp:
                        OnUpdateNebula(player, reader);
                        return;
                    case PacketTypes.CrystalInvasionStart:
                        OnStartOldOnesInvasion(player);
                        return;
                }
            }
        }

        private void OnNetSendData(SendDataEventArgs args)
        {
            if (!_infiniteChests || args.Handled || args.MsgId != PacketTypes.ChestItem || args.remoteClient < 0)
            {
                return;
            }

            // If InfiniteChests is installed, we have to monitor SendData so that we know exactly what the client sees.
            var itemIndex = (int)args.number2;
            var item = Main.chest[args.number].item[itemIndex];
            TShock.Players[args.remoteClient].SetChestItem(itemIndex, (NetItem)item);
        }

        private void OnPaintTileOrWall(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            reader.ReadInt16();
            var paint = reader.ReadByte();

            var paintItem = _paintLookup[paint].FirstOrDefault();
            if (paintItem != null)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(paintItem.type, -1);
            }
        }

        private void OnPlaceObject(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            reader.ReadInt16();
            var createObject = reader.ReadInt16();
            var style = reader.ReadInt16();

            var objectItem = _createTileLookup[createObject].FirstOrDefault(i => i.placeStyle == style);
            if (objectItem != null)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(objectItem.type, -1);
            }
        }

        private void OnReleaseNpc(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt32();
            reader.ReadInt32();
            var npcId = reader.ReadInt16();

            var npcItem = _createNpcLookup[npcId].FirstOrDefault();
            if (npcItem != null)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(npcItem.type, -1);
            }
        }

        private void OnRemoveProjectile(TSPlayer player, BinaryReader reader)
        {
            var identity = reader.ReadInt16();

            var projectile = Main.projectile.FirstOrDefault(
                p => p.active && p.identity == identity && p.owner == player.Index);
            if (projectile == null)
            {
                return;
            }

            player.SetDestroyedProjectileId(projectile.type);
            // Handle rope coils, since they can create tiles.
            if (projectile.aiStyle == 35)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(RopeCoilProjectileIdToRopeId[projectile.type], 10);
            }
            // Handle fishing poles being reeled in. Note that we don't actually check what item is on the hook, as that
            // can be done in a separate module.
            else if (projectile.aiStyle == 61)
            {
                var itemId = (int)projectile.ai[1];
                if (itemId <= 0)
                {
                    return;
                }

                var stackSize = 1;
                if (itemId == ItemID.BombFish)
                {
                    stackSize = player.TPlayer.FishingLevel() / 20 + 7;
                }
                else if (itemId == ItemID.FrostDaggerfish)
                {
                    stackSize = player.TPlayer.FishingLevel() / 4 + 31;
                }
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(itemId, stackSize);
            }
        }

        private void OnSendTileSquare(TSPlayer player, BinaryReader reader)
        {
            var size = reader.ReadUInt16();
            if (size != 1)
            {
                return;
            }

            reader.ReadInt16();
            reader.ReadInt16();
            var tile = new NetTile(reader.BaseStream);
            if (tile.Type == TileID.Mannequin || tile.Type == TileID.Womannequin)
            {
                var baseFrameX = tile.FrameX / 100;
                ILookup<int, Item> lookup;
                if (tile.FrameY == 0)
                {
                    lookup = _headSlotLookup;
                }
                else if (tile.FrameY == 18)
                {
                    lookup = _bodySlotLookup;
                }
                else
                {
                    lookup = _legSlotLookup;
                }

                var armorItem = lookup[baseFrameX].FirstOrDefault();
                if (armorItem != null)
                {
                    var balanceSheet = player.GetOrCreateBalanceSheet();
                    balanceSheet.AddTransaction(armorItem.type, -1);
                }
            }
            else if (tile.Type == TileID.WeaponsRack)
            {
                var baseFrameX = tile.FrameX - 5000 * (tile.FrameX / 5000);
                // When placing a weapon an a weapon rack, the client will send two SendTileSquare packets; the first
                // gives the item ID, and the second gives the item prefix. We know which one is the item ID since it is
                // always over 100.
                if (baseFrameX > 100)
                {
                    player.SetWeaponRackItemId(baseFrameX - 100);
                }
                else
                {
                    var balanceSheet = player.GetOrCreateBalanceSheet();
                    balanceSheet.AddTransaction(player.GetWeaponRackItemId(), -1, (byte)baseFrameX);
                }
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
            {
                return;
            }

            // Fast forward the balance sheet if the player leaves. This prevents players from exploiting the delayed
            // nature of the balance sheet.
            var balanceSheet = player.GetOrCreateBalanceSheet();
            var config = new Config
            {
                GracePeriod = TimeSpan.FromSeconds(-1),
                SimplifyingPeriod = TimeSpan.FromSeconds(-1),
                CheckingRecipesPeriod = TimeSpan.FromSeconds(-1),
                CheckingConversionsPeriod = TimeSpan.FromSeconds(-1)
            };
            balanceSheet.Update(config);
            balanceSheet.Update(config);
            balanceSheet.Update(config);
        }

        private void OnStartOldOnesInvasion(TSPlayer player)
        {
            var balanceSheet = player.GetOrCreateBalanceSheet();
            balanceSheet.AddTransaction(ItemID.DD2ElderCrystal, -1);
        }

        private void OnStrikeNpc(TSPlayer player)
        {
            if (player.TPlayer.coins)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.ForgetTransaction(ItemID.CopperCoin, -10_00_00);
            }
            if (player.TPlayer.setNebula)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                foreach (var itemId in NebulaBuffIdToItemId.Values)
                {
                    if (balanceSheet.ForgetTransaction(itemId, -1))
                    {
                        break;
                    }
                }
            }
        }

        private void OnSummon(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            var summonId = reader.ReadInt16();
            if (SummonIdToItemId.TryGetValue(summonId, out var itemId))
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(itemId, -1);
            }
        }

        private bool OnTalkNpc(TSPlayer player, BinaryReader reader)
        {
            reader.ReadByte();
            var npcIndex = reader.ReadInt16();
            var npcId = npcIndex < 0 ? 0 : Main.npc[npcIndex].type;
            if (npcId == NPCID.Guide)
            {
                player.SendData(PacketTypes.NpcTalk, "", player.Index, -1);
                player.SendData(PacketTypes.NpcUpdate, "", npcIndex);
                player.SendWarningMessage("The guide is disabled. Look up recipes online.");
                return true;
            }
            if (npcId == NPCID.TaxCollector)
            {
                player.SendData(PacketTypes.NpcTalk, "", player.Index, -1);
                player.SendData(PacketTypes.NpcUpdate, "", npcIndex);
                player.SendWarningMessage("The tax collector is disabled. Coins are easy enough to get as it is.");
                return true;
            }

            var shopId = NpcIdToShopId.Get(npcId);
            if (shopId == 0)
            {
                return false;
            }

            var shop = new Chest();
            // Set Main.myPlayer so that the shop is populated properly.
            Main.myPlayer = player.Index;
            shop.SetupShop(shopId);
            player.SetShop(shop);

            player.SendSuccessMessage("Shop items:");
            var sb = new StringBuilder();
            for (var i = 0; i < shop.item.Length; ++i)
            {
                var item = shop.item[i];
                if (item.type > 0)
                {
                    sb.Append($"[{i + 1}:[i:{item.type}]] ");
                }
                if ((i + 1) % 10 == 0 && sb.Length > 0)
                {
                    player.SendInfoMessage(sb.ToString());
                    sb.Clear();
                }
            }
            player.SendInfoMessage("Use /npcbuy to buy items and /npcsell to sell your selected item.");
            player.SendData(PacketTypes.NpcTalk, "", player.Index, -1);
            player.SendData(PacketTypes.NpcUpdate, "", npcIndex);

            if (npcId == NPCID.DyeTrader)
            {
                player.SendInfoMessage("Use /dyetrade to trade strange plants for dyes.");
            }
            else if (npcId == NPCID.GoblinTinkerer)
            {
                player.SendInfoMessage("Use /reforge to reforge your selected item.");
            }
            return true;
        }

        private void OnTeleportationPotion(TSPlayer player)
        {
            var balanceSheet = player.GetOrCreateBalanceSheet();
            balanceSheet.AddTransaction(ItemID.TeleportationPotion, -1);
        }

        private void OnUnlockObject(TSPlayer player, BinaryReader reader)
        {
            var unlockType = reader.ReadByte();
            if (unlockType == 1)
            {
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var tile = Main.tile[x, y];
                // Shadow chests don't consume the shadow key.
                if (tile.frameX >= 144 && tile.frameX <= 178)
                {
                    return;
                }

                int keyId = ItemID.GoldenKey;
                if (tile.frameX >= 828 && tile.frameX <= 1006)
                {
                    keyId = ItemID.JungleKey + (tile.frameX / 36 - 23);
                }

                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(keyId, -1);
            }
            else if (unlockType == 2)
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(ItemID.TempleKey, -1);
            }
        }

        private void OnUpdateChest(TSPlayer player, BinaryReader reader)
        {
            var chestIndex = reader.ReadInt16();
            var itemIndex = reader.ReadByte();
            var stackSize = reader.ReadInt16();
            var prefix = reader.ReadByte();
            var itemId = reader.ReadInt16();

            NetItem item;
            if (_infiniteChests)
            {
                item = player.GetChestItem(itemIndex);
                player.SetChestItem(itemIndex, new NetItem(itemId, stackSize, prefix));
            }
            else
            {
                item = (NetItem)Main.chest[chestIndex].item[itemIndex];
            }

            var balanceSheet = player.GetOrCreateBalanceSheet();
            if (item.NetId == itemId)
            {
                balanceSheet.AddTransaction(item.NetId, item.Stack - stackSize, item.PrefixId);
            }
            else
            {
                balanceSheet.AddTransaction(item.NetId, item.Stack, item.PrefixId);
                balanceSheet.AddTransaction(itemId, -stackSize, prefix);
            }
        }

        private void OnUpdateInventory(TSPlayer player, BinaryReader reader)
        {
            reader.ReadByte();
            var slot = reader.ReadByte();
            var stackSize = reader.ReadInt16();
            var prefix = reader.ReadByte();
            var itemId = reader.ReadInt16();

            var tplayer = player.TPlayer;
            var items = tplayer.inventory.Concat(tplayer.armor).Concat(tplayer.dye).Concat(tplayer.miscEquips)
                .Concat(tplayer.miscDyes).Concat(tplayer.bank.item).Concat(tplayer.bank2.item)
                .Concat(new[] {tplayer.trashItem}).Concat(tplayer.bank3.item);
            var item = items.ElementAt(slot);

            var balanceSheet = player.GetOrCreateBalanceSheet();
            if (item.type == itemId)
            {
                balanceSheet.AddTransaction(item.type, item.stack - stackSize, item.prefix);
            }
            else
            {
                // Don't credit the player for a trashed item that gets overriden.
                if (slot != 179 || itemId == 0)
                {
                    balanceSheet.AddTransaction(item.type, item.stack, item.prefix);
                }
                balanceSheet.AddTransaction(itemId, -stackSize, prefix);
            }
        }

        private void OnUpdateItem(TSPlayer player, BinaryReader reader)
        {
            var itemIndex = reader.ReadInt16();
            reader.ReadVector2();
            reader.ReadVector2();
            var stackSize = reader.ReadInt16();
            var prefix = reader.ReadByte();
            reader.ReadByte();
            var itemId = reader.ReadInt16();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            if (itemIndex == Main.maxItems)
            {
                var projectileId = player.GetDestroyedProjectileId();
                // Prevent destroyed projectiles from being treated as item drops.
                if (stackSize == 1 &&
                    (ProjectileIdToItemDropId.Get(projectileId) == itemId ||
                     ProjectileIdsDroppingWoodenArrows.Contains(projectileId) && itemId == ItemID.WoodenArrow))
                {
                    player.SetDestroyedProjectileId(0);
                    return;
                }

                balanceSheet.AddTransaction(itemId, -stackSize, prefix);
            }
            else
            {
                var item = Main.item[itemIndex];
                balanceSheet.AddTransaction(item.type, item.stack - stackSize, item.prefix);
            }
        }

        private void OnUpdateItemFrame(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            reader.ReadInt16();
            var itemId = reader.ReadInt16();
            var prefixId = reader.ReadByte();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            balanceSheet.AddTransaction(itemId, -1, prefixId);
        }

        private void OnUpdateNebula(TSPlayer player, BinaryReader reader)
        {
            reader.ReadByte();
            var buffId = reader.ReadByte();
            reader.ReadVector2();

            if (NebulaBuffIdToItemId.TryGetValue(buffId, out var itemId))
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                // We give a debit of 2 since the player earns a credit of 2 from picking up the nebula booster.
                balanceSheet.AddTransaction(itemId, -2);
            }
        }

        private void OnUpdatePlayerHp(TSPlayer player, BinaryReader reader)
        {
            reader.ReadByte();
            reader.ReadInt16();
            var maxHp = reader.ReadInt16();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            var oldMaxHp = player.TPlayer.statLifeMax;
            while (oldMaxHp < maxHp && oldMaxHp <= 400)
            {
                balanceSheet.AddTransaction(ItemID.LifeCrystal, -1);
                oldMaxHp += 20;
            }
            while (oldMaxHp < maxHp)
            {
                balanceSheet.AddTransaction(ItemID.LifeFruit, -1);
                oldMaxHp += 5;
            }
        }

        private void OnUpdatePlayerMp(TSPlayer player, BinaryReader reader)
        {
            reader.ReadByte();
            reader.ReadInt16();
            var maxMp = reader.ReadInt16();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            var oldMaxMp = player.TPlayer.statManaMax;
            while (oldMaxMp < maxMp)
            {
                balanceSheet.AddTransaction(ItemID.ManaCrystal, -1);
                oldMaxMp += 20;
            }
        }

        private void OnUpdateProjectile(TSPlayer player, BinaryReader reader)
        {
            var identity = reader.ReadInt16();
            reader.ReadVector2();
            reader.ReadVector2();
            reader.ReadSingle();
            reader.ReadInt16();
            reader.ReadByte();
            var projectileId = reader.ReadInt16();

            var projectile = Main.projectile.FirstOrDefault(
                p => p.active && p.identity == identity && p.owner == player.Index);
            if (projectile != null)
            {
                return;
            }

            var projectileItem = _shootLookup[projectileId].FirstOrDefault();
            if (projectileItem != null && projectileItem.consumable)
            {
                var selectedItem = player.SelectedItem;
                // Don't debit the player if there are ammo reductions in place.
                // TODO: create a heuristic for infammo
                if (ReducedAmmoItemIds.Contains(selectedItem.type))
                {
                    return;
                }

                var tplayer = player.TPlayer;
                if (selectedItem.useAmmo == AmmoID.Arrow && tplayer.magicQuiver)
                {
                    return;
                }
                if (projectileItem.ranged &&
                    (tplayer.ammoCost75 || tplayer.ammoCost80 || tplayer.ammoBox || tplayer.ammoPotion))
                {
                    return;
                }
                if (projectileItem.thrown && (tplayer.thrownCost33 || tplayer.thrownCost50))
                {
                    return;
                }

                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(projectileItem.type, -1);
            }
        }

        private void OnUpdateTile(TSPlayer player, BinaryReader reader)
        {
            var action = reader.ReadByte();
            var x = reader.ReadInt16();
            var y = reader.ReadInt16();
            var data = reader.ReadInt16();
            var style = reader.ReadByte();

            switch (action)
            {
                case 0:
                    var tileType = Main.tile[x, y].type;
                    if (player.SelectedItem.type == ItemID.Sickle)
                    {
                        if (GrassIds.Contains(tileType))
                        {
                            var balanceSheet = player.GetOrCreateBalanceSheet();
                            balanceSheet.AddTransaction(ItemID.Hay, 2);
                        }
                        else if (TallGrassIds.Contains(tileType))
                        {
                            var balanceSheet = player.GetOrCreateBalanceSheet();
                            balanceSheet.AddTransaction(ItemID.Hay, 4);
                        }
                    }
                    return;
                case 1:
                    var tileItem = _createTileLookup[data].FirstOrDefault(i => i.placeStyle == style);
                    if (tileItem != null)
                    {
                        var balanceSheet = player.GetOrCreateBalanceSheet();
                        balanceSheet.AddTransaction(tileItem.type, -1);
                    }
                    return;
                case 3:
                    var wallItem = _createWallLookup[data].FirstOrDefault();
                    if (wallItem != null)
                    {
                        var balanceSheet = player.GetOrCreateBalanceSheet();
                        balanceSheet.AddTransaction(wallItem.type, -1);
                    }
                    return;
                case 5:
                case 10:
                case 12:
                case 16:
                {
                    var balanceSheet = player.GetOrCreateBalanceSheet();
                    balanceSheet.AddTransaction(ItemID.Wire, -1);
                    return;
                }
                case 8:
                {
                    var balanceSheet = player.GetOrCreateBalanceSheet();
                    balanceSheet.AddTransaction(ItemID.Actuator, -1);
                    return;
                }
            }
        }
    }
}
