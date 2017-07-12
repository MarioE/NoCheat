using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning.Accounting
{
    /// <summary>
    ///     Provides a balance sheet for a player. This class is thread-safe.
    /// </summary>
    public sealed class BalanceSheet
    {
        private static ILookup<int, Recipe> _recipeLookup;

        private readonly List<Transaction> _credits = new List<Transaction>();
        private readonly List<Transaction> _debits = new List<Transaction>();
        private readonly object _lock = new object();
        private readonly TSPlayer _player;

        private DateTime _updateTime = DateTime.UtcNow;

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
        ///     Adds a transaction with the specified slot, item ID, stack size, and prefix ID.
        /// </summary>
        /// <param name="slot">The slot of the transaction.</param>
        /// <param name="itemId">The item ID, which must be non-negative and in range.</param>
        /// <param name="stackSize">The stack size.</param>
        /// <param name="prefixId">The prefix ID, which must be in range.</param>
        public void AddTransaction(int slot, int itemId, int stackSize, byte prefixId = 0)
        {
            Debug.Assert(itemId >= 0, "Item ID must be non-negative.");
            Debug.Assert(itemId < ItemID.Count, "Item ID must be in range.");
            Debug.Assert(prefixId < PrefixID.Count, "Prefix ID must be in range.");

            if (itemId == 0 || stackSize == 0)
            {
                return;
            }

            // Convert all coins into copper coins. This makes our logic for processing coins significantly simpler.
            while (itemId >= ItemID.SilverCoin && itemId <= ItemID.PlatinumCoin)
            {
                --itemId;
                stackSize *= 100;
            }

            lock (_lock)
            {
                var credits = _credits.FindAll(c => c.Info.Stage == PipelineStage.Simplifying);
                var debits = _debits.FindAll(d => d.Info.Stage == PipelineStage.Simplifying);
                // If the slot is the mouse, trash can, or special stack update slot, then we need to be looking for the
                // most recent transaction that matches exactly. This is because we're essentially checking for
                // inventory management or item stacking by clients; whenever an item is moved around in the inventory,
                // the mouse slot or the trash can slot will be updated last.
                //
                // This also handles dropping items from the mouse for a similar reason, even though it's not strictly
                // inventory management.
                if (slot == Transaction.MouseSlot || slot == Transaction.TrashCanSlot ||
                    slot == Transaction.StackUpdateSlot)
                {
                    var searchTransactions = stackSize > 0 ? debits : credits;
                    var searchTransaction = searchTransactions.Reversed().FirstOrDefault(
                        t => t.ItemId == itemId && t.StackSize == -stackSize && t.PrefixId == prefixId);
                    if (searchTransaction != null)
                    {
                        searchTransaction.StackSize = 0;
                        Debug.WriteLine($"DEBUG: [{searchTransaction.GetHashCode():X8}] cleared by mouse/trash/stack");
                        return;
                    }
                }
                // If we have a credit, we need to be looking at world debits. Furthermore, those world debits must
                // have either occurred with the player selecting the proper slot (e.g., placing an item requires that
                // item to be selected), or the slot is the first matching slot in the player's inventory (e.g.,
                // painting a tile requires the first matching paint to be used).
                //
                // FindItem should work properly, since all calls from this method should be occurring on the thread
                // that receives packets. So information will be updated in sync with the arriving packets.
                else if (stackSize > 0)
                {
                    var firstSlot = _player.TPlayer.FindItem(itemId);
                    foreach (var debit in debits.Reversed().Where(
                        d => d.Slot == Transaction.WorldSlot && (d.Info.SelectedSlot == slot || slot == firstSlot) &&
                             d.ItemId == itemId && d.StackSize < 0 && d.PrefixId == prefixId))
                    {
                        var payment = Math.Min(stackSize, -debit.StackSize);
                        Debug.Assert(payment > 0, "Payment must be positive");
                        stackSize -= payment;
                        debit.StackSize += payment;
                        Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] cleared by world, x{payment}");

                        if (stackSize == 0)
                        {
                            return;
                        }
                    }
                }
                // If we have a debit, we need to be looking at world credits. We don't exactly care what slot this
                // debit is, since the debit could be anywhere.
                else if (stackSize < 0)
                {
                    foreach (var credit in credits.Reversed().Where(
                        c => c.Slot == Transaction.WorldSlot &&
                             c.ItemId == itemId && c.StackSize > 0 && c.PrefixId == prefixId))
                    {
                        var payment = Math.Min(credit.StackSize, -stackSize);
                        Debug.Assert(payment > 0, "Payment must be positive");
                        credit.StackSize -= payment;
                        stackSize += payment;
                        Debug.WriteLine($"DEBUG: [{credit.GetHashCode():X8}] cleared by world, x{payment}");

                        if (stackSize == 0)
                        {
                            return;
                        }
                    }
                }

                var transaction = new Transaction(slot, itemId, stackSize, prefixId, _player);
                var transactions = stackSize > 0 ? _credits : _debits;
                transactions.Add(transaction);
                Debug.WriteLine($"DEBUG: [{transaction.GetHashCode():X8}] added transaction {transaction}");
            }
        }

        /// <summary>
        ///     Forgets a debit of the specified item ID and prefix ID up to the stack size.
        /// </summary>
        /// <param name="itemId">The item ID, which must be non-negative and in range.</param>
        /// <param name="stackSize">The stack size, which must be positive.</param>
        /// <param name="prefixId">The prefix ID, which must be in range.</param>
        /// <returns><c>true</c> if any debits were forgotten; otherwise, <c>false</c>.</returns>
        public bool ForgetDebit(int itemId, int stackSize, byte prefixId = 0)
        {
            Debug.Assert(itemId >= 0, "Item ID must be non-negative.");
            Debug.Assert(itemId < ItemID.Count, "Item ID must be in range.");
            Debug.Assert(stackSize > 0, "Stack size must be positive.");
            Debug.Assert(prefixId < PrefixID.Count, "Prefix ID must be in range.");

            if (itemId == 0 || stackSize == 0)
            {
                return false;
            }

            // Convert all coins into copper coins. This makes our logic for processing coins significantly simpler.
            while (itemId >= ItemID.SilverCoin && itemId <= ItemID.PlatinumCoin)
            {
                --itemId;
                stackSize *= 100;
            }

            lock (_lock)
            {
                var succeeded = false;
                foreach (var debit in _debits.Reversed().Where(
                    d => d.ItemId == itemId && d.StackSize < 0 && d.PrefixId == prefixId))
                {
                    var payment = Math.Min(stackSize, -debit.StackSize);
                    Debug.Assert(payment > 0, "Payment must be positive");
                    stackSize -= payment;
                    debit.StackSize += payment;
                    Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] cleared by forgetting, x{payment}");
                    succeeded = true;

                    if (stackSize == 0)
                    {
                        break;
                    }
                }
                return succeeded;
            }
        }

        /// <summary>
        ///     Updates the balance sheet using the specified configuration.
        /// </summary>
        public void Update([NotNull] Config config)
        {
            Debug.Assert(config != null, "Config must not be null.");

            lock (_lock)
            {
                // Set an update time so that we can ensure the entire block of code runs as if it were simultaneous.
                _updateTime = DateTime.UtcNow;
                HandlePipeline(config);
                ProcessSimplifying(config);
                ProcessRecipes(config);
                ProcessConversions(config);
            }
        }

        private void HandlePipeline(Config config)
        {
            var periods = new Dictionary<PipelineStage, TimeSpan>
            {
                [PipelineStage.Simplifying] = config.SimplifyingPeriod,
                [PipelineStage.CheckingRecipes] = config.CheckingRecipesPeriod,
                [PipelineStage.CheckingConversions] = config.CheckingConversionsPeriod
            };
            foreach (var transaction in _credits.Concat(_debits))
            {
                var info = transaction.Info;
                if (_updateTime > info.LastUpdate + periods.Get(info.Stage, TimeSpan.MaxValue))
                {
                    info.LastUpdate = _updateTime;
                    ++info.Stage;
                    Debug.WriteLine($"DEBUG: [{transaction.GetHashCode():X8}] is at {info.Stage}");
                }
            }

            var session = _player.GetOrCreateSession();
            foreach (var debit in _debits.Where(d => d.StackSize < 0 && d.Info.Stage == PipelineStage.Expired))
            {
                // First, try to remove the expired debit from the player's inventory. If that fails, then give them an
                // infraction as a last resort.
                var playerItems = _player.GetAllItems().ToList();
                for (var i = 0; i < playerItems.Count; ++i)
                {
                    var playerItem = playerItems[i];
                    if (playerItem.type == debit.ItemId && playerItem.prefix == debit.PrefixId)
                    {
                        var payment = Math.Min(playerItem.stack, -debit.StackSize);
                        Debug.Assert(payment > 0, "Payment must be positive.");
                        playerItem.stack -= payment;
                        debit.StackSize += payment;
                        Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for, x{payment}");
                        _player.SendData(PacketTypes.PlayerSlot, "", _player.Index, i, playerItem.prefix);
                    }

                    if (debit.StackSize == 0)
                    {
                        break;
                    }
                }

                var item = debit.ToItem();
                var points = -item.stack * config.PointOverrides.Get(item.type, config.Points.Get(item.rare));
                if (points > 0)
                {
                    session.AddInfraction(points, config.Duration, $"spawning {item.Name} x{-item.stack}");
                }
            }
            _credits.RemoveAll(c => c.StackSize == 0 || c.Info.Stage == PipelineStage.Expired);
            _debits.RemoveAll(d => d.StackSize == 0 || d.Info.Stage == PipelineStage.Expired);
        }

        private void ProcessConversions(Config config)
        {
            var credits = _credits.FindAll(c => c.Info.Stage == PipelineStage.CheckingConversions);
            var debits = _debits.FindAll(d => d.Info.Stage == PipelineStage.CheckingConversions);
            // Conversions can only occur with inventory slots. We also have a small grace period here since the credit
            // may have occurred before the corresponding debits. This makes it more likely that all the debits are
            // in the correct stage.
            foreach (var credit in credits.Where(
                c => c.Slot >= 0 && _updateTime > c.Info.LastUpdate + config.GracePeriod))
            {
                ItemConversion conversion;
                if (credit.ItemId == credit.Info.QuestFishId && credit.Info.TalkingToNpcId == NPCID.Angler)
                {
                    conversion = ItemConversion.AnglerRewards;
                }
                else if (credit.ItemId == credit.Info.SelectedItemId &&
                         ItemID.Sets.ExtractinatorMode[credit.ItemId] >= 0)
                {
                    conversion = ItemConversion.ExtractinatorDrops;
                }
                else
                {
                    conversion = ItemConversion.ItemIdToConversion.Get(credit.ItemId);
                }

                if (conversion != null)
                {
                    while (credit.StackSize > 0 && conversion.Check(debits))
                    {
                        conversion.Apply(debits);
                        --credit.StackSize;
                    }
                }
            }
        }

        private void ProcessRecipes(Config config)
        {
            if (_recipeLookup == null)
            {
                _recipeLookup = Main.recipe.ToLookup(r => r.createItem.type);
            }

            var credits = _credits.FindAll(c => c.Info.Stage == PipelineStage.CheckingRecipes);
            var debits = _debits.FindAll(d => d.Info.Stage == PipelineStage.CheckingRecipes);
            // Crafting can only occur with the mouse slot. We also have a small grace period here since the debit
            // may have occurred before the corresponding credits. This makes it more likely that all the credits are
            // in the correct stage.
            foreach (var debit in debits.Where(
                d => d.Slot == Transaction.MouseSlot && _updateTime > d.Info.LastUpdate + config.GracePeriod))
            {
                bool ProcessRecipe(Recipe recipe)
                {
                    var payments = new Dictionary<Transaction, int>();
                    foreach (var ingredient in recipe.requiredItem)
                    {
                        var stackLeft = ingredient.stack;
                        var ingredientIds = recipe.GetSubstituteIds(ingredient.type).ToList();
                        // Crafting ingredients can only be taken from the inventory.
                        foreach (var credit in credits.Where(
                            c => c.Slot >= 0 && ingredientIds.Contains(c.ItemId) && c.StackSize > 0))
                        {
                            var payment = Math.Min(credit.StackSize, stackLeft);
                            Debug.Assert(payment > 0, "Payment must be positive.");
                            payments[credit] = payment;
                            stackLeft -= payment;

                            if (stackLeft == 0)
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
                    Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for, x{recipe.createItem.stack}, recipe");
                    if (!recipe.alchemy || !debit.Info.NearAlchemyTable)
                    {
                        foreach (var kvp in payments)
                        {
                            var credit = kvp.Key;
                            credit.StackSize -= kvp.Value;
                            Debug.WriteLine($"DEBUG: [{credit.GetHashCode():X8}] paid for, x{-kvp.Value}, ingredient");
                        }
                    }
                    return true;
                }

                foreach (var recipe in _recipeLookup[debit.ItemId])
                {
                    while (debit.StackSize < 0 && ProcessRecipe(recipe))
                    {
                    }
                }
            }
        }

        private void ProcessSimplifying(Config config)
        {
            var credits = _credits.FindAll(c => c.Info.Stage == PipelineStage.Simplifying);
            var debits = _debits.FindAll(d => d.Info.Stage == PipelineStage.Simplifying);
            foreach (var credit in credits.Where(c => c.ItemId == ItemID.CopperCoin))
            {
                foreach (var debit in debits.Where(d => d.ItemId == ItemID.CopperCoin && d.StackSize < 0))
                {
                    var payment = Math.Min(credit.StackSize, -debit.StackSize);
                    Debug.Assert(payment > 0, "Payment must be positive");
                    credit.StackSize -= payment;
                    debit.StackSize += payment;
                    Debug.WriteLine($"DEBUG: [{credit.GetHashCode():X8}] cleared by coin simplification, x{payment}");

                    if (credit.StackSize == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
