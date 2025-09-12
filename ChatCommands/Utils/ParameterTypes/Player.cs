using System;
using System.Linq;
using ChatCommands.Parsing;
using MyceliumNetworking;
using Steamworks;
using UnityEngine;

namespace ChatCommands.Utils.ParameterTypes;

public class Player
{
    public readonly CSteamID steamID;

    public bool InCurrentLobby => MyceliumNetwork.Players.Contains(steamID);
    public string Username => field ?? SteamFriends.GetFriendPersonaName(steamID);
    public ClientInstance Client => ClientInstance.playerInstances.Values.FirstOrDefault(ci => ci.PlayerSteamID == steamID.m_SteamID);
    public GameObject SpawnedPlayer => Client?.GetComponent<PlayerManager>().SpawnedObject;

    public Player(CSteamID steamID) {
        this.steamID = steamID;
        
        string name = SteamFriends.GetFriendPersonaName(steamID);
        if (name != "" && name != "[unknown]") {
            Username = name;
        }
    }

    public static bool TryGetInLobby(string username, out Player player) {
        foreach (var id in MyceliumNetwork.Players) {
            if (string.Equals(SteamFriends.GetFriendPersonaName(id), username, StringComparison.OrdinalIgnoreCase)) {
                player = new Player(id);
                return true;
            }
        }

        player = null;
        return false;
    }
}

public class PlayerParser : IParsingExtension
{
    public Type Target => typeof(Player);
    public object Parse(string value) {
        CSteamID steamID;
        if (ulong.TryParse(value, out var id) && (steamID = new CSteamID(id)).IsValid()) {
            return new Player(steamID);
        }
        
        if (Player.TryGetInLobby(value, out var player)) {
            return player;
        }
        
        throw new InvalidCastException($"\"{value}\" is not a valid steam id, or no user with that name is in your lobby!");
    }
}