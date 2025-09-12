using ChatCommands.Attributes;
using MyceliumNetworking;
using UnityEngine;
using PlayerParam = ChatCommands.Utils.ParameterTypes.Player;

namespace ChatCommands.BuiltinCommands;

[CommandCategory("Teleportation")]
public class TeleportationCommands
{
    public static TeleportationCommands Instance => field ??= new TeleportationCommands();
    
    private const CommandFlags c_tpCommandFlags = CommandFlags.ExplorationOnly | CommandFlags.IngameOnly;
    private static Vector3? m_location;
    private static Vector3? m_rotation;

    [Command("saveloc", "saves your current location and rotation", c_tpCommandFlags)]
    [CommandAliases("tps")]
    public static void SaveLoc() {
        var player = Settings.Instance.localPlayer;
        m_location = player.transform.position;
        m_rotation = PlayerEulerAngles(player);
    }
    
    [Command("loadloc", "loads your saved location and, optionally, rotation", c_tpCommandFlags)]
    [CommandAliases("tpl")]
    public static void LoadLoc(bool includeRotation = true, bool cancelMovement = true) {
        if (m_location is null) {
            throw new CommandException("No location is saved!");
        }
        if (includeRotation && m_rotation is null) {
            throw new CommandException("No rotation is saved!");
        }
        
        TeleportLocalPlayer(m_location.Value, includeRotation ? m_rotation : null, cancelMovement);
    }

    [Command("teleport", "teleports you to a specific location with a specific rotation", c_tpCommandFlags)]
    [CommandAliases("tp")]
    public static void Teleport(Vector3 location, Vector3? rotation = null, bool cancelMovement = true) {
        TeleportLocalPlayer(location, rotation, cancelMovement);
    }

    [Command("teleportplayer", "teleports a player to another player. the target must have chat commands installed.", c_tpCommandFlags)]
    [CommandAliases("tpp")]
    public static void TeleportPlayer(PlayerParam target, PlayerParam destination, bool includeRotation = false, bool cancelMovement = true) {
        if (!target.InCurrentLobby || !destination.InCurrentLobby) {
            throw new CommandException("Both the target and destination players must be in the lobby!");
        }
        if (!MyceliumNetwork.GetPlayerData<bool>(target.steamID, "chatCommandsInstalled")) {
            throw new CommandException("The target does not have chat commands installed!");
        }
        
        var targetPlayer = target.SpawnedPlayer?.GetComponent<FirstPersonController>();
        if (!targetPlayer) {
            throw new CommandException("The target player is not spawned!");
        }
        var destPlayer = destination.SpawnedPlayer?.GetComponent<FirstPersonController>();
        if (!destPlayer) {
            throw new CommandException("The destination player is not spawned!");
        }
        
        MyceliumNetwork.RPCTarget(Plugin.c_myceliumID, nameof(RPCTeleportPlayer), target.steamID, ReliableType.Reliable,
                destPlayer.transform.position, PlayerEulerAngles(destPlayer), includeRotation, cancelMovement
                );
    }

    [CustomRPC]
    private void RPCTeleportPlayer(Vector3 location, Vector3 rotation, bool includeRotation, bool cancelMovement) {
        TeleportLocalPlayer(location, includeRotation ? rotation : null, cancelMovement);
    }

    private static Vector3 PlayerEulerAngles(FirstPersonController player) {
        return new Vector3(player.rotationX, player.transform.rotation.eulerAngles.y, player.rotationZ);
    }

    private static void TeleportLocalPlayer(Vector3 location, Vector3? rotation = null, bool cancelMovement = true) {
        var player = Settings.Instance.localPlayer;
        player.transform.position = location;

        if (rotation is { } euler) {
            player.transform.rotation = Quaternion.Euler(0, euler.y, 0);
            // these are handled by the controller class (they rotate the camera holder)
            player.rotationX = euler.x;
            player.rotationZ = euler.z;
        }

        // awful by necessity (thanks sirius)
        if (cancelMovement) {
            player.moveDirection = Vector3.zero;
            player.verticalInput = 0f;
            player.horizontalInput = 0f;
            player.forceFactor = 0f;
            player.bstop = true;
            player.slopeSlideScript.slopeSlideMove = Vector3.zero;
            player.slopeSlideScript.steepSlopeSlideMove = Vector3.zero;
            player.objectCollisionMoveDirection = Vector3.zero;
            player.customForceScript.impact = Vector3.zero;
            player.characterController.SimpleMove(Vector3.zero);
        }
        
        player.PlaySoundServer(23);

    }
}