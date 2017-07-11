using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace NoCheat
{
    [ApiVersion(2, 1)]
    [UsedImplicitly]
    public sealed class NoCheatPlugin : TerrariaPlugin
    {
        /// <summary>
        ///     Specifies the number of infractions to show per page for /nc list.
        /// </summary>
        private const int InfractionsPerPage = 5;

        private static readonly string ConfigPath = Path.Combine("nocheat", "config.json");

        private readonly List<NoCheatModule> _modules;

        public NoCheatPlugin(Main game) : base(game)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            _modules = (from t in Assembly.GetExecutingAssembly().GetTypes()
                        where !t.IsAbstract && t.IsSubclassOf(typeof(NoCheatModule))
                        select (NoCheatModule)Activator.CreateInstance(t, this)).ToList();
        }

        public override string Author => "MarioE";
        public override string Description => "Provides anti-hack measures.";
        public override string Name => "NoCheat";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public override void Initialize()
        {
            Directory.CreateDirectory("nocheat");
            if (File.Exists(ConfigPath))
            {
                NoCheatConfig.Instance = JsonConvert.DeserializeObject<NoCheatConfig>(File.ReadAllText(ConfigPath));
            }

            foreach (var module in _modules)
            {
                module.Initialize();
            }

            GeneralHooks.ReloadEvent += OnReload;
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);

            Commands.ChatCommands.Add(new Command("nocheat", NoCheat, "nocheat", "nc"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(NoCheatConfig.Instance, Formatting.Indented));

                foreach (var module in _modules)
                {
                    module.Dispose();
                }

                GeneralHooks.ReloadEvent -= OnReload;
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }

            base.Dispose(disposing);
        }

        private void NoCheat(CommandArgs args)
        {
            var parameters = args.Parameters;
            var subcommand = parameters.Count > 0 ? parameters[0] : "";
            if (subcommand.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                NoCheatList(args);
            }
            else if (subcommand.Equals("pardon", StringComparison.OrdinalIgnoreCase))
            {
                NoCheatPardon(args);
            }
            else
            {
                var player = args.Player;
                player.SendErrorMessage("Syntax: /nocheat list <player/user> [page]");
                player.SendErrorMessage("Syntax: /nocheat pardon <player/user> <number>");
            }
        }

        private void NoCheatList(CommandArgs args)
        {
            var parameters = args.Parameters;
            var player = args.Player;
            if (parameters.Count < 2 || parameters.Count > 3)
            {
                player.SendErrorMessage($"Syntax: {Commands.Specifier}nocheat list <player/user> [page]");
                return;
            }

            var inputPlayerOrUser = parameters[1];
            var players = TShock.Utils.FindPlayer(inputPlayerOrUser);
            if (players.Count > 1)
            {
                TShock.Utils.SendMultipleMatchError(player, players);
                return;
            }

            IList<Infraction> infractions;
            var name = inputPlayerOrUser;
            if (players.Count == 0)
            {
                var path = Path.Combine("nocheat", $"{inputPlayerOrUser}.session");
                if (!File.Exists(path))
                {
                    player.SendErrorMessage($"Invalid user '{inputPlayerOrUser}'.");
                    return;
                }

                infractions = JsonConvert.DeserializeObject<List<Infraction>>(File.ReadAllText(path));
            }
            else
            {
                var otherPlayer = players[0];
                infractions = otherPlayer.GetOrCreateSession().Infractions;
                name = otherPlayer.Name;
            }

            if (infractions.Count == 0)
            {
                player.SendInfoMessage($"No infractions found for {name}.");
                return;
            }

            var pages = (infractions.Count - 1) / InfractionsPerPage + 1;
            var inputPage = parameters.Count > 2 ? parameters[2] : "1";
            if (!int.TryParse(inputPage, out var page) || page <= 0 || page > pages)
            {
                player.SendErrorMessage($"Invalid page number '{inputPage}'.");
                return;
            }

            player.SendSuccessMessage($"{name}'s infractions (page {page}/{pages}):");
            var offset = InfractionsPerPage * (page - 1);
            for (var i = 0; i < InfractionsPerPage && i + offset < infractions.Count; ++i)
            {
                var infraction = infractions[i + offset];
                var timeLeft = infraction.Expiration - DateTime.UtcNow;
                var days = timeLeft.Days;
                var hours = timeLeft.Hours;
                var minutes = timeLeft.Minutes;
                var seconds = timeLeft.Seconds;
                if (days > 0)
                {
                    player.SendInfoMessage($"[{i + offset + 1}] {infraction.Points} points for {infraction.Reason} " +
                                           $"(expires in {days} day{(days == 1 ? "" : "s")} and " +
                                           $"{hours} hour{(hours == 1 ? "" : "s")}).");
                }
                else if (hours > 0)
                {
                    player.SendInfoMessage($"[{i + offset + 1}] {infraction.Points} points for {infraction.Reason} " +
                                           $"(expires in {hours} hour{(hours == 1 ? "" : "s")} and " +
                                           $"{minutes} minute{(minutes == 1 ? "" : "s")}).");
                }
                else if (minutes > 0)
                {
                    player.SendInfoMessage($"[{i + offset + 1}] {infraction.Points} points for {infraction.Reason} " +
                                           $"(expires in {minutes} minute{(minutes == 1 ? "" : "s")} and " +
                                           $"{seconds} second{(seconds == 1 ? "" : "s")}).");
                }
                else if (seconds > 0)
                {
                    player.SendInfoMessage($"[{i + offset + 1}] {infraction.Points} points for {infraction.Reason} " +
                                           $"(expires in {seconds} second{(seconds == 1 ? "" : "s")}).");
                }
            }
            if (page != pages)
            {
                player.SendInfoMessage($"Type {Commands.Specifier}nocheat list {name} {page + 1} for more.");
            }
        }

        private void NoCheatPardon(CommandArgs args)
        {
            var parameters = args.Parameters;
            var player = args.Player;
            if (parameters.Count != 3)
            {
                player.SendErrorMessage($"Syntax: {Commands.Specifier}nocheat list <player/user> <number>");
                return;
            }

            var inputPlayerOrUser = parameters[1];
            var players = TShock.Utils.FindPlayer(inputPlayerOrUser);
            if (players.Count > 1)
            {
                TShock.Utils.SendMultipleMatchError(player, players);
                return;
            }

            Session session = null;
            IList<Infraction> infractions;
            var name = inputPlayerOrUser;
            if (players.Count == 0)
            {
                var path = Path.Combine("nocheat", $"{inputPlayerOrUser}.session");
                if (!File.Exists(path))
                {
                    player.SendErrorMessage($"Invalid user '{inputPlayerOrUser}'.");
                    return;
                }

                infractions = JsonConvert.DeserializeObject<List<Infraction>>(File.ReadAllText(path));
            }
            else
            {
                var otherPlayer = players[0];
                session = otherPlayer.GetOrCreateSession();
                infractions = session.Infractions;
                name = otherPlayer.Name;
            }

            var inputNumber = parameters[2];
            if (!int.TryParse(inputNumber, out var number) || number <= 0 || number > infractions.Count)
            {
                player.SendErrorMessage($"Invalid number '{inputNumber}'.");
                return;
            }

            var infraction = infractions[number - 1];
            player.SendSuccessMessage($"Pardoned {name} for {infraction.Reason}.");
            if (session == null)
            {
                infractions.RemoveAt(number - 1);
                var path = Path.Combine("nocheat", $"{inputPlayerOrUser}.session");
                File.WriteAllText(path, JsonConvert.SerializeObject(infractions));
            }
            else
            {
                session.RemoveInfraction(infraction);
                var otherPlayer = players[0];
                if (player != otherPlayer)
                {
                    otherPlayer.SendInfoMessage($"You have been pardoned for {infraction.Reason}.");
                }
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            foreach (var player in TShock.Players.Where(p => p?.Active == true))
            {
                var session = player.GetOrCreateSession();
                session.CheckInfractions();
            }
        }

        private void OnReload(ReloadEventArgs args)
        {
            if (File.Exists(ConfigPath))
            {
                NoCheatConfig.Instance = JsonConvert.DeserializeObject<NoCheatConfig>(File.ReadAllText(ConfigPath));
            }
            args.Player.SendSuccessMessage("[NoCheat] Reloaded config!");
        }
    }
}
