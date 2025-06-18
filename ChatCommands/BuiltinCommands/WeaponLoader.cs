using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChatCommands.BuiltinCommands;

internal static class WeaponLoader
{
    private static Dictionary<string, Weapon> m_weaponPrefabs = [];
    private static Dictionary<string, string> m_nameRedirects = [];

    public static void Init() {
        foreach (var weapon in Resources.LoadAll<Weapon>("RandomWeapons")) {
            string normalisedPrefabName = weapon.name.ToUpper().Replace(" ", "");
            string normalisedWeaponName = weapon.GetComponent<ItemBehaviour>().weaponName.ToUpper().Replace(" ", "");
            m_weaponPrefabs.TryAdd(normalisedPrefabName, weapon);
            m_nameRedirects.TryAdd(normalisedWeaponName, normalisedPrefabName);
        }
    }

    public static bool TryGetWeapon(string name, out Weapon prefab) {
        // trygetvalue slop. we love trygetvalue
        string normalisedName = name.ToUpper().Replace(" ", "");
        return m_weaponPrefabs.TryGetValue(normalisedName, out prefab) // try use the internal name
               || m_nameRedirects.TryGetValue(normalisedName, out var redirected) // didn't work, get the internal name from the display name
               && m_weaponPrefabs.TryGetValue(redirected, out prefab); // use that name to get the prefab
    }

    public static Weapon RandomWeapon() {
        return Utils.RandomValues(m_weaponPrefabs).Take(1).First();
    }

    public static Weapon[] RandomWeapons(int count) {
        return Utils.RandomValues(m_weaponPrefabs).Take(count).ToArray();
    }
}
