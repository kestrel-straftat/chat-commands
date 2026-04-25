using System.Collections.Generic;
using System.IO;
using System.Text;
using ChatCommands.Attributes;
using HarmonyLib;
using HeathenEngineering.DEMO;
using HeathenEngineering.SteamworksIntegration;
using ChatCommands.Utils;
using PlayerParam = ChatCommands.Utils.ParameterTypes.Player;

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
        var save = JsonUtils.FromJsonFile<SavedLists?>(m_savePath);
        m_ignoredPlayers = save?.ignores ?? [];
        m_bannedPlayers = save?.bans ?? [];
    }

    private static void SaveToJson() {
        JsonUtils.ToJsonFile(new SavedLists{bans = m_bannedPlayers, ignores = m_ignoredPlayers}, m_savePath);    
    }

    private static string UsernameOrId(this PlayerParam player) {
        var name = player.Username;
        return (name is "" or "[unknown]") ? player.steamID.ToString() : name;
    }
    
    [Command("ignore", "ignores a player, hiding their chat messages.")]
    public static string Ignore(PlayerParam target) {
        if (!m_ignoredPlayers.Add(target.steamID.m_SteamID)) {
            throw new CommandException($"\"{target.UsernameOrId()}\" is already ignored!");
        }

        SaveToJson();
        return $"ignored {target.UsernameOrId()}";
    }

    [Command("unignore", "unignores a player.")]
    public static string Unignore(PlayerParam target) {
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
    
    [HarmonyPatch(typeof(LobbyChatUILogic))]
    internal static class LobbyChatUILogicPatch
    {
        [HarmonyPatch("HandleChatMessage")]
        [HarmonyPrefix]
        public static bool DontHandleIfIgnored(LobbyChatMsg message) => !m_ignoredPlayers.Contains(message.sender.SteamId);
    }
}