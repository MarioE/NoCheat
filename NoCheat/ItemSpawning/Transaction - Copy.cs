using System;
using Terraria;
using Terraria.ID;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents a transaction.
    /// </summary>
    public sealed class Transaction
    {
        private readonly DateTime _expiration;
        private readonly DateTime _gracePeriod;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Transaction" /> class with the specified item ID, stack size, prefix,
        ///     grace period, and expiration.
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
        public Transaction(int itemId, int stackSize, byte prefix, DateTime gracePeriod, DateTime expiration)
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

            ItemId = itemId;
            StackSize = stackSize;
            Prefix = prefix;
            _gracePeriod = gracePeriod;
            _expiration = expiration;
        }

        /// <summary>
        ///     Gets a value indicating whether the transaction is expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > _expiration.ToUniversalTime();

        /// <summary>
        ///     Gets a value indicating whether the transaction is in its grace period.
        /// </summary>
        public bool IsInGracePeriod => DateTime.UtcNow < _gracePeriod.ToUniversalTime();

        /// <summary>
        ///     Gets or sets the item ID.
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        ///     Gets the prefix.
        /// </summary>
        public byte Prefix { get; }

        /// <summary>
        ///     Gets or sets the stack size.
        /// </summary>
        public int StackSize { get; set; }
    }
}
