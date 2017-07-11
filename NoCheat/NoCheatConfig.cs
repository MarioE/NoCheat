using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NoCheat.ItemSpawning;

namespace NoCheat
{
    /// <summary>
    ///     Represents the NoCheat configuration. This class is a singleton.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class NoCheatConfig
    {
        /// <summary>
        ///     Gets the NoCheat configuration instance.
        /// </summary>
        [NotNull]
        public static NoCheatConfig Instance { get; internal set; } = new NoCheatConfig();

        /// <summary>
        ///     Gets or sets the ban duration.
        /// </summary>
        [JsonProperty(Order = 1)]
        public TimeSpan BanDuration { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        ///     Gets or sets the ban message.
        /// </summary>
        [JsonProperty(Order = 2)]
        [NotNull]
        public string BanMessage { get; set; } = "Cheating";

        /// <summary>
        ///     Gets or sets the ItemSpawning module configuration.
        /// </summary>
        [JsonProperty(Order = 3)]
        [NotNull]
        public Config ItemSpawning { get; set; } = new Config();

        /// <summary>
        ///     Gets or sets the number of infraction points required for a temporary ban to be issued.
        /// </summary>
        [JsonProperty(Order = 0)]
        public int PointThreshold { get; set; } = 1000;
    }
}
