using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents the configuration for the ItemSpawning module. This class is a singleton.
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
        ///     Gets or sets the checking conversions period for transactions.
        /// </summary>
        [JsonProperty(Order = 4)]
        public TimeSpan CheckingConversionsPeriod { get; set; } = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Gets or sets the checking recipes period for transactions.
        /// </summary>
        [JsonProperty(Order = 3)]
        public TimeSpan CheckingRecipesPeriod { get; set; } = TimeSpan.FromSeconds(0.5);

        /// <summary>
        ///     Gets or sets the infraction duration.
        /// </summary>
        [JsonProperty(Order = 6)]
        public TimeSpan Duration { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        ///     Gets a value indicating whether the module is enabled.
        /// </summary>
        [JsonProperty(Order = 0)]
        public bool Enabled { get; } = true;

        /// <summary>
        ///     Gets or sets the grace period for transactions being simplified.
        /// </summary>
        [JsonProperty(Order = 1)]
        public TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(0.25);

        /// <summary>
        ///     Gets the infraction point overrides, keyed by item ID.
        /// </summary>
        [JsonProperty(Order = 7)]
        [NotNull]
        public Dictionary<int, int> PointOverrides { get; private set; } = new Dictionary<int, int>();

        /// <summary>
        ///     Gets the infraction points, keyed by item rarity. This will be multiplied by the stack size to get the final number
        ///     of infraction points.
        /// </summary>
        [JsonProperty(Order = 5)]
        [NotNull]
        public Dictionary<int, int> Points { get; private set; } = new Dictionary<int, int>
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

        /// <summary>
        ///     Gets or sets the simplifying period for transactions.
        /// </summary>
        [JsonProperty(Order = 2)]
        public TimeSpan SimplifyingPeriod { get; set; } = TimeSpan.FromSeconds(0.5);
    }
}
