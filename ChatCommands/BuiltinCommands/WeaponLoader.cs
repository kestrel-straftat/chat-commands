using System.Collections.Generic;
using System.Linq;
using ChatCommands.Utils;
using FishNet;
using FishNet.Managing.Object;
using UnityEngine;

namespace ChatCommands.BuiltinCommands;

internal static class WeaponLoader
{
    private static Dictionary<string, Weapon> m_weaponPrefabs = [];
    private static Dictionary<string, PhysicsProp> m_propPrefabs = [];

    public static void Init() {
        foreach (var weapon in Resources.LoadAll<Weapon>("RandomWeapons")) {
            string normalisedPrefabName = weapon.name.ToUpper().Replace(" ", "");
            string normalisedWeaponName = weapon.GetComponent<ItemBehaviour>().weaponName.ToUpper().Replace(" ", "");
            m_weaponPrefabs.TryAdd(normalisedPrefabName, weapon);
            m_weaponPrefabs.TryAdd(normalisedWeaponName, weapon);
        }

        var nobPrefabs = (InstanceFinder.NetworkManager.SpawnablePrefabs as DefaultPrefabObjects)!.Prefabs;
        var props = nobPrefabs
            .Select(nob => nob.GetComponent<PhysicsProp>())
            .Where(x => x);
        
        foreach (var prop in props) {
            string normalisedPrefabName = prop.name.ToUpper().Replace(" ", "");
            string normalisedPropName = prop.popupText.ToUpper().Replace(" ", "");
            m_propPrefabs.TryAdd(normalisedPrefabName, prop);
            m_propPrefabs.TryAdd(normalisedPropName, prop);
        }
    }

    public static bool TryGetProp(string name, out PhysicsProp prefab) {
        string normalisedName = name.ToUpper().Replace(" ", "");
        return m_propPrefabs.TryGetValue(normalisedName, out prefab);
    }

    public static bool TryGetWeapon(string name, out Weapon prefab) {
        string normalisedName = name.ToUpper().Replace(" ", "");
        return m_weaponPrefabs.TryGetValue(normalisedName, out prefab);
    }

    public static Weapon RandomWeapon() {
        return m_weaponPrefabs.RandomValues().Take(1).First();
    }

    public static Weapon[] RandomWeapons(int count) {
        return m_weaponPrefabs.RandomValues().Take(count).ToArray();
    }
}
