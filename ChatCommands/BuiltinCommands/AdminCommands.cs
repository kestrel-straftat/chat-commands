using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChatCommands.Attributes;
using FishNet;
using HarmonyLib;
using HeathenEngineering.DEMO;
using HeathenEngineering.SteamworksIntegration;
using Steamworks;

namespace ChatCommands.BuiltinCommands;

[CommandCategory("Admin")]
public static class AdminCommands
{
    private struct SavedLists
    {
        public HashSet<ulong> bans;
        public HashSet<ulong> ignores;
    }
    
    private static HashSet<ulong> m_ignoredPlayers;
    private static HashSet<ulong> m_bannedPlayers;
    
    private static string m_savePath = Path.Combine(BepInEx.Paths.ConfigPath, PluginInfo.PLUGIN_GUID + ".adminlists.json");

    public static void Init() {
        var save = Utils.LoadFromJsonFile<SavedLists?>(m_savePath);
        m_ignoredPlayers = save?.ignores ?? [];
        m_bannedPlayers = save?.bans ?? [];
    }

    private static void SaveToJson() {
        Utils.SaveToJsonFile(new SavedLists{bans = m_bannedPlayers, ignores = m_ignoredPlayers}, m_savePath);    
    }

    private static string UsernameOrId(this ParameterTypes.Player player) {
        var name = player.Username;
        return (name is "" or "[unknown]") ? player.steamID.ToString() : name;
    }
    
    [Command("ignore", "ignores a player, hiding their chat messages.")]
    public static string Ignore(ParameterTypes.Player target) {
        if (!m_ignoredPlayers.Add(target.steamID.m_SteamID)) {
            throw new CommandException($"\"{target.UsernameOrId()}\" is already ignored!");
        }

        SaveToJson();
        return $"ignored {target.UsernameOrId()}";
    }

    [Command("unignore", "unignores a player.")]
    public static string Unignore(ParameterTypes.Player target) {
        if (!m_ignoredPlayers.Remove(target.steamID.m_SteamID)) {
            throw new CommandException($"\"{target.UsernameOrId()}\" was not ignored!");
        }

        SaveToJson();
        return $"unignored {target.UsernameOrId()}";
    }

    [Command("ignorelist", "lists ignored players")]
    public static string ListIgnored() {
        var builder = new StringBuilder();
        builder.AppendLine("<u>ignored players</u>");
        if (m_ignoredPlayers.Count == 0) {
            builder.AppendLine("[none]");
        }
        foreach (var player in m_ignoredPlayers) {
            builder.AppendLine(player.ToString());
        }
        return builder.ToString();
    }
    
    [Command("clearignores", "removes ALL ignores. use with caution!")]
    public static string ClearIgnores() {
        m_ignoredPlayers.Clear();
        SaveToJson();
        return "cleared ignored list";
    }
    
    [Command("ban", "bans a player, automatically kicking them from any of your lobbies.")]
    public static string Ban(ParameterTypes.Player target) {
        if (!m_bannedPlayers.Add(target.steamID.m_SteamID)) {
            throw new CommandException($"\"{target.UsernameOrId()}\" is already banned!");
        }

        LobbyControllerPatch.KickBannedPlayers(InstanceFinder.NetworkManager.IsHost);
        SaveToJson();
        return $"banned {target.UsernameOrId()}";
    }

    [Command("unban", "unbans a player.")]
    public static string Unban(ParameterTypes.Player target) {
        if (!m_bannedPlayers.Remove(target.steamID.m_SteamID)) {
            throw new CommandException($"\"{target.UsernameOrId()}\" was not banned");
        }

        SaveToJson();
        return $"unbanned {target.UsernameOrId()}";
    }

    [Command("banlist", "lists banned players")]
    public static string ListBanned() {
        var builder = new StringBuilder();
        builder.AppendLine("<u>banned players</u>");
        if (m_bannedPlayers.Count == 0) {
            builder.AppendLine("[none]");
        }
        foreach (var player in m_bannedPlayers) {
            builder.AppendLine(player.ToString());
        }
        return builder.ToString();
    }

    [Command("clearbans", "removes ALL bans. use with caution!")]
    public static string ClearBans() {
        m_bannedPlayers.Clear();
        SaveToJson();
        return "cleared banned list";
    }
    
    [HarmonyPatch(typeof(LobbyChatUILogic))]
    internal static class LobbyChatUILogicPatch
    {
        [HarmonyPatch("HandleChatMessage")]
        [HarmonyPrefix]
        public static bool DontHandleIfIgnored(LobbyChatMsg message) => !m_ignoredPlayers.Contains(message.sender.SteamId);
    }

    [HarmonyPatch(typeof(LobbyController))]
    internal static class LobbyControllerPatch
    {
        private static List<PlayerListItem> m_playerListItems;

        public static void KickBannedPlayers(bool isHost) {
            foreach (var player in m_playerListItems.Where(player => m_bannedPlayers.Contains(player.PlayerSteamID))) {
                if (isHost) {
                    PauseManager.Instance.WriteLog($"Kicked \"{player.PlayerName}\": banned from this lobby");
                    player.KickPlayer();
                }
                else {
                    PauseManager.Instance.WriteOfflineLog($"Warning: the player \"{player.PlayerName}\" is on your ban list!");
                }
            }
        }
        
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void CapturePlayerlist(List<PlayerListItem> ___PlayerListItems) => m_playerListItems = ___PlayerListItems;

        [HarmonyPatch("CreateClientPlayerItem")]
        [HarmonyPatch("CreateHostPlayerItem")]
        [HarmonyPostfix]
        public static void HandleBans() => KickBannedPlayers(InstanceFinder.NetworkManager.IsHost); 
    }
}