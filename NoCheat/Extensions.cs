using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace NoCheat
{
    /// <summary>
    ///     Provides extension methods.
    /// </summary>
    public static class Extensions
    {
        private const string SessionKey = "NoCheat_Session";

        private static readonly Dictionary<int, Color> RarityColors = new Dictionary<int, Color>
        {
            [-11] = new Color(255, 175, 0),
            [-1] = new Color(130, 130, 130),
            [0] = new Color(255, 255, 255),
            [1] = new Color(150, 150, 255),
            [2] = new Color(150, 255, 150),
            [3] = new Color(255, 200, 150),
            [4] = new Color(255, 150, 150),
            [5] = new Color(255, 150, 255),
            [6] = new Color(210, 160, 255),
            [7] = new Color(150, 255, 10),
            [8] = new Color(255, 255, 10),
            [9] = new Color(5, 200, 255),
            [10] = new Color(255, 40, 100),
            [11] = new Color(180, 40, 255)
        };

        /// <summary>
        ///     Gets a value from the specified dictionary, returning a default value if the specified key is not present.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <param name="dictionary">The dictionary, which must not be <c>null</c>.</param>
        /// <param name="key">The key, which must not be <c>null</c>.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value, or the default value.</returns>
        [Pure]
        public static TValue Get<TKey, TValue>(
            [NotNull] this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            Debug.Assert(dictionary != null, "Dictionary must not be null.");
            Debug.Assert(key != null, "Key must not be null.");

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     Gets all of the items for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The items.</returns>
        [Pure]
        public static IEnumerable<Item> GetAllItems([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            var tplayer = player.TPlayer;
            return tplayer.inventory.Concat(tplayer.armor).Concat(tplayer.dye).Concat(tplayer.miscEquips)
                .Concat(tplayer.miscDyes).Concat(tplayer.bank.item).Concat(tplayer.bank2.item)
                .Concat(new[] {tplayer.trashItem}).Concat(tplayer.bank3.item);
        }

        /// <summary>
        ///     Gets the colored name for the specified item.
        /// </summary>
        /// <param name="item">The item, which must not be <c>null</c>.</param>
        /// <returns>The pretty name.</returns>
        [Pure]
        public static string GetColoredName([NotNull] this Item item)
        {
            Debug.Assert(item != null, "Item must not be null.");

            return string.IsNullOrEmpty(item.HoverName) ? "" : $"[c/{RarityColors[item.rare].Hex3()}:{item.HoverName}]";
        }

        /// <summary>
        ///     Gets or creates the session associated with the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The session.</returns>
        public static Session GetOrCreateSession([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            var session = player.GetData<Session>(SessionKey);
            if (session == null)
            {
                session = Session.Load(player);
                player.SetData(SessionKey, session);
            }
            return session;
        }

        /// <summary>
        ///     Returns an iterator that efficiently iterates through the specified list in reverse.
        /// </summary>
        /// <typeparam name="T">The type of list.</typeparam>
        /// <param name="list">The list, which must not be <c>null</c>.</param>
        /// <returns>The iterator.</returns>
        [Pure]
        public static IEnumerable<T> Reversed<T>(this IList<T> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                yield return list[i];
            }
        }
    }
}
