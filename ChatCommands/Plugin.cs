using System;
using BepInEx;
using BepInEx.Logging;
using ChatCommands.BuiltinCommands;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace ChatCommands;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
internal class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }
    internal static new ManualLogSource Logger;

    public static readonly string loadBearingColonThree = ":3";
    private void Awake() {
        if (loadBearingColonThree != ":3") Application.Quit();
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Instance = this;
        Logger = base.Logger;
        
        WeaponLoader.Init();
        BindingCommands.Init();
        AdminCommands.Init();
        CommandRegistry.RegisterCommandsFromAssembly();
        
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo("Hiiiiiiiiiiii :3");
    }

    private void Update() {
        BindingCommands.Update();
    }

    private void FixedUpdate() {
        BindingCommands.FixedUpdate();
    }
}

