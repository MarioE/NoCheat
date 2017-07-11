using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Provides commands for the ItemSpawning module.
    /// </summary>
    public sealed class Commands : IDisposable
    {
        private static readonly Color[] CoinDenominationColors =
        {
            Colors.CoinPlatinum, Colors.CoinGold, Colors.CoinSilver, Colors.CoinCopper
        };

        private static readonly string[] CoinDenominationNames = {"platinum", "gold", "silver", "copper"};
        private static readonly int[] CoinDenominationStacks = {999, 99, 99, 99};
        private static readonly int[] CoinDenominationValues = {1_00_00_00, 1_00_00, 1_00, 1};

        private static readonly int[] DyeIds =
        {
            ItemID.RedAcidDye, ItemID.BlueAcidDye, ItemID.MushroomDye, ItemID.AcidDye, ItemID.PurpleOozeDye,
            ItemID.ReflectiveDye, ItemID.ReflectiveGoldDye, ItemID.ReflectiveSilverDye, ItemID.ReflectiveObsidianDye,
            ItemID.ReflectiveCopperDye, ItemID.ReflectiveMetalDye, ItemID.NegativeDye, ItemID.MirageDye,
            ItemID.ShadowDye
        };

        private static readonly int[] DyeIdsHardMode =
        {
            ItemID.TwilightDye, ItemID.HadesDye, ItemID.GrimDye, ItemID.BurningHadesDye, ItemID.ShadowflameHadesDye,
            ItemID.PhaseDye, ItemID.ShiftingSandsDye, ItemID.GelDye
        };

        private static readonly int[] DyeIdsMartians = {ItemID.MartianArmorDye, ItemID.MidnightRainbowDye};

        private static readonly int[] DyeIdsMechanicalBoss =
        {
            ItemID.ChlorophyteDye, ItemID.LivingFlameDye, ItemID.LivingOceanDye, ItemID.LivingRainbowDye
        };

        private static readonly int[] DyeIdsMoonlord = {ItemID.DevDye};

        private static readonly int[] DyeIdsPlantera =
        {
            ItemID.WispDye, ItemID.PixieDye, ItemID.UnicornWispDye, ItemID.UnicornWispDye
        };

        private readonly Random _random = new Random();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Commands" /> class, registering the commands.
        /// </summary>
        public Commands()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(DyeTrade, "dyetrade")
            {
                HelpText = $"Syntax: {TShockAPI.Commands.Specifier}dyetrade\n" +
                           "Allows you to trade strange plants for dyes."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(NpcBuy, "npcbuy")
            {
                HelpText = $"Syntax: {TShockAPI.Commands.Specifier}npcbuy <item index> [amount = 1]\n" +
                           "Allows you to buy items from an NPC."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(NpcSell, "npcsell")
            {
                HelpText = $"Syntax: {TShockAPI.Commands.Specifier}sell\n" +
                           "Allows you to sell your selected item to an NPC."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Reforge, "reforge")
            {
                HelpText = $"Syntax: {TShockAPI.Commands.Specifier}reforge\n" +
                           "Allows you to reforge your selected item."
            });
        }

        /// <summary>
        ///     Disposes the commands class, unregistering the commands.
        /// </summary>
        public void Dispose()
        {
            TShockAPI.Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == NpcBuy);
            TShockAPI.Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == DyeTrade);
            TShockAPI.Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == Reforge);
            TShockAPI.Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == NpcSell);
        }

        private static int FindItemIndex(TSPlayer player, Item searchItem)
        {
            for (var i = 0; i < player.TPlayer.inventory.Length; ++i)
            {
                var item = player.TPlayer.inventory[i];
                if (item.type == searchItem.type && item.stack == searchItem.stack && item.prefix == searchItem.prefix)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string GetPriceText(int price, int customCurrencyId = -1)
        {
            if (customCurrencyId != -1)
            {
                var lines = new[] {""};
                var currentLine = 0;
                // GetPriceText depends on Main.mouseTextColor being set to the proper value. We also set Lang.tip[50]
                // to be empty so that "Buy price:" does not appear.
                Main.mouseTextColor = 255;
#pragma warning disable 618
                Lang.tip[50].SetValue("");
#pragma warning restore 618
                CustomCurrencyManager.GetPriceText(customCurrencyId, lines, ref currentLine, price);
                Console.WriteLine(lines);
                return lines[0];
            }

            var tempPrice = price;
            var sb = new StringBuilder();
            for (var i = 0; i < CoinDenominationValues.Length; ++i)
            {
                var amount = tempPrice / CoinDenominationValues[i];
                if (amount > 0 || i == CoinDenominationValues.Length - 1 && price == 0)
                {
                    tempPrice -= amount * CoinDenominationValues[i];
                    sb.Append($"[c/{CoinDenominationColors[i].Hex3()}: {amount} {CoinDenominationNames[i]}]");
                }
            }

            Debug.Assert(!string.IsNullOrWhiteSpace(sb.ToString()), "Text must not be empty.");
            return sb.ToString();
        }

        private static void UpdatePlayerCurrency(TSPlayer player)
        {
            var items = player.GetAllItems().ToList();
            for (var i = 0; i < items.Count; ++i)
            {
                var itemId = items[i].type;
                // We may have to send an update if the item is empty since coins or defender medals may have been there
                // originally.
                if (itemId == 0 || itemId >= ItemID.CopperCoin && itemId <= ItemID.PlatinumCoin ||
                    itemId == ItemID.DefenderMedal)
                {
                    player.SendData(PacketTypes.PlayerSlot, "", player.Index, i);
                }
            }
        }

        private void DyeTrade(CommandArgs args)
        {
            var player = args.Player;
            var tplayer = player.TPlayer;
            var strangePlantIndex = -1;
            for (var i = 0; i < 59; ++i)
            {
                var item = tplayer.inventory[i];
                if (item.stack > 0 && ItemID.Sets.ExoticPlantsForDyeTrade[item.type])
                {
                    --item.stack;
                    player.SendData(PacketTypes.PlayerSlot, "", player.Index, i);
                    strangePlantIndex = i;
                    break;
                }
            }
            if (strangePlantIndex < 0)
            {
                player.SendErrorMessage("You have no strange plants.");
                return;
            }

            var dyeIds = new List<int>(DyeIds);
            if (Main.hardMode)
            {
                dyeIds.AddRange(DyeIdsHardMode);
                if (NPC.downedMechBossAny)
                {
                    dyeIds.AddRange(DyeIdsMechanicalBoss);
                }
                if (NPC.downedPlantBoss)
                {
                    dyeIds.AddRange(DyeIdsPlantera);
                }
                if (NPC.downedMartians)
                {
                    dyeIds.AddRange(DyeIdsMartians);
                }
                if (NPC.downedMoonlord)
                {
                    dyeIds.AddRange(DyeIdsMoonlord);
                }
            }

            var dyeId = dyeIds[_random.Next(dyeIds.Count)];
            player.GiveItem(dyeId, "", tplayer.width, tplayer.height, 3);
            player.SendSuccessMessage("Traded strange plant for dyes.");
        }

        private void NpcBuy(CommandArgs args)
        {
            var parameters = args.Parameters;
            var player = args.Player;
            if (parameters.Count != 1 && parameters.Count != 2)
            {
                player.SendErrorMessage($"Syntax: {TShockAPI.Commands.Specifier}npcbuy <item index> [amount = 1]");
                return;
            }

            var shop = player.GetShop();
            if (shop == null)
            {
                player.SendErrorMessage("You have no shop open. Talk to an NPC.");
                return;
            }

            var inputItemIndex = parameters[0];
            if (!int.TryParse(inputItemIndex, out var itemIndex) || itemIndex <= 0 || itemIndex > shop.item.Length ||
                shop.item[itemIndex - 1].type == 0)
            {
                player.SendErrorMessage($"Invalid item index '{inputItemIndex}'.");
                return;
            }
            var shopItem = shop.item[itemIndex - 1];

            var inputAmount = parameters.Count == 2 ? parameters[1] : "1";
            if (!int.TryParse(inputAmount, out var amount) || amount <= 0 || amount > shopItem.maxStack)
            {
                player.SendErrorMessage($"Invalid amount '{inputAmount}'.");
                return;
            }
            shopItem.stack = amount;

            var tplayer = player.TPlayer;
            // The discount card should only affect coins.
            var price = amount * (int)((tplayer.discount && shopItem.shopSpecialCurrency == -1 ? 0.8 : 1.0) *
                                       shopItem.GetStoreValue());
            var priceText = GetPriceText(price, shopItem.shopSpecialCurrency);

            player.SendInfoMessage($"Purchase cost of {shopItem.GetColoredName()}:{priceText}");
            player.SendInfoMessage("Do you wish to proceed? Type /yes or /no.");
            player.AddResponse("yes", args2 =>
            {
                player.AwaitingResponse.Remove("no");

                if (!tplayer.BuyItem(price, shopItem.shopSpecialCurrency))
                {
                    player.SendErrorMessage("You don't have enough money.");
                    return;
                }

                UpdatePlayerCurrency(player);

                player.GiveItem(shopItem.type, "", tplayer.width, tplayer.height, amount, -1);
                player.SendSuccessMessage($"Purchased {shopItem.GetColoredName()}.");
            });
            player.AddResponse("no", args2 =>
            {
                player.AwaitingResponse.Remove("yes");
                player.SendSuccessMessage("Canceled purchase.");
            });
        }

        private void NpcSell(CommandArgs args)
        {
            var player = args.Player;
            var shop = player.GetShop();
            if (shop == null)
            {
                player.SendErrorMessage("You have no shop open. Talk to an NPC.");
                return;
            }

            var selectedItem = player.SelectedItem;
            if (selectedItem.type == 0 ||
                selectedItem.type >= ItemID.CopperCoin && selectedItem.type <= ItemID.PlatinumCoin)
            {
                player.SendErrorMessage("Your selected item cannot be sold.");
                return;
            }

            var tplayer = player.TPlayer;
            var price = selectedItem.stack * (selectedItem.value > 0 ? Math.Max(1, selectedItem.value / 5) : 0);
            var priceText = GetPriceText(price);

            player.SendInfoMessage($"Sell price of {selectedItem.GetColoredName()}:{priceText}");
            player.SendInfoMessage("Do you wish to proceed? Type /yes or /no.");
            player.AddResponse("yes", args2 =>
            {
                player.AwaitingResponse.Remove("no");

                // Find the selected item again, because the player may have moved it around before typing /yes.
                var itemIndex = FindItemIndex(player, selectedItem);
                if (itemIndex < 0)
                {
                    player.SendErrorMessage("Could not find the item to sell. Did it leave your inventory?");
                    return;
                }

                for (var i = 0; i < CoinDenominationValues.Length; ++i)
                {
                    while (price >= CoinDenominationValues[i])
                    {
                        var amount = Math.Min(CoinDenominationStacks[i], price / CoinDenominationValues[i]);
                        Debug.Assert(amount > 0, "Amount must be positive.");
                        price -= amount * CoinDenominationValues[i];
                        player.GiveItem(ItemID.PlatinumCoin - i, "", tplayer.width, tplayer.height, amount);
                    }
                }

                var saleItem = tplayer.inventory[itemIndex];
                player.SendSuccessMessage($"Sold {saleItem.GetColoredName()}.");
                saleItem.SetDefaults();
                player.SendData(PacketTypes.PlayerSlot, "", player.Index, itemIndex);
            });
            player.AddResponse("no", args2 =>
            {
                player.AwaitingResponse.Remove("yes");
                player.SendSuccessMessage("Canceled sale.");
            });
        }

        private void Reforge(CommandArgs args)
        {
            var player = args.Player;
            var selectedItem = player.SelectedItem;
            if (!selectedItem.Prefix(-3))
            {
                player.SendErrorMessage("Your selected item cannot be reforged.");
                return;
            }

            var tplayer = player.TPlayer;
            var price = (int)((tplayer.discount ? 0.8 : 1.0) * selectedItem.value) / 3;
            var priceText = GetPriceText(price);

            player.SendInfoMessage($"Reforge cost of {selectedItem.GetColoredName()}:{priceText}");
            player.SendInfoMessage("Do you wish to proceed? Type /yes or /no.");
            player.AddResponse("yes", args2 =>
            {
                player.AwaitingResponse.Remove("no");

                // Find the selected item again, because the player may have moved it around before typing /yes.
                var itemIndex = FindItemIndex(player, selectedItem);
                if (itemIndex < 0)
                {
                    player.SendErrorMessage("Could not find the item to reforge. Did it leave your inventory?");
                    return;
                }

                if (!tplayer.BuyItem(price))
                {
                    player.SendErrorMessage("You don't have enough money.");
                    return;
                }

                UpdatePlayerCurrency(player);

                var reforgeItem = tplayer.inventory[itemIndex];
                reforgeItem.SetDefaults(reforgeItem.type);
                reforgeItem.Prefix(-2);
                player.SendSuccessMessage($"Reforged item into {reforgeItem.GetColoredName()}.");
                player.SendData(PacketTypes.PlayerSlot, "", player.Index, itemIndex, reforgeItem.prefix);
            });
            player.AddResponse("no", args2 =>
            {
                player.AwaitingResponse.Remove("yes");
                player.SendSuccessMessage("Canceled reforge.");
            });
        }
    }
}
