using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents the module that prevents item spawning.
    /// </summary>
    public sealed class Module : NoCheatModule
    {
        private const string LogMessage = "[NoCheat] {0} spawned {1} x{2}.";
        private const string ReasonMessage = "spawning {0} x{1}";
        private const string WarningMessage = "You spawned {0} x{1}.";

        /// <summary>
        ///     The list of item drop IDs to ignore.
        /// </summary>
        private readonly List<int> _itemDropIdsToIgnore = new List<int>
        {
            ItemID.Heart,
            ItemID.Star,
            ItemID.CandyApple,
            ItemID.SoulCake,
            ItemID.CandyCane,
            ItemID.SugarPlum,
            ItemID.NebulaPickup1,
            ItemID.NebulaPickup2,
            ItemID.NebulaPickup3
        };

        /// <summary>
        ///     The list of items.
        /// </summary>
        private readonly List<Item> _items = new List<Item>();

        /// <summary>
        ///     The mapping from NPC IDs to shop IDs. This is used to set up shops whenever a player talks to an NPC, ensuring that
        ///     they only buy valid items.
        /// </summary>
        private readonly Dictionary<int, int> _npcIdToShopId = new Dictionary<int, int>
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

        /// <summary>
        ///     The mapping from projectile IDs to item drop IDs. This is used to credit a player for destroying a projectile that
        ///     drops items.
        /// </summary>
        private readonly Dictionary<int, int> _projectileIdToItemDropId = new Dictionary<int, int>
        {
            [ProjectileID.Shuriken] = ItemID.Shuriken,
            [ProjectileID.UnholyArrow] = ItemID.UnholyArrow,
            [ProjectileID.FallingStar] = ItemID.FallenStar,
            [ProjectileID.Bone] = ItemID.Bone,
            [ProjectileID.ThrowingKnife] = ItemID.ThrowingKnife,
            [ProjectileID.Glowstick] = ItemID.Glowstick,
            [ProjectileID.StickyGlowstick] = ItemID.StickyGlowstick,
            [ProjectileID.PoisonedKnife] = ItemID.PoisonedKnife,
            [ProjectileID.HolyArrow] = ItemID.HolyArrow,
            [ProjectileID.BeachBall] = ItemID.BeachBall,
            [ProjectileID.BouncyGlowstick] = ItemID.BouncyGlowstick,
            [ProjectileID.BoneJavelin] = ItemID.BoneJavelin,
            [ProjectileID.BoneDagger] = ItemID.BoneDagger
        };

        /// <summary>
        ///     The mapping from summon IDs to item IDs. This is used to debit a player for the relevant item when summoning a boss
        ///     or invasion.
        /// </summary>
        private readonly Dictionary<int, int> _summonIdToItemId = new Dictionary<int, int>
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

        private ILookup<int, Item> _buffLookup;
        private ILookup<int, Item> _createTileLookup;
        private ILookup<int, Item> _createWallLookup;
        private bool _infiniteChests;
        private ILookup<byte, Item> _paintLookup;
        private ILookup<int, Item> _shootLookup;

        public Module([NotNull] NoCheatPlugin plugin) : base(plugin)
        {
        }

        public override void Dispose()
        {
            ServerApi.Hooks.GamePostInitialize.Deregister(Plugin, OnGamePostInitialize);
            ServerApi.Hooks.GameUpdate.Deregister(Plugin, OnGameUpdate);
            ServerApi.Hooks.NetGetData.Deregister(Plugin, OnNetGetData);
            ServerApi.Hooks.NetSendData.Deregister(Plugin, OnNetSendData);
            ServerApi.Hooks.ServerLeave.Deregister(Plugin, OnServerLeave);
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(Plugin, OnGamePostInitialize);
            ServerApi.Hooks.GameUpdate.Register(Plugin, OnGameUpdate);
            ServerApi.Hooks.NetGetData.Register(Plugin, OnNetGetData, int.MinValue);
            ServerApi.Hooks.NetSendData.Register(Plugin, OnNetSendData, int.MinValue);
            ServerApi.Hooks.ServerLeave.Register(Plugin, OnServerLeave);
        }

        private void HandleInvalidDebits(TSPlayer player)
        {
            var session = player.GetOrCreateSession();
            var config = Config.Instance;
            foreach (var debit in player.GetOrCreateBalanceSheet().ConsumeInvalidDebits())
            {
                Debug.Assert(debit.StackSize < 0, "Invalid debit stack size must be negative.");

                var itemName = _items[debit.ItemId].Name;
                TShock.Log.ConsoleInfo(LogMessage, player.Name, itemName, -debit.StackSize);
                player.SendWarningMessage(WarningMessage, itemName, -debit.StackSize);

                var points = config.PointOverrides.Get(debit.ItemId, config.Points.Get(_items[debit.ItemId].rare));
                var reason = string.Format(ReasonMessage, itemName, -debit.StackSize);
                session.AddInfraction(-debit.StackSize * points, config.Duration, reason);
            }
        }

        private void OnGamePostInitialize(EventArgs args)
        {
            for (var i = 0; i < Main.maxItemTypes; ++i)
            {
                var item = new Item();
                item.SetDefaults(i);
                _items.Add(item);
            }
            _buffLookup = _items.Where(i => i.buffType > 0).ToLookup(i => i.buffType);
            _createTileLookup = _items.Where(i => i.createTile >= 0).ToLookup(i => i.createTile);
            _createWallLookup = _items.Where(i => i.createWall > 0).ToLookup(i => i.createWall);
            _paintLookup = _items.Where(i => i.paint > 0).ToLookup(i => i.paint);
            _shootLookup = _items.Where(i => i.shoot > 0).ToLookup(i => i.shoot);

            _infiniteChests = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.Contains("InfiniteChests"));
        }

        private void OnGameUpdate(EventArgs args)
        {
            foreach (var player in TShock.Players.Where(p => p?.Active == true))
            {
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.Update(Config.Instance.StageDurations);
                HandleInvalidDebits(player);
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
                // TODO: handle weapon rack placement
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
                    case PacketTypes.ProjectileNew:
                        OnUpdateProjectile(player, reader);
                        return;
                    case PacketTypes.NpcStrike:
                        OnStrikeNpc(player);
                        return;
                    case PacketTypes.ProjectileDestroy:
                        OnRemoveProjectile(player, reader);
                        return;
                    case PacketTypes.ChestItem:
                        OnUpdateChest(player, reader);
                        return;
                    case PacketTypes.NpcTalk:
                        OnTalkNpc(player, reader);
                        return;
                    case PacketTypes.PlayerMana:
                        OnUpdatePlayerMp(player, reader);
                        return;
                    case PacketTypes.LiquidSet:
                        OnUpdateLiquid(player, reader);
                        return;
                    case PacketTypes.PlayerBuff:
                        OnUpdatePlayerBuffs(player, reader);
                        return;
                    case PacketTypes.SpawnBossorInvasion:
                        OnSummon(player, reader);
                        return;
                    case PacketTypes.PaintTile:
                    case PacketTypes.PaintWall:
                        OnPaintTileOrWall(player, reader);
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
                }
            }
        }

        private void OnNetSendData(SendDataEventArgs args)
        {
            if (args.Handled || args.MsgId != PacketTypes.ChestItem || args.remoteClient < 0)
            {
                return;
            }

            // We use NetSendData to detect chest items sent to players. This is the most plugin-agnostic way of
            // determining the items in a chest shown to a player.
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
                // Debit the player for the item that corresponds to the paint.
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(new Transaction(paintItem.type, -1));
            }
        }

        private void OnPlaceObject(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            reader.ReadInt16();
            var createObject = reader.ReadInt16();
            var placeStyle = reader.ReadInt16();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            var objectItem = _createTileLookup[createObject].FirstOrDefault(i => i.placeStyle == placeStyle);
            if (objectItem != null)
            {
                // Debit the player for the item that corresponds to the object.
                balanceSheet.AddTransaction(new Transaction(objectItem.type, -1));
            }
        }

        private void OnRemoveProjectile(TSPlayer player, BinaryReader reader)
        {
            var identity = reader.ReadInt16();

            var projectile =
                Main.projectile.FirstOrDefault(p => p.active && p.identity == identity && p.owner == player.Index);
            if (projectile != null && _projectileIdToItemDropId.TryGetValue(projectile.type, out var itemId))
            {
                // Credit the player for the item that can be created when the projectile is removed.
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(new Transaction(itemId));
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
            {
                return;
            }

            // Fast forward the balance sheet if the player leaves. This prevents players from joining, spawning an
            // item, and then quickly leaving.
            var balanceSheet = player.GetOrCreateBalanceSheet();
            var stageDurations = Config.Instance.StageDurations.Select(ts => new TimeSpan(-ts.Ticks)).ToArray();
            balanceSheet.Update(stageDurations);
            balanceSheet.Update(stageDurations);
            balanceSheet.Update(stageDurations);
            balanceSheet.Update(stageDurations);

            HandleInvalidDebits(player);
        }

        private void OnStrikeNpc(TSPlayer player)
        {
            var balanceSheet = player.GetOrCreateBalanceSheet();
            var tplayer = player.TPlayer;
            if (tplayer.coins)
            {
                // Credit the player for coins spawned using the lucky coin.
                balanceSheet.AddTransaction(new Transaction(ItemID.CopperCoin, 10_00_00));
            }
            if (tplayer.setNebula)
            {
                // Credit the player for nebula pickups spawned.
                balanceSheet.AddTransaction(new Transaction(ItemID.NebulaPickup1));
                balanceSheet.AddTransaction(new Transaction(ItemID.NebulaPickup2));
                balanceSheet.AddTransaction(new Transaction(ItemID.NebulaPickup3));
            }
        }

        private void OnSummon(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            var summonId = reader.ReadInt16();
            if (_summonIdToItemId.TryGetValue(summonId, out var itemId))
            {
                // Debit the player for the item used to summon the boss or invasion.
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(new Transaction(itemId, -1));
            }
        }

        private void OnTalkNpc(TSPlayer player, BinaryReader reader)
        {
            reader.ReadByte();
            var npcIndex = reader.ReadInt16();
            // If the player hasn't switched to a different NPC, don't bother setting the active shop.
            if (npcIndex == player.TPlayer.talkNPC)
            {
                return;
            }

            var shopId = npcIndex < 0 ? 0 : _npcIdToShopId.Get(Main.npc[npcIndex].type);
            if (shopId == 0)
            {
                player.SetActiveShop(null);
            }
            else
            {
                var shop = new Chest();
                // Set Main.myPlayer so that the shop is populated based on the correct player.
                Main.myPlayer = player.Index;
                shop.SetupShop(shopId);
                player.SetActiveShop(shop);
            }

            // Clear the sold items, since the player no longer has access to the old shop.
            player.SetSoldItems(new List<NetItem>());
        }

        private void OnTeleportationPotion(TSPlayer player)
        {
            // Debit the player for the teleportation potion that.
            var balanceSheet = player.GetOrCreateBalanceSheet();
            balanceSheet.AddTransaction(new Transaction(ItemID.TeleportationPotion, -1));
        }

        private void OnUpdateChest(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            var itemIndex = reader.ReadByte();
            var stackSize = reader.ReadInt16();
            var prefix = reader.ReadByte();
            var itemId = reader.ReadInt16();
            var item = player.GetChestItem(itemIndex);

            var balanceSheet = player.GetOrCreateBalanceSheet();
            if (item.NetId == itemId)
            {
                balanceSheet.AddTransaction(new Transaction(item.NetId, item.Stack - stackSize, item.PrefixId));
            }
            else
            {
                balanceSheet.AddTransaction(new Transaction(item.NetId, item.Stack, item.PrefixId));
                balanceSheet.AddTransaction(new Transaction(itemId, -stackSize, prefix));
            }

            player.SetChestItem(itemIndex, new NetItem(itemId, stackSize, prefix));
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
            var activeShop = player.GetActiveShop();
            var soldItems = player.GetSoldItems();
            if (item.type == itemId)
            {
                balanceSheet.AddTransaction(new Transaction(item.type, item.stack - stackSize, item.prefix)
                {
                    ActiveShop = activeShop,
                    SoldItems = soldItems
                });
            }
            else
            {
                balanceSheet.AddTransaction(new Transaction(item.type, item.stack, item.prefix)
                {
                    ActiveShop = activeShop,
                    SoldItems = soldItems
                });
                balanceSheet.AddTransaction(new Transaction(itemId, -stackSize, prefix)
                {
                    ActiveShop = activeShop,
                    SoldItems = soldItems
                });
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
                if (!_itemDropIdsToIgnore.Contains(itemId))
                {
                    balanceSheet.AddTransaction(new Transaction(itemId, -stackSize, prefix));
                }
            }
            else
            {
                var item = Main.item[itemIndex];
                balanceSheet.AddTransaction(new Transaction(item.type, item.stack - stackSize, item.prefix));
            }
        }

        private void OnUpdateItemFrame(TSPlayer player, BinaryReader reader)
        {
            reader.ReadInt16();
            reader.ReadInt16();
            var itemId = reader.ReadInt16();
            var prefix = reader.ReadByte();

            // Debit the player for the item placed in the item frame.
            var balanceSheet = player.GetOrCreateBalanceSheet();
            balanceSheet.AddTransaction(new Transaction(itemId, -1, prefix));
        }

        private void OnUpdateLiquid(TSPlayer player, BinaryReader reader)
        {
            var x = reader.ReadInt16();
            var y = reader.ReadInt16();
            var liquid = reader.ReadByte();
            var liquidType = reader.ReadByte();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            var tile = Main.tile[x, y];
            if (liquid < tile.liquid)
            {
                // If the liquid level went down, credit the player for the relevant liquid bucket. We can't debit the
                // player for an empty bucket, however, as the client is unreliable in sending liquid updates.
                switch (liquidType)
                {
                    case 0:
                        balanceSheet.AddTransaction(new Transaction(ItemID.WaterBucket));
                        break;
                    case 1:
                        balanceSheet.AddTransaction(new Transaction(ItemID.LavaBucket));
                        break;
                    case 2:
                        balanceSheet.AddTransaction(new Transaction(ItemID.HoneyBucket));
                        break;
                }
            }
            else if (liquid > tile.liquid)
            {
                // If the liquid level went up, credit the player for an empty bucket. We can't debit the player for the
                // relevant liquid bucket, however, as the client is unreliable in sending liquid updates.
                balanceSheet.AddTransaction(new Transaction(ItemID.EmptyBucket));
            }
        }

        private void OnUpdatePlayerBuffs(TSPlayer player, BinaryReader reader)
        {
            var balanceSheet = player.GetOrCreateBalanceSheet();
            for (var i = 0; i < Player.maxBuffs; ++i)
            {
                var buffType = reader.ReadByte();
                if (player.TPlayer.FindBuffIndex(buffType) >= 0)
                {
                    continue;
                }

                var buffItems = _buffLookup[buffType].ToList();
                if (buffItems.Count == 1)
                {
                    // Debit the player for the item that creates the buff, if it is unique.
                    balanceSheet.AddTransaction(new Transaction(buffItems[0].type, -1));
                }
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
                // Debit the player for each life crystal consumed.
                balanceSheet.AddTransaction(new Transaction(ItemID.LifeCrystal, -1));
                oldMaxHp += 20;
            }
            while (oldMaxHp < maxHp)
            {
                // Debit the player for each life fruit consumed.
                balanceSheet.AddTransaction(new Transaction(ItemID.LifeFruit, -1));
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
                // Debit the player for each mana crystal consumed.
                balanceSheet.AddTransaction(new Transaction(ItemID.ManaCrystal, -1));
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

            var projectile =
                Main.projectile.FirstOrDefault(p => p.active && p.identity == identity && p.owner == player.Index);
            if (projectile != null)
            {
                return;
            }

            var projectileItem = _shootLookup[projectileId].FirstOrDefault();
            if (projectileItem != null && projectileItem.ammo == 0 && projectileItem.consumable)
            {
                // Debit the player for the item that creates the projectile. We don't bother checking for ammo, since
                // ammo consumption can be reduced.
                // TODO: consider heuristic based approach? Probably not worth it.
                var balanceSheet = player.GetOrCreateBalanceSheet();
                balanceSheet.AddTransaction(new Transaction(projectileItem.type, -1));
            }
        }

        private void OnUpdateTile(TSPlayer player, BinaryReader reader)
        {
            var action = reader.ReadByte();
            reader.ReadInt16();
            reader.ReadInt16();

            var balanceSheet = player.GetOrCreateBalanceSheet();
            switch (action)
            {
                case 0:
                    if (player.SelectedItem.type == ItemID.Sickle)
                    {
                        // Credit the player for hay, since the player spawns it when mowing grass with the sickle.
                        balanceSheet.AddTransaction(new Transaction(ItemID.Hay, 4));
                    }
                    return;
                case 1:
                    var createTile = reader.ReadInt16();
                    var placeStyle = reader.ReadByte();
                    var tileItem = _createTileLookup[createTile].FirstOrDefault(i => i.placeStyle == placeStyle);
                    if (tileItem != null)
                    {
                        // Debit the player for the item that corresponds to the tile.
                        balanceSheet.AddTransaction(new Transaction(tileItem.type, -1));
                    }
                    return;
                case 3:
                    var createWall = reader.ReadInt16();
                    var wallItem = _createWallLookup[createWall].FirstOrDefault();
                    if (wallItem != null)
                    {
                        // Debit the player for the item that corresponds to the wall.
                        balanceSheet.AddTransaction(new Transaction(wallItem.type, -1));
                    }
                    return;
                case 5:
                case 10:
                case 12:
                case 16:
                    // Debit the player for a wire.
                    balanceSheet.AddTransaction(new Transaction(ItemID.Wire, -1));
                    return;
                case 8:
                    // Debit the player for an actuator.
                    balanceSheet.AddTransaction(new Transaction(ItemID.Actuator, -1));
                    return;
            }
        }
    }
}
