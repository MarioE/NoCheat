using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents the configuration. This class is a singleton.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Config
    {
        /// <summary>
        ///     Gets the configuration instance.
        /// </summary>
        [NotNull]
        public static Config Instance => NoCheatConfig.Instance.ItemSpawning;

        /// <summary>
        ///     Gets the base number of points given for an infraction.
        /// </summary>
        [JsonProperty(Order = 3)]
        public int BasePoints { get; } = 10;

        /// <summary>
        ///     Gets the time before considering transactions expired.
        /// </summary>
        [JsonProperty(Order = 1)]
        public TimeSpan Expiration { get; } = TimeSpan.FromSeconds(2);

        /// <summary>
        ///     Gets the time before considering transactions valid.
        /// </summary>
        [JsonProperty(Order = 0)]
        public TimeSpan GracePeriod { get; } = TimeSpan.FromSeconds(1);

        /// <summary>
        ///     Gets the infraction duration.
        /// </summary>
        [JsonProperty(Order = 4)]
        public TimeSpan InfractionDuration { get; private set; } = TimeSpan.FromDays(1);

        /// <summary>
        ///     Gets the point overrides. If an item ID is present here, then that number of points will be given for an
        ///     infraction.
        /// </summary>
        [JsonProperty(Order = 6)]
        [NotNull]
        public Dictionary<int, int> PointOverrides { get; private set; } = new Dictionary<int, int>();

        /// <summary>
        ///     Gets the rarity multipliers. The number of points given for an infraction will be multiplied by the rarity.
        /// </summary>
        [JsonProperty(Order = 5)]
        [NotNull]
        public Dictionary<int, int> RarityMultipliers { get; private set; } = new Dictionary<int, int>
        {
            [-11] = 86,
            [-1] = 1,
            [0] = 1,
            [1] = 2,
            [2] = 2,
            [3] = 3,
            [4] = 5,
            [5] = 8,
            [6] = 11,
            [7] = 17,
            [8] = 26,
            [9] = 38,
            [10] = 58,
            [11] = 86
        };
    }
}
