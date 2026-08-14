# Nevaris Cursor Customizer (BepInEx 6 / IL2CPP)

Cursor visibility and accessibility mod for SpiritVale. It draws a non-clickable marker over the game's native cursor, so click targeting and the original cursor remain unchanged.

## Features

- Halo, high-contrast arrow, crosshair, target, and dot marker styles
- White, gold, cyan, lime, magenta, and red color choices
- Adjustable size, line thickness, opacity, and dark contrast outline
- Optional hiding while SpiritVale locks the cursor for camera control
- Configurable find-cursor hotkey with an expanding locator ring, default **F6**
- Immediate in-game configuration through Mod Configuration Menu

The default halo is deliberately restrained. It improves visibility across dark and bright terrain while leaving the familiar SpiritVale cursor visible in its center.

## Build

From this directory:

```powershell
dotnet build -c Release
```

The DLL is written to `bin/Release/net6.0/NevarisCursorCustomizer.dll`.

## Install

1. Install the IL2CPP build of BepInEx 6 and launch SpiritVale once.
2. Install Mod Configuration Menu.
3. Copy `NevarisCursorCustomizer.dll` to `<game>\BepInEx\plugins\CursorCustomizer\`.
4. Enter the world, then open **ESC > Settings > MODS > Cursor Customizer**.

For local updates, build the Release configuration and run `Install-CursorCustomizer.ps1`. Existing DLL and configuration files are backed up under `<game>\BepInEx\mod-backups\CursorCustomizer\` before replacement.

Configuration is stored at `<game>\BepInEx\config\alex.nevaris.cursorcustomizer.cfg`.

## Runtime behavior

The mod waits until the owned player is network-spawned, active, and character-loaded. It then creates a topmost Screen Space Overlay canvas. The two marker graphics do not receive raycasts, so they cannot intercept clicks, dragging, scrolling, or hover behavior.

The native cursor is never hidden or replaced. If the overlay is disabled or destroyed during a scene transition, SpiritVale's cursor continues to work normally. The overlay is rebuilt when the character returns to the world.

The find-cursor shortcut is suppressed while a TextMesh Pro input field has keyboard focus. All plugin-authored log messages begin with `[CursorCustomizer]`.

The default game path is `F:\SteamLibrary\steamapps\common\SpiritVale`; override it when building if needed:

```powershell
dotnet build -c Release -p:GameDir="D:\Games\SpiritVale"
```
