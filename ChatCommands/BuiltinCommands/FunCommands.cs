using System.Collections;
using System.Linq;
using ChatCommands.Attributes;
using FishNet;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChatCommands.BuiltinCommands;

[CommandCategory("Fun")]
public static class FunCommands
{
    private const CommandFlags c_weaponCommandFlags = CommandFlags.ExplorationOnly | CommandFlags.IngameOnly | CommandFlags.TryRunOnHost;

    private static GameObject SteamIDToPlayer(CSteamID requester) {
        return ClientInstance.playerInstances.Values.FirstOrDefault(ci => ci.PlayerSteamID == requester.m_SteamID)?.GetComponent<PlayerManager>().SpawnedObject;
    }

    private static Transform SteamIDToTransform(CSteamID requester) {
        return requester == default ? Settings.Instance.localPlayer.transform : SteamIDToPlayer(requester).transform;
    }

    [Command("weapon", "spawns a weapon", c_weaponCommandFlags)]
    [CommandAliases("w")]
    public static void Weapon(string weaponName, int count = 1, CSteamID requester = default) {
        if (WeaponLoader.TryGetWeapon(weaponName, out var weapon)) { 
            var playerTransform = SteamIDToTransform(requester);
            for (int i = 0; i < count; ++i) {
                var obj = Object.Instantiate(weapon, playerTransform.position, playerTransform.rotation).gameObject;
                InstanceFinder.ServerManager.Spawn(obj);
                obj.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
            }
        }
        else {
            throw new CommandException($"No weapon found with name {weaponName}");
        }
    }

    [Command("randomweapon", "spawns a random weapon", c_weaponCommandFlags)]
    [CommandAliases("rw")]
    public static void RandomWeapon(int count = 1, CSteamID requester = default) {
        var weapons = WeaponLoader.RandomWeapons(count);
        var playerTransform = SteamIDToTransform(requester);
        foreach (var weapon in weapons) {
            var obj = Object.Instantiate(weapon, playerTransform.position, playerTransform.rotation).gameObject;
            InstanceFinder.ServerManager.Spawn(obj);
            obj.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
        }
    }
    
    private static bool m_weaponRainEnabled = false;
    
    [Command("weaponrain", "toggles weapon rain", c_weaponCommandFlags)]
    public static void ToggleWeaponRain(CSteamID requester = default) {
        m_weaponRainEnabled = !m_weaponRainEnabled;
        if (m_weaponRainEnabled) {
            Plugin.Instance.StartCoroutine(WeaponRain(SteamIDToTransform(requester)));
        }
    }

    private static IEnumerator WeaponRain(Transform playerTransform) {
        while (m_weaponRainEnabled) {
            var weapon = WeaponLoader.RandomWeapon();
            var offset = Random.insideUnitCircle * 10f;
            var spawnPos = new Vector3(
                playerTransform.position.x + offset.x,
                playerTransform.position.y + 10f,
                playerTransform.position.z + offset.y
            );
            var obj = Object.Instantiate(weapon, spawnPos, playerTransform.rotation).gameObject;
            InstanceFinder.ServerManager.Spawn(obj);
            obj.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);

            yield return new WaitForSeconds(0.1f);
        }
    }

    [Command("clearweapons", "removes all spawned weapons in the scene", c_weaponCommandFlags)]
    public static void ClearWeapons() {
        var weapons = SceneManager
            .GetActiveScene()
            .GetRootGameObjects()
            .Where(w => w.GetComponent<Weapon>() && w.GetComponent<Rigidbody>());
        
        foreach (var weapon in weapons) {
            InstanceFinder.ServerManager.Despawn(weapon);
            Object.Destroy(weapon);
        }
    }

    [Command("fling", "flings you", CommandFlags.ExplorationOnly | CommandFlags.IngameOnly)]
    public static void Fling(int magnitude) {
        var player = Settings.Instance.localPlayer;
        
        player.CustomAddForce(player.playerCamera.transform.forward, magnitude);
        //player.BForce(player.playerCamera.transform.forward, magnitude, true, true, 3f, true); // how the FUCK does bforce work
    }
}