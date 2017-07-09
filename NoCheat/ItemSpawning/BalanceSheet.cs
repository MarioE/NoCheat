using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Provides a thread-safe item balance sheet for a player.
    /// </summary>
    // TODO: handle 5x defender medals for first time
    public sealed class BalanceSheet
    {
        private static Dictionary<int, LootDrop> _lootDrops;
        private static ILookup<int, Recipe> _recipeLookup;

        private readonly List<Transaction> _credits = new List<Transaction>();
        private readonly List<Transaction> _debits = new List<Transaction>();
        private readonly List<Transaction> _invalidDebits = new List<Transaction>();
        private readonly object _lock = new object();
        private readonly TSPlayer _player;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BalanceSheet" /> class for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        public BalanceSheet([NotNull] TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            _player = player;
        }

        /// <summary>
        ///     Adds the specified transaction.
        /// </summary>
        /// <param name="transaction">The transaction, which must not be <c>null</c>.</param>
        public void AddTransaction([NotNull] Transaction transaction)
        {
            Debug.Assert(transaction != null, "Transaction must not be null.");

            if (transaction.ItemId == 0 || transaction.StackSize == 0)
            {
                return;
            }

            // Convert coins into copper coins. This simplifies the logic significantly.
            while (ItemID.SilverCoin <= transaction.ItemId && transaction.ItemId <= ItemID.PlatinumCoin)
            {
                --transaction.ItemId;
                transaction.StackSize *= 100;
            }
            lock (_lock)
            {
                if (transaction.StackSize > 0)
                {
                    _credits.Add(transaction);
                }
                else
                {
                    _debits.Add(transaction);
                }
            }
        }

        /// <summary>
        ///     Consumes the invalid debits and returns them.
        /// </summary>
        /// <returns>The invalid debits.</returns>
        [ItemNotNull]
        [NotNull]
        public IEnumerable<Transaction> ConsumeInvalidDebits()
        {
            lock (_lock)
            {
                var result = new List<Transaction>(_invalidDebits);
                _invalidDebits.Clear();
                return result;
            }
        }

        /// <summary>
        ///     Updates the balance sheet using the supplied stage durations.
        /// </summary>
        /// <param name="stageDurations">The durations for each stage.</param>
        public void Update([NotNull] TimeSpan[] stageDurations)
        {
            Debug.Assert(stageDurations != null, "Stage durations must not be null.");

            lock (_lock)
            {
                ProcessPipeline(stageDurations);
                ProcessStage1();
                ProcessStage2();
                ProcessStage3();
                ProcessStage4();
            }
        }

        private IList<Transaction> GetCredits(int stage) => _credits.Where(c => c.Stage == stage).ToList();
        private IList<Transaction> GetDebits(int stage) => _debits.Where(c => c.Stage == stage).ToList();

        /// <summary>
        ///     Processes the pipeline.
        /// </summary>
        private void ProcessPipeline(IReadOnlyList<TimeSpan> stageDurations)
        {
            // Update the transactions' stages.
            foreach (var transaction in _credits.Concat(_debits).Where(t => t.Stage <= stageDurations.Count))
            {
                transaction.UpdateStage(stageDurations[transaction.Stage - 1]);
            }
            _credits.RemoveAll(c => c.StackSize <= 0 || c.Stage > stageDurations.Count);
            _invalidDebits.AddRange(_debits.Where(d => d.StackSize < 0 && d.Stage > stageDurations.Count));
            _debits.RemoveAll(d => d.StackSize >= 0 || d.Stage > stageDurations.Count);
        }

        /// <summary>
        ///     Processes stage 1 credits and debits. In this stage, item movement will be handled.
        /// </summary>
        private void ProcessStage1()
        {
            var credits = GetCredits(1);
            var debits = GetDebits(1);
            // Simplify credits and debits. For example, a credit of 2 dirt blocks and a debit of 3 dirt blocks will be
            // simplified into a debit of 1 dirt block.
            foreach (var debit in debits)
            {
                foreach (var credit in credits.Where(c => c.ItemId == debit.ItemId))
                {
                    var payment = Math.Min(credit.StackSize, -debit.StackSize);
                    credit.StackSize -= payment;
                    debit.StackSize += payment;

                    // Stop if the debit has been cleared out so we don't unnecessarily check more credits.
                    if (debit.StackSize >= 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Processes stage 2 credits and debits. In this stage, recipes will be handled.
        /// </summary>
        private void ProcessStage2()
        {
            var credits = GetCredits(2);
            var debits = GetDebits(2);

            bool ProcessDebitWithRecipe(Transaction debit, Recipe recipe)
            {
                // Create a pending table of payments. If the recipe turns out to not be possible, then no payments will
                // end up occurring.
                var payments = new Dictionary<Transaction, int>();
                foreach (var item in recipe.requiredItem)
                {
                    var stackLeft = item.stack;
                    var ingredientIds = recipe.GetSubstituteIds(item.type).ToList();
                    foreach (var credit in credits.Where(c => ingredientIds.Contains(c.ItemId)))
                    {
                        var payment = Math.Min(credit.StackSize, stackLeft);
                        stackLeft -= payment;
                        payments[credit] = payment;

                        // Stop if the ingredient has been cleared out so we don't unnecessarily check more credits.
                        if (stackLeft <= 0)
                        {
                            break;
                        }
                    }

                    if (stackLeft > 0)
                    {
                        return false;
                    }
                }

                debit.StackSize += recipe.createItem.stack;
                // Don't pay for alchemy recipes if the player is at an alchemy table, as the alchemy table provides
                // a 33% chance of not using each ingredient.
                // TODO: consider heuristic based approach? Probably not worth it.
                if (!recipe.alchemy || !_player.TPlayer.alchemyTable)
                {
                    foreach (var kvp in payments)
                    {
                        kvp.Key.StackSize -= kvp.Value;
                    }
                }
                return true;
            }

            // Lazily initialize the recipe lookup table. This table provides a mapping from item IDs to recipes that
            // create those item IDs.
            if (_recipeLookup == null)
            {
                _recipeLookup = Main.recipe.ToLookup(r => r.createItem.type);
            }

            foreach (var debit in debits)
            {
                foreach (var recipe in _recipeLookup[debit.ItemId])
                {
                    while (debit.StackSize < 0 && ProcessDebitWithRecipe(debit, recipe))
                    {
                    }

                    // Stop if the debit has been cleared out so we don't unnecessarily check more recipes.
                    if (debit.StackSize >= 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Processes stage 3 credits and debits. In this stage, item conversions will be handled.
        /// </summary>
        private void ProcessStage3()
        {
            // Lazily initialize the loot drops table. This table provides relevant conversions from certain items to
            // groups of items.
            if (_lootDrops == null)
            {
                _lootDrops = new Dictionary<int, LootDrop>();
                var item = new Item();
                for (var i = 0; i < Main.maxItemTypes; ++i)
                {
                    if (LootDrop.GrabBags.TryGetValue(i, out var lootDrop))
                    {
                        // Grab bags can turn into loot.
                        _lootDrops[i] = lootDrop;
                    }
                    else if (ItemID.Sets.ExoticPlantsForDyeTrade[i])
                    {
                        // Strange plants can turn into dye trader rewards.
                        // TODO: maybe check if the player is talking to the dye trader.
                        _lootDrops[i] = LootDrop.DyeTraderRewards;
                    }
                    else if (ItemID.Sets.ExtractinatorMode[i] >= 0)
                    {
                        // Silt, slush, and desert fossils can turn into extractinator drops.
                        // TODO: maybe check if the player is within range of an extractinator.
                        _lootDrops[i] = LootDrop.ExtractinatorDrops;
                    }
                    else
                    {
                        item.SetDefaults(i);
                        if (item.questItem)
                        {
                            // Quest fish can turn into angler rewards.
                            // TODO: maybe check if the player is actually able to turn in the quest fish.
                            _lootDrops[i] = LootDrop.AnglerRewards;
                        }
                        else if (item.bait > 0)
                        {
                            // Bait can turn into fishing drops.
                            // TODO: maybe check if the player is actually fishing.
                            _lootDrops[i] = LootDrop.FishingDrops;
                        }
                    }
                }
            }

            var credits = GetCredits(3);
            // We need to consider stage 2 debits along with stage 3 debits. In a conversion, the player spawns the
            // item, and then picks up the item. The packets for picking up the spawned item may occur after the
            // packet for updating the converted item; therefore, there may be debits that are still stuck in stage 2.
            var debits = GetDebits(2).Concat(GetDebits(3)).ToList();
            foreach (var credit in credits.Where(c => _lootDrops.ContainsKey(c.ItemId)))
            {
                var lootDrop = _lootDrops[credit.ItemId];
                while (credit.StackSize > 0 && lootDrop.IsContainedIn(debits))
                {
                    lootDrop.Apply(debits);
                    --credit.StackSize;
                }
            }
        }

        /// <summary>
        ///     Processes stage 4 credits and debits. In this stage, item purchases and sales will be handled.
        /// </summary>
        private void ProcessStage4()
        {
            var credits = GetCredits(4);
            var debits = GetDebits(4);
            // Treat credits of non-currency items with an active shop as sales.
            foreach (var credit in credits.Where(
                c => c.ActiveShop != null && c.ItemId != ItemID.CopperCoin && c.ItemId != ItemID.DefenderMedal))
            {
                var item = new Item();
                item.SetDefaults(credit.ItemId);
                item.Prefix(credit.Prefix);

                var coinPayment = credit.StackSize * Math.Max(1, item.GetStoreValue() / 5);
                // Clear out coin debits wherever possible.
                foreach (var debit in debits.Where(d => d.ItemId == ItemID.CopperCoin))
                {
                    var payment = Math.Min(coinPayment, -debit.StackSize);
                    coinPayment -= payment;
                    debit.StackSize += payment;

                    // Stop if the coins have been cleared out so we don't unnecessarily check more debits.
                    if (coinPayment <= 0)
                    {
                        break;
                    }
                }

                credit.SoldItems.Add(new NetItem(credit.ItemId, credit.StackSize, credit.Prefix));
                credit.StackSize = 0;
                // If not all of the coins were used, we need to add the remainder as a credit. This is because the
                // player may have quickly purchased an item of equivalent value.
                if (coinPayment > 0)
                {
                    AddTransaction(new Transaction(ItemID.CopperCoin, coinPayment));
                }
            }

            // Treat debits of non-currency items with an active shop as purchases.
            foreach (var debit in debits.Where(
                d => d.ActiveShop != null && d.ItemId != ItemID.CopperCoin && d.ItemId != ItemID.DefenderMedal))
            {
                // Items can be purchased using defender medals, so we have to support both types of currencies here.
                var currencyDebit = new Dictionary<int, int>
                {
                    [ItemID.CopperCoin] = 0,
                    [ItemID.DefenderMedal] = 0
                };

                // First check previously-sold items. We can't tell if a player purchased from a designated shop slot
                // or previously-sold items, but we give the player the benefit of the doubt since purchasing a
                // previously-sold item requires less money.
                var soldItems = debit.SoldItems;
                for (var i = 0; i < soldItems.Count; ++i)
                {
                    var soldItem = soldItems[i];
                    if (soldItem.NetId != debit.ItemId)
                    {
                        continue;
                    }

                    var payment = Math.Min(soldItem.Stack, -debit.StackSize);
                    soldItems[i] = new NetItem(soldItem.NetId, soldItem.Stack - payment, soldItem.PrefixId);
                    debit.StackSize += payment;

                    var item = new Item();
                    item.SetDefaults(soldItem.NetId);
                    item.Prefix(soldItem.PrefixId);
                    // The purchase must have occurred using coins.
                    currencyDebit[ItemID.CopperCoin] -= payment * Math.Max(1, item.GetStoreValue() / 5);

                    // Stop if the debit has been cleared out so we don't unnecessarily check more sold items.
                    if (debit.StackSize >= 0)
                    {
                        break;
                    }
                }
                soldItems.RemoveAll(si => si.Stack <= 0);

                // Finally, check designated shop slots.
                var shopItem = debit.ActiveShop.item.FirstOrDefault(i => i.type == debit.ItemId);
                if (shopItem != null)
                {
                    var currencyId = shopItem.shopSpecialCurrency == CustomCurrencyID.DefenderMedals
                        ? ItemID.DefenderMedal
                        : ItemID.CopperCoin;
                    currencyDebit[currencyId] += debit.StackSize * shopItem.GetStoreValue();
                    debit.StackSize = 0;
                }

                // Clear out currency debits wherever possible.
                foreach (var credit in _credits.Where(c => currencyDebit.ContainsKey(c.ItemId)))
                {
                    var payment = Math.Min(credit.StackSize, currencyDebit[credit.ItemId]);
                    credit.StackSize -= payment;
                    currencyDebit[credit.ItemId] += payment;
                }

                // If not all of the coins could be accounted for, we need to add the remainder as a debit. This is
                // because the player may have quickly sold an item of equivalent value. This reasoning doesn't apply
                // for defender medals since items cannot be sold in that currency.
                var coinDebit = currencyDebit[ItemID.CopperCoin];
                if (coinDebit < 0)
                {
                    AddTransaction(new Transaction(ItemID.CopperCoin, coinDebit));
                }
            }
        }
    }
}
