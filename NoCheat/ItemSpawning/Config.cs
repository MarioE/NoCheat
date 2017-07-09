using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Represents the ItemSpawning module configuration. This class is a singleton.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Config
    {
        /// <summary>
        ///     Gets the ItemSpawning module configuration instance.
        /// </summary>
        [NotNull]
        public static Config Instance => NoCheatConfig.Instance.ItemSpawning;

        /// <summary>
        ///     Gets the infraction duration.
        /// </summary>
        [JsonProperty(Order = 2)]
        public TimeSpan Duration { get; private set; } = TimeSpan.FromDays(1);

        /// <summary>
        ///     Gets the infraction point overrides, keyed by item ID.
        /// </summary>
        [JsonProperty(Order = 3)]
        [NotNull]
        public Dictionary<int, int> PointOverrides { get; private set; } = new Dictionary<int, int>();

        /// <summary>
        ///     Gets the infraction points, keyed by item rarity. This will be multiplied by the stack size to get the final number
        ///     of infraction points.
        /// </summary>
        [JsonProperty(Order = 1)]
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
        ///     Gets the transaction stage durations. These values likely will need to increase if latency is an issue.
        /// </summary>
        [JsonProperty(Order = 0)]
        public TimeSpan[] StageDurations { get; } =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1)
        };
    }
}
