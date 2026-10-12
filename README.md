# Defense Manager

One panel for the traps and barricades outside your home.

Keeping your outdoor defenses up means walking to each one and clicking through its menu. Defense Manager lists them all in one panel: repair every damaged one in one press, clear away the broken ones, or repair and remove them one by one.

## Features

- **Panel key**: press `F7` to open and close the panel in a loaded game, past the prologue. `Esc` or the close button in the top-right corner also closes it.
- **HUD button**: a shield at the top of the tool column in the bottom-left corner of the screen, above the game's own buttons there. It opens and closes the panel like `F7`, and hovering it shows "Defense Manager" with the key and "Repair or remove your outdoor defenses". It only shows in a loaded game past the prologue, and it hides with the rest of the column on the Layout screen.
- **Layout screen buttons**: while the game's Layout screen is open, three buttons sit at the top middle of the screen, styled like Layout's own buttons: **Repair all (n)**, **Clear all broken (n)** and **Defense list (F7)**. The first two do exactly what the panel's buttons of the same name do. Defense list opens the panel with every defense on its own row. When the buttons are greyed out, the reason shows under them. They can be turned off in the config.
- **Repair all (n)**: queues a repair for every damaged defense your materials cover (n is how many it would repair now). The character goes nearest-next from where they stand, so the round does not zigzag across the yard. A defense the remaining materials cannot cover is skipped and the round goes on to the next one they can.
- **Clear all broken (n)**: queues the removal of every broken defense, nearest-next. A broken defense you already queued for repair is kept: you chose to fix it.
- **One row per defense**: icon, name, state (Broken, Damaged or Intact), a condition bar, the repair cost, and a **Repair** and a **Remove** button. A defense already queued shows "Repair queued" or "Removal queued".
- **Buttons say why not**: a button that would be refused is greyed out and the reason shows under it: "Needs 3 more Wood", the game's own reason, or "Repair queued: let it finish or cancel it first".
- **Remove without the are-you-sure box**: Remove queues the same removal action as the defense's own menu, without the confirmation box.
- **Materials bar**: each repair material shows as free / needed: what is left after the repairs already queued, against what the repairs not yet queued would take. A material you are short of is outlined in red.
- **Clear messages**: after each press a line says what was queued and what was left and why. Repair all names up to three defenses that are short of materials and exactly what each is missing. When the panel is closed, the message shows as a short pop-up instead.
- **Game hotkeys paused**: the game's own hotkeys are off while the panel is open and come back when it closes, so keys you press in the panel do not reach the game.
- **Log file**: every press and what came of it is written to `BepInEx\defense-manager.log`.

Every repair and removal is the game's own action, the same one the defense's menu lines up, so the game's own walking, timing and material use apply. Only defenses outside your home are listed; doors and windows are not touched.

Defense Manager adds no data of its own to your save. The repairs and removals it queues are real: they use your materials and take defenses down, exactly as if you had used each defense's menu. You can remove the mod at any time; your defenses stay as they are.

## Hotkeys

| Key | What it does |
| --- | --- |
| `F7` | Opens and closes the panel (in a loaded game, past the prologue). |
| `Esc` | Closes the panel. |

`F7` can be changed or turned off in the config. The HUD button works either way.

The mod's panel and messages are in English. Defense and material names come from the game, and the game's own refusal reasons are shown as the game words them, which can be Chinese.

## How it works

Repairs and removals join the end of your character's action queue, so actions you queued yourself stay and run first. Pressing Repair all or Clear all broken again while an earlier batch is still queued lines up only what that batch did not cover, and says how many are still queued from before. If the game is ever in its replace-the-queue action mode while you have actions queued, a press does nothing but say "Finish or cancel your queued actions first.", so your actions are never thrown away.

Costs, the materials you have, and the game's own repair check are all read at the moment you press. Repair all keeps back the materials the repairs already queued will need.

## Known issues and limits

This mod repairs and removes defenses and uses your materials. Back up your save before trying it for the first time.

- Nothing can be repaired or removed during a zombie wave or while you are away from home. The panel says so and its buttons wait.
- The game is paused while the Layout screen is open, so repairs and removals you queue there wait. The character starts on them when you leave Layout (or, if you had paused the game before opening Layout, when you unpause). The message after each press says so.
- The Layout buttons sit at the top middle of the screen. If they cover something you need on your screen size, turn them off with `LayoutControls` in the config; `F7` and the HUD button still work.
- If something else uses the materials before the character reaches a defense, the game decides what happens to that repair, not the mod.
- Nearest-next is measured in a straight line from where the character stands, not along the path they walk.
- Rows are listed by name. The walking order only applies to Repair all and Clear all broken.
- Robot repair is not included.
- A removal you did not mean can be cancelled from the action queue before the character gets to it, like any other queued action. There is no confirmation box.
- Colour and other formatting tags in the game's refusal reasons are removed.
- `BepInEx\defense-manager.log` is never trimmed. Delete it while the game is closed if it gets large.

If a defense is missing from the list, a repair uses the wrong materials, or a press does something you did not expect, please tell me (see [Feedback](#feedback-and-bug-reports)).

## Requirements

- Survival Log 1.1.18317. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Download `DefenseManager.dll` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-DefenseManager/releases/latest) and put it in the `BepInEx\plugins` folder inside the game folder (the folder with the game .exe). Full path example: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\DefenseManager.dll`

## Uninstall

Delete `DefenseManager.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.defensemanager.cfg` and `BepInEx\defense-manager.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Tested on Survival Log 1.1.18317 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Defense Manager 0.2.0 started, key F7, Layout controls on`. The same line, plus one line for every press saying what was queued and why anything was left, is in `BepInEx\defense-manager.log`, which survives restarts.

If `F7` does nothing, check that you are in a loaded game past the prologue, and that no other mod uses the same key. You can pick another key in the config, or use the HUD button.

If the Layout buttons do not show, check that `LayoutControls` is `true` in the config and that you are in a loaded game past the prologue. They can take a second to appear after Layout opens.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-DefenseManager/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\defense-manager.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/DefenseManager -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\DefenseManager\bin\Release\DefenseManager.dll`.

The unit tests cover the pure rules (which defenses each button acts on, the walking order, the material budget, the panel view, the Layout buttons and the messages) and need nothing from the game:

```
dotnet test tests/DefenseManager.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
