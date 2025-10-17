#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;

namespace SpectatorTest
{
    public class SpectatorConfig : BasePluginConfig
    {
        [JsonPropertyName("ChatPrefix")] public string ChatPrefix { get; set; } = "[Spectatorlist]";
        [JsonPropertyName("ChatInterval")] public float ChatInterval { get; set; } = 3.0f;
        [JsonPropertyName("Commands")] public List<string> Commands { get; set; } = new List<string> { "css_spectatorlist", "css_speclist" };
        [JsonPropertyName("SettingsCommands")] public List<string> SettingsCommands { get; set; } = new List<string> { "css_spectatorlistedit", "css_speclistedit" };
    }

    public static class SpectatorExtensions
    {
        public static CCSPlayerController? GetSpectatingPlayer(this CCSPlayerController? spec)
        {
            if (spec?.Pawn.Value is not { IsValid: true } pawn) return null;
            if (spec.ControllingBot) return null;
            if (pawn.ObserverServices is not { } obServices) return null;
            if (obServices.ObserverTarget?.Value?.As<CCSPlayerPawn>() is not { IsValid: true } obPawn) return null;
            if (obPawn.OriginalController.Value is not { IsValid: true } obController) return null;
            return obController;
        }

        public static List<CCSPlayerController> GetSpectators(this CCSPlayerController? obPlayer)
        {
            var spectators = new List<CCSPlayerController>();
            if (obPlayer == null || !obPlayer.IsValid) return spectators;
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (player.Pawn.Value is not { IsValid: true } pawn) continue;
                if (player.ControllingBot) continue;
                if (pawn.ObserverServices is not { } obServices) continue;
                if (obServices.ObserverTarget?.Value?.As<CCSPlayerPawn>() is not { IsValid: true } obPawn) continue;
                if (obPawn.OriginalController.Value is not { IsValid: true } obController) continue;
                if (obController.Slot == obPlayer.Slot)
                    spectators.Add(player);
            }
            return spectators;
        }
    }

    public class SpectatorTest : BasePlugin, IPluginConfig<SpectatorConfig>
    {
        public override string ModuleName => "CS2 Spectator List";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "PoncikMarket";
        public override string ModuleDescription => "Real spectator list";

        public SpectatorConfig Config { get; set; } = new SpectatorConfig();

        private Dictionary<ulong, Timer> spectatorTimers = new Dictionary<ulong, Timer>();
        private Dictionary<ulong, bool> spectatorActive = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> showNames = new Dictionary<ulong, bool>();
        private Dictionary<ulong, string> playerLanguages = new Dictionary<ulong, string>();
        private List<(string Command, CommandInfo.CommandCallback Handler)> registeredCommands = new List<(string, CommandInfo.CommandCallback)>();

        public override void Load(bool hotReload)
        {
            base.Load(hotReload);
            RegisterDynamicCommands();
        }

        public void OnConfigParsed(SpectatorConfig config)
        {
            if (config.ChatInterval > 60) config.ChatInterval = 60;
            if (string.IsNullOrEmpty(config.ChatPrefix) || config.ChatPrefix.Length > 25)
                throw new Exception($"Invalid value for ChatPrefix: {config.ChatPrefix}");

            Config = config;
            RegisterDynamicCommands();
            Logger.LogInformation("Config parsed successfully, prefix: {Prefix}", Config.ChatPrefix);
        }

        [GameEventHandler]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                ulong steamId = player.SteamID;
                spectatorActive[steamId] = true;
                showNames[steamId] = false; 
                StartSpectatorTimer(player); 
                RefreshSpectator(player); 
            }
            return HookResult.Continue;
        }

        private void RegisterDynamicCommands()
        {
            foreach (var (command, handler) in registeredCommands)
            {
                RemoveCommand(command, handler);
            }
            registeredCommands.Clear();

            var toggleCallback = new CommandInfo.CommandCallback(HandleSpectatorCommand);
            foreach (var cmd in Config.Commands)
            {
                AddCommand(cmd, "Toggle spectator list", toggleCallback);
                registeredCommands.Add((cmd, toggleCallback));
                Logger.LogInformation("Registered toggle command: {Command}", cmd);
            }

            var settingsCallback = new CommandInfo.CommandCallback(OnIzleyiciAyar);
            foreach (var cmd in Config.SettingsCommands)
            {
                AddCommand(cmd, "Customize spectator list display", settingsCallback);
                registeredCommands.Add((cmd, settingsCallback));
                Logger.LogInformation("Registered settings command: {Command}", cmd);
            }
        }

        private void HandleSpectatorCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected) return;

            ulong steamId = player.SteamID;
            bool isActive = spectatorActive.ContainsKey(steamId) ? spectatorActive[steamId] : true;

            spectatorActive[steamId] = !isActive;

            if (!isActive)
            {
                player.PrintToChat($"\x04{Config.ChatPrefix} \x03{Localizer["Opened"]}");
                StartSpectatorTimer(player);
            }
            else
            {
                StopSpectatorTimer(steamId);
                player.PrintToCenter("");
                player.PrintToChat($"\x02{Config.ChatPrefix} \x04{Localizer["Closed"]}");
            }
        }

        [ConsoleCommand("izleyiciayar", "Customize spectator list display")]
        [CommandHelper(minArgs: 0, usage: "[izleyiciayar]")]
        public void OnIzleyiciAyar(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected) return;

            var menu = new CenterHtmlMenu(Localizer["SettingsMenu"], this);
            menu.TitleColor = "#FFD700";
            menu.EnabledColor = "#44ff44";
            menu.DisabledColor = "#ff4444";

            ulong steamId = player.SteamID;
            bool currentSetting = showNames.ContainsKey(steamId) ? showNames[steamId] : false;

            menu.AddMenuOption(Localizer["ShowNames"], (p, o) =>
            {
                showNames[steamId] = true;
                p.PrintToChat($"\x04{Config.ChatPrefix} \x03{Localizer["NamesOpen"]}");
                p.PrintToCenterHtml("");
                RefreshSpectator(p);
            });

            menu.AddMenuOption(Localizer["HideNames"], (p, o) =>
            {
                showNames[steamId] = false;
                p.PrintToChat($"\x02{Config.ChatPrefix} \x04{Localizer["NamesClosed"]}");
                p.PrintToCenterHtml("");
                RefreshSpectator(p);
            });

            menu.Open(player);
            player.PrintToChat($"\x06{Config.ChatPrefix} \x01{Localizer["SettingsOpened"]}");
        }

        [ConsoleCommand("spectatorlistedit", "Customize spectator list display alias")]
        [CommandHelper(minArgs: 0, usage: "[spectatorlistedit]")]
        public void OnSpectatorListEdit(CCSPlayerController? player, CommandInfo command)
        {
            OnIzleyiciAyar(player, command);
        }

        [ConsoleCommand("css_lang", "Change player language (DISABLED)")]
        [CommandHelper(minArgs: 1, usage: "css_lang <lang>")]
        public void OnChangeLanguage(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && player.IsValid)
            {
                player.PrintToChat($"\x02{Config.ChatPrefix} \x04Dil değiştirme devre dışı, config dosyasından ayarlayın!");
            }
        }

        private void StartSpectatorTimer(CCSPlayerController? player)
        {
            if (player == null) return;
            ulong steamId = player.SteamID;

            StopSpectatorTimer(steamId);

            Timer timer = AddTimer(Config.ChatInterval, () =>
            {
                if (spectatorActive.ContainsKey(steamId) && spectatorActive[steamId])
                {
                    player.PrintToCenter("");
                    RefreshSpectator(player);
                }
            }, TimerFlags.REPEAT);

            spectatorTimers[steamId] = timer;
            RefreshSpectator(player);
        }

        private void StopSpectatorTimer(ulong steamId)
        {
            if (spectatorTimers.ContainsKey(steamId))
            {
                spectatorTimers[steamId].Kill();
                spectatorTimers.Remove(steamId);
            }
        }

        public void RefreshSpectator(CCSPlayerController? player)
        {
            if (player == null || !spectatorActive.ContainsKey(player.SteamID) || !spectatorActive[player.SteamID])
                return;
            ShowRealSpectatorList(player);
        }

        void ShowRealSpectatorList(CCSPlayerController? player)
        {
            if (player == null) return;
            ulong steamId = player.SteamID;
            bool showNamesSetting = showNames.ContainsKey(steamId) ? showNames[steamId] : false;

            var spectators = GetSpectatorsForPlayer(player);
            string message;
            if (showNamesSetting)
            {
                int count = spectators.Count;
                string names = string.Join(", ", spectators.Select(p => p.PlayerName));
                message = Localizer["Watchers", count, names];
            }
            else
            {
                message = Localizer["Watching", spectators.Count];
            }

            if (string.IsNullOrEmpty(message)) message = Localizer["NoWatchers"];
            Logger.LogDebug("Displaying message: {Message}", message);
            player.PrintToCenter($"\x07⚠️ {message}");
        }

        private List<CCSPlayerController> GetSpectatorsForPlayer(CCSPlayerController? player)
        {
            if (player == null) return new List<CCSPlayerController>();
            var targetPlayer = player.GetSpectatingPlayer();
            return targetPlayer != null ? targetPlayer.GetSpectators() : player.GetSpectators();
        }
    }
}