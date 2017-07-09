using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using TShockAPI;

namespace NoCheat
{
    /// <summary>
    ///     Holds session information.
    /// </summary>
    public sealed class Session
    {
        private readonly List<Infraction> _infractions;
        private readonly TSPlayer _player;

        private Session(TSPlayer player, IEnumerable<Infraction> infractions)
        {
            _player = player;
            _infractions = infractions.ToList();
        }

        /// <summary>
        ///     Gets the infractions.
        /// </summary>
        [ItemNotNull]
        [NotNull]
        public ReadOnlyCollection<Infraction> Infractions => _infractions.AsReadOnly();

        /// <summary>
        ///     Loads the session for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The session.</returns>
        [NotNull]
        public static Session Load(TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            var username = player.User?.Name ?? player.Name;
            var path = Path.Combine("nocheat", $"{username}.session");
            var infractions = File.Exists(path)
                ? JsonConvert.DeserializeObject<List<Infraction>>(File.ReadAllText(path))
                : new List<Infraction>();
            return new Session(player, infractions);
        }

        /// <summary>
        ///     Adds an infraction for the specified number of points, duration, and reason.
        /// </summary>
        /// <param name="points">The number of points, which must be positive.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="reason">The reason, which must not be <c>null</c>.</param>
        public void AddInfraction(int points, TimeSpan duration, [NotNull] string reason)
        {
            Debug.Assert(points > 0, "Number of points must be positive.");
            Debug.Assert(reason != null, "Reason must not be null.");

            _infractions.Add(new Infraction(points, DateTime.UtcNow + duration, reason));
            Save();
        }

        /// <summary>
        ///     Checks the infractions. If the total number of points exceeds the threshold set in the configuration, then a
        ///     temporary ban will be issued.
        /// </summary>
        public void CheckInfractions()
        {
            if (_infractions.RemoveAll(i => DateTime.UtcNow > i.Expiration) > 0)
            {
                Save();
            }

            var config = NoCheatConfig.Instance;
            var totalPoints = _infractions.Sum(i => i.Points);
            if (totalPoints > config.PointThreshold && !_player.HasPermission(Permissions.immunetoban))
            {
                _player.Disconnect($"Banned: {config.BanMessage}");
                TSPlayer.All.SendInfoMessage($"{_player.Name} was banned for '{config.BanMessage}'.");
                TShock.Bans.AddBan(_player.IP, _player.Name, _player.UUID, config.BanMessage,
                    expiration: DateTime.UtcNow.Add(config.BanDuration).ToString("s"));
            }
        }

        /// <summary>
        ///     Removes the specified infraction.
        /// </summary>
        /// <param name="infraction">The infraction, which must not be <c>null</c>.</param>
        public void RemoveInfraction([NotNull] Infraction infraction)
        {
            Debug.Assert(infraction != null, "Infraction must not be null.");

            _infractions.Remove(infraction);
            Save();
        }

        /// <summary>
        ///     Saves the session. The infractions will be JSON-serialized.
        /// </summary>
        private void Save()
        {
            var username = _player.User?.Name ?? _player.Name;
            var path = Path.Combine("nocheat", $"{username}.session");
            File.WriteAllText(path, JsonConvert.SerializeObject(_infractions));
        }
    }
}
