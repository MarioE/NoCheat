using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using TShockAPI;

namespace NoCheat
{
    /// <summary>
    ///     Provides extension methods.
    /// </summary>
    public static class Extensions
    {
        private const string SessionKey = "NoCheat_Session";

        /// <summary>
        ///     Gets a value from the dictionary, returning a default value if the specified key is not present.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TValue">The type of value.</typeparam>
        /// <param name="dictionary">The dictionary, which must not be <c>null</c>.</param>
        /// <param name="key">The key, which must not be <c>null</c>.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value, or the default value.</returns>
        public static TValue Get<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, TKey key,
            TValue defaultValue = default(TValue))
        {
            Debug.Assert(dictionary != null, "Dictionary must not be null.");
            Debug.Assert(key != null, "Key must not be null.");

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     Gets or creates the session associated with the player.
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
    }
}
