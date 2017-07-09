using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NoCheat
{
    /// <summary>
    ///     Represents an infraction.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Infraction
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Infraction" /> class with the specified number of points, expiration,
        ///     and reason.
        /// </summary>
        /// <param name="points">The number of points, which must be positive.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="reason">The reason, which must not be <c>null</c>.</param>
        public Infraction(int points, DateTime expiration, [NotNull] string reason)
        {
            Debug.Assert(points >= 0, "Number of points must be positive.");
            Debug.Assert(reason != null, "Reason must not be null.");

            Points = points;
            Expiration = expiration;
            Reason = reason;
        }

        /// <summary>
        ///     Gets the expiration.
        /// </summary>
        [JsonProperty]
        public DateTime Expiration { get; }

        /// <summary>
        ///     Gets the number of points.
        /// </summary>
        [JsonProperty]
        public int Points { get; }

        /// <summary>
        ///     Gets the reason.
        /// </summary>
        [JsonProperty]
        [NotNull]
        public string Reason { get; }
    }
}
