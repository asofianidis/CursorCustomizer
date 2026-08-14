using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Configuration;
using HarmonyLib;
using SpiritVale.ModConfig;
using UnityEngine;

namespace NevarisCursorCustomizer;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency(ModConfigPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BasePlugin
{
    public const string Guid = "alex.nevaris.cursorcustomizer";
    public const string Name = "Cursor Customizer";
    public const string Version = "1.0.0";

    internal static ManualLogSource Logger = null!;
    internal static CursorSettings Settings = null!;
    private Harmony _harmony;

    public override void Load()
    {
        Logger = Log;
        Settings = new CursorSettings(Config);
        CursorOverlayController.Initialize();
        McmIntegration.Register();

        _harmony = new Harmony(Guid);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
        Info($"{Name} {Version} loaded. {Settings.FindCursorHotkey.Value} locates the cursor.");
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        PlayerWorldGate.Shutdown();
        CursorOverlayController.Shutdown();
        return base.Unload();
    }

    internal static void Info(string message) => Logger.LogInfo($"[CursorCustomizer] {message}");
    internal static void Warning(string message) => Logger.LogWarning($"[CursorCustomizer] {message}");
    internal static void Error(string message) => Logger.LogError($"[CursorCustomizer] {message}");
}

internal sealed class CursorSettings
{
    internal readonly ConfigEntry<bool> Enabled;
    internal readonly ConfigEntry<bool> HideWhenLocked;
    internal readonly ConfigEntry<string> Style;
    internal readonly ConfigEntry<string> Color;
    internal readonly ConfigEntry<int> Size;
    internal readonly ConfigEntry<int> Thickness;
    internal readonly ConfigEntry<float> Opacity;
    internal readonly ConfigEntry<bool> DarkOutline;
    internal readonly ConfigEntry<KeyboardShortcut> FindCursorHotkey;

    internal CursorSettings(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true,
            "Show the custom cursor marker while a character is loaded.");
        HideWhenLocked = config.Bind("General", "HideWhenLocked", true,
            "Hide the marker while the game locks the cursor for camera control.");
        Style = config.Bind("Appearance", "Style", CursorOverlayController.DefaultStyle,
            "Visual marker drawn over the game's native cursor.");
        Color = config.Bind("Appearance", "Color", CursorOverlayController.DefaultColor,
            "Marker color. A dark outline can preserve contrast on bright scenes.");
        Size = config.Bind("Appearance", "Size", 56,
            "Marker size in screen pixels.");
        Thickness = config.Bind("Appearance", "Thickness", 4,
            "Stroke thickness in screen pixels.");
        Opacity = config.Bind("Appearance", "Opacity", 0.95f,
            "Marker opacity.");
        DarkOutline = config.Bind("Appearance", "DarkOutline", true,
            "Draw a dark edge around the marker for contrast on light backgrounds.");
        FindCursorHotkey = config.Bind("Locator", "FindCursor", new KeyboardShortcut(KeyCode.F6),
            "Show an expanding ring around the current cursor position.");
    }
}
