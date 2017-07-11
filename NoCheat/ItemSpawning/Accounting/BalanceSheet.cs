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
        ///     Adds a transaction with the specified item ID, stack size, and prefix ID.
        /// </summary>
        /// <param name="itemId">The item ID, which must be non-negative and in range.</param>
        /// <param name="stackSize">The stack size.</param>
        /// <param name="prefixId">The prefix ID, which must be in range.</param>
        /// <param name="checkNext">
        ///     <c>true</c> to wait for the next transaction to see if it would get canceled out; otherwise,
        ///     <c>false</c>.
        /// </param>
        public void AddTransaction(int itemId, int stackSize, byte prefixId = 0, bool checkNext = false)
        {
            Debug.Assert(itemId >= 0, "Item ID must be non-negative.");
            Debug.Assert(itemId < ItemID.Count, "Item ID must be in range.");
            Debug.Assert(prefixId < PrefixID.Count, "Prefix ID must be in range.");

            if (itemId == 0 || stackSize == 0)
            {
                return;
            }

            // Convert coins into copper coins. This simplifies our logic significantly, since clients will be
            // casually convert between denominations.
            while (itemId >= ItemID.SilverCoin && itemId <= ItemID.PlatinumCoin)
            {
                --itemId;
                stackSize *= 100;
            }

            var transaction = new Transaction(itemId, stackSize, prefixId, _player);
            lock (_lock)
            {
                var transactions = stackSize > 0 ? _credits : _debits;
                transactions.Add(transaction);
                Debug.WriteLine($"DEBUG: [{transaction.GetHashCode():X8}] transaction {transaction}");
            }
        }

        /// <summary>
        ///     Forgets a transaction of the specified item ID and prefix ID up to the stack size.
        /// </summary>
        /// <param name="itemId">The item ID, which must be non-negative and in range.</param>
        /// <param name="stackSize">The stack size.</param>
        /// <param name="prefixId">The prefix ID, which must be in range.</param>
        /// <returns><c>true</c> if a debit was forgiven; otherwise, <c>false</c>.</returns>
        public bool ForgetTransaction(int itemId, int stackSize, byte prefixId = 0)
        {
            Debug.Assert(itemId >= 0, "Item ID must be non-negative.");
            Debug.Assert(itemId < ItemID.Count, "Item ID must be in range.");
            Debug.Assert(prefixId < PrefixID.Count, "Prefix ID must be in range.");

            if (itemId == 0 || stackSize == 0)
            {
                return false;
            }

            // Convert coins into copper coins. This simplifies our logic significantly, since clients will be
            // casually convert between denominations.
            while (itemId >= ItemID.SilverCoin && itemId <= ItemID.PlatinumCoin)
            {
                --itemId;
                stackSize *= 100;
            }

            var oldStackSize = stackSize;
            lock (_lock)
            {
                if (stackSize > 0)
                {
                    foreach (var credit in _credits.Where(
                        c => c.ItemId == itemId && c.StackSize > 0 && c.PrefixId == prefixId))
                    {
                        var payment = Math.Min(credit.StackSize, stackSize);
                        Debug.Assert(payment > 0, "Payment must be positive.");
                        credit.StackSize -= payment;
                        stackSize -= payment;
                        Debug.WriteLine($"DEBUG: [{credit.GetHashCode():X8}] paid for by {-payment}, forgotten");

                        if (stackSize == 0)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var debit in _debits.Where(
                        d => d.ItemId == itemId && d.StackSize < 0 && d.PrefixId == prefixId))
                    {
                        var payment = Math.Min(-stackSize, -debit.StackSize);
                        Debug.Assert(payment > 0, "Payment must be positive.");
                        stackSize += payment;
                        debit.StackSize += payment;
                        Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for by {payment}, forgotten");

                        if (stackSize == 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return stackSize != oldStackSize;
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

        private IList<Transaction> GetCredits(PipelineStage stage) =>
            _credits.Where(c => c.Info.Stage == stage).ToList();

        private IList<Transaction> GetDebits(PipelineStage stage) =>
            _debits.Where(c => c.Info.Stage == stage).ToList();

        private void HandlePipeline(Config config)
        {
            var periods = new Dictionary<PipelineStage, TimeSpan>
            {
                [PipelineStage.CheckingRecipes] = config.CheckingRecipesPeriod,
                [PipelineStage.CheckingConversions] = config.CheckingConversionsPeriod,
                [PipelineStage.Simplifying] = config.SimplifyingPeriod
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
                var item = debit.ToItem();
                var points = -item.stack * config.PointOverrides.Get(item.type, config.Points.Get(item.rare));
                if (points > 0)
                {
                    session.AddInfraction(points, config.Duration, $"spawning {item.Name} x{-item.stack}");
                }

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
                        Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for by {payment}");
                        _player.SendData(PacketTypes.PlayerSlot, "", _player.Index, i, playerItem.prefix);
                    }

                    if (debit.StackSize == 0)
                    {
                        break;
                    }
                }
            }
            _credits.RemoveAll(c => c.StackSize == 0 || c.Info.Stage == PipelineStage.Expired);
            _debits.RemoveAll(d => d.StackSize == 0 || d.Info.Stage == PipelineStage.Expired);
        }

        private void ProcessConversions(Config config)
        {
            var credits = GetCredits(PipelineStage.CheckingConversions);
            var debits = GetDebits(PipelineStage.CheckingConversions);
            // The reason that we have a small grace period here is that the credit may have occurred before all of the
            // debits. This makes it more likely that all the necessary debits are in the correct stage.
            foreach (var credit in credits.Where(c => _updateTime > c.Info.LastUpdate + config.GracePeriod))
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

            var credits = GetCredits(PipelineStage.CheckingRecipes);
            var debits = GetDebits(PipelineStage.CheckingRecipes);
            // The reason that we have a small grace period here is that the debit may have occurred before all of the
            // credits. This makes it more likely that all the necessary credits are in the correct stage.
            foreach (var debit in debits.Where(d => _updateTime > d.Info.LastUpdate + config.GracePeriod))
            {
                bool ProcessRecipe(Recipe recipe)
                {
                    var payments = new Dictionary<Transaction, int>();
                    foreach (var ingredient in recipe.requiredItem)
                    {
                        var ingredientDebit = -ingredient.stack;
                        var ingredientIds = recipe.GetSubstituteIds(ingredient.type).ToList();
                        foreach (var credit in credits.Where(c => ingredientIds.Contains(c.ItemId) && c.StackSize > 0))
                        {
                            var payment = Math.Min(credit.StackSize, -ingredientDebit);
                            Debug.Assert(payment > 0, "Payment must be positive.");
                            payments[credit] = payment;
                            ingredientDebit += payment;

                            if (ingredientDebit == 0)
                            {
                                break;
                            }
                        }

                        if (ingredientDebit < 0)
                        {
                            return false;
                        }
                    }

                    debit.StackSize += recipe.createItem.stack;
                    Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for by {recipe.createItem.stack}, recipe");
                    if (!recipe.alchemy || !debit.Info.NearAlchemyTable)
                    {
                        foreach (var kvp in payments)
                        {
                            var credit = kvp.Key;
                            credit.StackSize -= kvp.Value;
                            Debug.WriteLine($"DEBUG: [{credit.GetHashCode():X8}] paid for by {-kvp.Value}, recipe");
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
            var credits = GetCredits(PipelineStage.Simplifying);
            var debits = GetDebits(PipelineStage.Simplifying);
            foreach (var debit in debits)
            {
                // Only check credits that are within the grace period of the debit. This prevents simplification from
                // affecting credits or debits that are farther ahead.
                foreach (var credit in credits.Where(
                    c => c.ItemId == debit.ItemId && c.StackSize > 0 && c.PrefixId == debit.PrefixId &&
                         (debit.Info.LastUpdate - c.Info.LastUpdate).Duration() < config.GracePeriod))
                {
                    var payment = Math.Min(credit.StackSize, -debit.StackSize);
                    Debug.Assert(payment > 0, "Payment must be positive.");
                    credit.StackSize -= payment;
                    debit.StackSize += payment;
                    Debug.WriteLine($"DEBUG: [{credit.GetHashCode():X8}] paid for by {-payment}");
                    Debug.WriteLine($"DEBUG: [{debit.GetHashCode():X8}] paid for by {payment}");

                    if (debit.StackSize == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
