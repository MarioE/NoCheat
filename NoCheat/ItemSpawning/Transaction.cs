using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents a mutable transaction. Transactions describe the movement of items throughout a player, and are
    ///     processed in various stages.
    /// </summary>
    public sealed class Transaction
    {
        private DateTime _lastUpdate = DateTime.UtcNow;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Transaction" /> class with the specified item ID, stack size, and
        ///     prefix.
        /// </summary>
        /// <param name="itemId">The item ID, which must be valid.</param>
        /// <param name="stackSize">The stack size.</param>
        /// <param name="prefix">The prefix, which must be valid.</param>
        public Transaction(int itemId, int stackSize = 1, byte prefix = 0)
        {
            Debug.Assert(0 <= itemId && itemId < Main.maxItemTypes, "Item ID must be valid.");
            Debug.Assert(prefix < PrefixID.Count, "Prefix must be valid.");

            ItemId = itemId;
            StackSize = stackSize;
            Prefix = prefix;
        }

        /// <summary>
        ///     Gets or sets the active shop at the time the transaction occurred.
        /// </summary>
        public Chest ActiveShop { get; set; }

        /// <summary>
        ///     Gets or sets the item ID.
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        ///     Gets or sets the prefix.
        /// </summary>
        public byte Prefix { get; set; }

        /// <summary>
        ///     Gets or sets the list of sold items at the time the transaction occurred.
        /// </summary>
        public List<NetItem> SoldItems { get; set; }

        /// <summary>
        ///     Gets or sets the stack size.
        /// </summary>
        public int StackSize { get; set; }

        /// <summary>
        ///     Gets the stage that this transaction is in.
        /// </summary>
        public int Stage { get; private set; } = 1;

        /// <summary>
        ///     Updates the stage of this transaction, if necessary, using the specified stage length.
        /// </summary>
        /// <param name="stageLength">The stage length.</param>
        public void UpdateStage(TimeSpan stageLength)
        {
            if (DateTime.UtcNow >= _lastUpdate + stageLength)
            {
                _lastUpdate = DateTime.UtcNow;
                ++Stage;
            }
        }
    }
}
