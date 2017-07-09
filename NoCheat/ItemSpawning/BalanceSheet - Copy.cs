using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents a balance sheet. This class is thread-safe.
    /// </summary>
    public sealed class BalanceSheet
    {
        private static ILookup<int, Recipe> _recipeLookup;

        private readonly List<Transaction> _credits = new List<Transaction>();
        private readonly List<Transaction> _debits = new List<Transaction>();
        private readonly List<Transaction> _invalidDebits = new List<Transaction>();
        private readonly object _lock = new object();
        private readonly List<Transaction> _pendingCredits = new List<Transaction>();
        private readonly List<Transaction> _pendingDebits = new List<Transaction>();
        private readonly TSPlayer _player;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BalanceSheet" /> class based on the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        public BalanceSheet([NotNull] TSPlayer player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        /// <summary>
        ///     Adds a credit for the specified item ID and stack size.
        /// </summary>
        /// <param name="itemId">The item ID, which must be valid.</param>
        /// <param name="stackSize">The stack size, which must be non-negative.</param>
        /// <param name="prefix">The prefix, which must be valid.</param>
        /// <param name="gracePeriod">The grace period.</param>
        /// <param name="expiration">The expiration.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Either <paramref name="itemId" /> is invalid, <paramref name="stackSize" /> is negative, or
        ///     <paramref name="prefix" /> is invalid.
        /// </exception>
        public void AddCredit(int itemId, int stackSize, byte prefix, TimeSpan gracePeriod, TimeSpan expiration)
        {
            if (itemId < 0 || itemId >= Main.maxItemTypes)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId), "Item ID is invalid.");
            }
            if (stackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stackSize), "Stack size must be non-negative.");
            }
            if (prefix >= PrefixID.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(stackSize), "Prefix is invalid.");
            }
            if (stackSize == 0)
            {
                return;
            }

            // Convert coins into copper coins.
            while (ItemID.SilverCoin <= itemId && itemId <= ItemID.PlatinumCoin)
            {
                --itemId;
                stackSize *= 100;
            }
            lock (_lock)
            {
                _pendingCredits.Add(new Transaction(itemId, stackSize, prefix,
                    DateTime.UtcNow + gracePeriod, DateTime.UtcNow + expiration));
            }
        }

        /// <summary>
        ///     Adds a debit for the specified item ID and stack size.
        /// </summary>
        /// <param name="itemId">The item ID, which must be valid.</param>
        /// <param name="stackSize">The stack size, which must be non-negative.</param>
        /// <param name="prefix">The prefix, which must be valid.</param>
        /// <param name="gracePeriod">The grace period.</param>
        /// <param name="expiration">The expiration.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Either <paramref name="itemId" /> is invalid, <paramref name="stackSize" /> is negative, or
        ///     <paramref name="prefix" /> is invalid.
        /// </exception>
        public void AddDebit(int itemId, int stackSize, byte prefix, TimeSpan gracePeriod, TimeSpan expiration)
        {
            if (itemId < 0 || itemId >= Main.maxItemTypes)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId), "Item ID is invalid.");
            }
            if (stackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stackSize), "Stack size must be non-negative.");
            }
            if (prefix >= PrefixID.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(stackSize), "Prefix is invalid.");
            }
            if (stackSize == 0)
            {
                return;
            }

            // Convert coins into copper coins.
            while (ItemID.SilverCoin <= itemId && itemId <= ItemID.PlatinumCoin)
            {
                --itemId;
                stackSize *= 100;
            }
            lock (_lock)
            {
                _pendingDebits.Add(new Transaction(itemId, stackSize, prefix,
                    DateTime.UtcNow + gracePeriod, DateTime.UtcNow + expiration));
            }
        }

        /// <summary>
        ///     Consumes and returns the invalid debits.
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
        ///     Updates the balance sheet.
        /// </summary>
        public void Update()
        {
            lock (_lock)
            {
                SimplifyTransactions();
                ProcessRecipes();
                ProcessSales();
                ProcessPurchases();
            }
        }

        /// <summary>
        ///     Processes purchase debits wherever possible.
        /// </summary>
        private void ProcessPurchases()
        {
            if (_player.GetShop() == null)
            {
                return;
            }

            bool ProcessPurchasedItem(Transaction debit)
            {
                var soldItems = _player.GetSoldItems();
                for (var i = 0; i < soldItems.Count; ++i)
                {
                    var soldItem = soldItems[i];
                    if (soldItem.NetId == debit.ItemId && soldItem.Stack > 0)
                    {
                        var purchase = Math.Min(debit.StackSize, soldItem.Stack);
                        var item = new Item();
                        item.SetDefaults(soldItem.NetId);
                        item.Prefix(soldItem.PrefixId);

                        AddDebit(ItemID.CopperCoin, purchase * Math.Max(1, item.GetStoreValue() / 5), 0,
                            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

                        debit.StackSize -= purchase;
                        soldItems[i] = new NetItem(soldItem.NetId, soldItem.Stack - purchase, soldItem.PrefixId);
                        return true;
                    }
                }

                var shopItem = _player.GetShop()?.item?.FirstOrDefault(i => i.type == debit.ItemId);
                if (shopItem == null)
                {
                    return false;
                }

                // Transform the debit directly into a currency debit.
                debit.ItemId = shopItem.shopSpecialCurrency == CustomCurrencyID.DefenderMedals
                    ? ItemID.DefenderMedal
                    : ItemID.CopperCoin;
                debit.StackSize *= shopItem.GetStoreValue();
                return true;
            }

            foreach (var debit in _debits)
            {
                while (debit.StackSize > 0 && ProcessPurchasedItem(debit))
                {
                }
            }
            _debits.RemoveAll(d => d.StackSize <= 0);
        }

        /// <summary>
        ///     Processes recipe debits wherever possible.
        /// </summary>
        private void ProcessRecipes()
        {
            if (_recipeLookup == null)
            {
                _recipeLookup = Main.recipe.ToLookup(r => r.createItem.type);
            }

            bool ProcessDebitWithRecipe(Transaction debit, Recipe recipe)
            {
                var payments = new Dictionary<Transaction, int>();
                foreach (var item in recipe.requiredItem)
                {
                    var stackLeft = item.stack;
                    var ingredientIds = recipe.GetIngredientIds(item.type).ToList();
                    foreach (var credit in _credits.Where(c => ingredientIds.Contains(c.ItemId)))
                    {
                        var payment = Math.Min(credit.StackSize, stackLeft);
                        stackLeft -= payment;
                        payments[credit] = payment;
                    }

                    if (stackLeft > 0)
                    {
                        return false;
                    }
                }

                debit.StackSize -= recipe.createItem.stack;
                // Don't bother paying for alchemy recipes, as it is possible for the recipe to never consume
                // ingredients due to the alchemy table.
                if (!recipe.alchemy || !_player.TPlayer.alchemyTable)
                {
                    foreach (var kvp in payments)
                    {
                        Console.WriteLine("Using credit " + kvp.Key.ItemId + " x" + kvp.Value);
                        kvp.Key.StackSize -= kvp.Value;
                    }
                }
                return true;
            }

            foreach (var debit in _debits)
            {
                Console.WriteLine("Trying recipe use on " + debit.ItemId + " x" + debit.StackSize);
                foreach (var recipe in _recipeLookup[debit.ItemId])
                {
                    while (debit.StackSize > 0 && ProcessDebitWithRecipe(debit, recipe))
                    {
                    }
                }
            }
            _credits.RemoveAll(c => c.StackSize <= 0);
            _debits.RemoveAll(d => d.StackSize <= 0);
        }

        /// <summary>
        ///     Processes sale credits wherever possible.
        /// </summary>
        private void ProcessSales()
        {
            if (_player.GetShop() == null)
            {
                return;
            }

            foreach (var credit in _credits.Where(c => c.ItemId != ItemID.CopperCoin))
            {
                Console.WriteLine("Sold item " + credit.ItemId + " x" + credit.StackSize);

                var item = new Item();
                item.SetDefaults(credit.ItemId);
                item.Prefix(credit.Prefix);
                _player.AddSoldItem(new NetItem(credit.ItemId, credit.StackSize, credit.Prefix));

                // Transform the credit into a monetary credit.
                credit.ItemId = ItemID.CopperCoin;
                credit.StackSize *= Math.Max(1, item.GetStoreValue() / 5);
            }
        }

        /// <summary>
        ///     Simplifies the transactions by clearing out credits and debits wherever possible.
        /// </summary>
        private void SimplifyTransactions()
        {
            // Ensure that expired credits and debits are appropriately dealt with.
            _credits.RemoveAll(c => c.IsExpired);
            _invalidDebits.AddRange(_debits.Where(d => d.IsExpired));
            _debits.RemoveAll(d => d.IsExpired);

            // Ensure that credits and debits not in the grace period are moved.
            _credits.AddRange(_pendingCredits.Where(c => !c.IsInGracePeriod));
            _pendingCredits.RemoveAll(c => !c.IsInGracePeriod);
            _debits.AddRange(_pendingDebits.Where(d => !d.IsInGracePeriod));
            _pendingDebits.RemoveAll(d => !d.IsInGracePeriod);

            foreach (var debit in _debits.Concat(_pendingDebits))
            {
                foreach (var credit in _credits.Concat(_pendingCredits).Where(c => c.ItemId == debit.ItemId))
                {
                    var payment = Math.Min(credit.StackSize, debit.StackSize);
                    credit.StackSize -= payment;
                    debit.StackSize -= payment;
                }
            }
            _credits.RemoveAll(c => c.StackSize <= 0);
            _debits.RemoveAll(d => d.StackSize <= 0);
            _pendingCredits.RemoveAll(d => d.StackSize <= 0);
            _pendingDebits.RemoveAll(d => d.StackSize <= 0);
        }
    }
}
