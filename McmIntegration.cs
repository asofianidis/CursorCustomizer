using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using SpiritVale.ModConfig;

namespace NevarisCursorCustomizer;

internal static class McmIntegration
{
    internal static void Register()
    {
        ModPage page = ModConfigMenu.Register(
            Plugin.Guid,
            Plugin.Name,
            "Make the in-game cursor easier to see without changing its click position.",
            Plugin.Version);

        page.AddHeader("GENERAL", "Changes apply immediately. The game's native cursor remains active underneath.");
        page.BindToggle(Plugin.Settings.Enabled, "Enable cursor marker",
            "Draw the selected marker over the native cursor.", _ => CursorOverlayController.ApplySettings());
        page.BindToggle(Plugin.Settings.HideWhenLocked, "Hide during camera lock",
            "Hide the marker when SpiritVale locks the pointer for camera control.",
            _ => CursorOverlayController.ApplySettings());

        page.AddHeader("APPEARANCE");
        page.BindChoice(Plugin.Settings.Style, "Marker style", CursorOverlayController.Styles,
            "Halo keeps the original cursor visible. Other styles add a stronger pointer shape.",
            _ => CursorOverlayController.ApplySettings());
        page.BindChoice(Plugin.Settings.Color, "Marker color", CursorOverlayController.Colors,
            "Choose a color that contrasts with the environments where the cursor gets lost.",
            _ => CursorOverlayController.ApplySettings());
        page.BindIntegerSlider(Plugin.Settings.Size, "Marker size",
            CursorOverlayController.MinimumSize, CursorOverlayController.MaximumSize, 4,
            "Overall marker size in screen pixels.", _ => CursorOverlayController.ApplySettings());
        page.BindIntegerSlider(Plugin.Settings.Thickness, "Stroke thickness",
            CursorOverlayController.MinimumThickness, CursorOverlayController.MaximumThickness, 1,
            "Width of rings and crosshair lines.", _ => CursorOverlayController.ApplySettings());
        page.BindFloatSlider(Plugin.Settings.Opacity, "Marker opacity",
            0.4f, 1f, 0.05f, "0%",
            "Lower opacity lets more of the native cursor and game remain visible.",
            _ => CursorOverlayController.ApplySettings());
        page.BindToggle(Plugin.Settings.DarkOutline, "Dark contrast outline",
            "Add a dark edge so bright marker colors remain visible over pale terrain and menus.",
            _ => CursorOverlayController.ApplySettings());

        AddLocatorHotkey(page);

        page.AddHeader("ACTIONS");
        page.AddButton("PreviewLocator", "Preview find-cursor pulse",
            CursorOverlayController.PreviewPulse,
            "Show the locator pulse at the current cursor position.");
        page.AddButton("RestoreDefaults", "Restore cursor defaults", () =>
        {
            page.ResetAll();
            CursorOverlayController.ApplySettings();
            CursorOverlayController.PreviewPulse();
        }, "Reset every Cursor Customizer setting.");

        Plugin.Info("Registered the Cursor Customizer page with Mod Configuration Menu.");
    }

    private static void AddLocatorHotkey(ModPage page)
    {
        MethodInfo bindHotkey = typeof(ModPage).GetMethod(
            "BindHotkey",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[]
            {
                typeof(ConfigEntry<KeyboardShortcut>),
                typeof(string),
                typeof(string),
                typeof(Action<KeyboardShortcut>)
            },
            null);

        if (bindHotkey is null)
        {
            page.AddHeader("LOCATOR",
                $"Press {Plugin.Settings.FindCursorHotkey.Value} to pulse the cursor. "
                + "This MCM version cannot edit key combinations.");
            Plugin.Warning("The installed MCM does not expose multi-key hotkey controls; "
                + "the locator shortcut remains active but cannot be edited in game.");
            return;
        }

        page.AddHeader("LOCATOR", "Press the shortcut whenever you lose track of the cursor.");
        bindHotkey.Invoke(page, new object[]
        {
            Plugin.Settings.FindCursorHotkey,
            "Find cursor",
            "Show an expanding ring around the cursor. Text entry suppresses this shortcut.",
            null
        });
    }
}
