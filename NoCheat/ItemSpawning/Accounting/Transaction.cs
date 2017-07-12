using System.Diagnostics;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning.Accounting
{
    /// <summary>
    ///     Represents a mutable transaction which describes the flow of items.
    /// </summary>
    public sealed class Transaction
    {
        /// <summary>
        ///     Represents the mouse slot.
        /// </summary>
        public const int MouseSlot = 58;

        /// <summary>
        ///     Represents the stack update slot.
        /// </summary>
        public const int StackUpdateSlot = -2;

        /// <summary>
        ///     Represents the trash can slot.
        /// </summary>
        public const int TrashCanSlot = 179;

        /// <summary>
        ///     Represents the world slot.
        /// </summary>
        public const int WorldSlot = -1;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Transaction" /> class with the specified slot, item ID, stack size,
        ///     prefix ID, and player.
        /// </summary>
        /// <param name="slot">The slot index that the transaction occurred in.</param>
        /// <param name="itemId">The item ID, which must be non-negative and in range.</param>
        /// <param name="stackSize">The stack size.</param>
        /// <param name="prefixId">The prefix ID, which must be in range.</param>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        public Transaction(int slot, int itemId, int stackSize, byte prefixId, TSPlayer player)
        {
            Debug.Assert(itemId >= 0, "Item ID must be non-negative.");
            Debug.Assert(itemId < ItemID.Count, "Item ID must be in range.");
            Debug.Assert(prefixId < PrefixID.Count, "Prefix ID must be in range.");
            Debug.Assert(player != null, "Player must not be null.");

            Slot = slot;
            ItemId = itemId;
            StackSize = stackSize;
            PrefixId = prefixId;
            Info = new TransactionInfo(player);
        }

        /// <summary>
        ///     Gets the transaction information.
        /// </summary>
        public TransactionInfo Info { get; }

        /// <summary>
        ///     Gets the item ID.
        /// </summary>
        public int ItemId { get; }

        /// <summary>
        ///     Gets the prefix ID.
        /// </summary>
        public byte PrefixId { get; }

        /// <summary>
        ///     Gets the slot.
        /// </summary>
        public int Slot { get; }

        /// <summary>
        ///     Gets the stack size.
        /// </summary>
        public int StackSize { get; set; }

        /// <summary>
        ///     Returns an item with the same characteristics.
        /// </summary>
        /// <returns>The item.</returns>
        [NotNull]
        [Pure]
        public Item ToItem()
        {
            var item = new Item();
            item.SetDefaults(ItemId);
            item.Prefix(PrefixId);
            item.stack = StackSize;
            return item;
        }

        /// <summary>
        ///     Returns the string representation of this instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            var item = ToItem();
            return $"{item.AffixName()} x{item.stack} (slot {Slot})";
        }
    }
}
