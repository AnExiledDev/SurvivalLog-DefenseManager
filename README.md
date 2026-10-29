# Defense Manager

One panel for the traps and barricades outside your home.

Keeping your outdoor defenses up means walking to each one and clicking through its menu. Defense Manager lists them all in one panel: repair every damaged one in one press, clear away the broken ones, or repair and remove them one by one. It can also switch your electrical defenses on and off for you: on during waves and while zombies are at the base, on only when a zombie comes near, or always on. And it lets the Mechanical Chainsaw hit crawler zombies, which dodge it every time in the base game.

## Features

- **Panel key**: press `F7` to open and close the panel at home in a loaded game, past the prologue. `Esc` or the close button in the top-right corner also closes it.
- **HUD button**: a shield at the top of the tool column in the bottom-left corner of the screen, above the game's own buttons there. It opens and closes the panel like `F7`, and hovering it shows "Defense Manager" with the key and "Repair or remove your outdoor defenses". It only shows at home in a loaded game past the prologue, so it is hidden on expeditions, and it hides with the rest of the column on the Layout screen.
- **Layout screen buttons**: while the game's Layout screen is open, three buttons sit at the top middle of the screen, styled like Layout's own buttons: **Repair all (n)**, **Clear all broken (n)** and **Defense list (F7)**. The first two do exactly what the panel's buttons of the same name do. Defense list opens the panel. When the buttons are greyed out, the reason shows under them. They can be turned off in the config.
- **Repair all (n)**: queues a repair for every damaged defense your materials cover (n is how many it would repair now). The character goes nearest-next from where they stand, so the round does not zigzag across the yard. A defense the remaining materials cannot cover is skipped and the round goes on to the next one they can.
- **Clear all broken (n)**: queues the removal of every broken defense, nearest-next. A broken defense you already queued for repair is kept: you chose to fix it.
- **Yard tab**: your yard as its real grid of squares, row 1 against the house and the highest row at the road side, with the house's doors and windows along the wall. Each defense sits on the square it stands on, colored by its state (Broken, Damaged or Intact). Squares you have not unlocked yet are marked with their Survival Points price. A defense on no square shows in a **Not on the grid** strip under the yard.
- **Repairs list**: beside the yard, every defense that needs repair is listed worst first under the **Repair all** button, each with its condition and repair cost. A defense already queued shows "Repair queued" or "Removal queued".
- **Click a defense** on the grid or in the list to see its name, where it stands, its condition, its repair cost, and a **Repair** and a **Remove** button. An electrical defense also shows whether its power is on and its power mode. **< Back to repairs** returns to the list.
- **Click a row number** to see that row: how many defenses and squares of yours it has, what the rest cost, and **Repair row n (count)**, which repairs every damaged defense in that row the way Repair all does. Click an empty square to see whether it is yours.
- **Buttons say why not**: a button that would be refused is greyed out and the reason shows under it: "Needs 3 more Wood", the game's own reason, or "Repair queued: let it finish or cancel it first".
- **Remove without the are-you-sure box**: Remove queues the same removal action as the defense's own menu, without the confirmation box.
- **Short of materials**: when your materials do not cover every repair, a line under Repair all names each material you are short of as free / needed: what is left after the repairs already queued, against what the repairs not yet queued would take.
- **Clear messages**: after each press a line says what was queued and what was left and why. Repair all names up to three defenses that are short of materials and exactly what each is missing. When the panel is closed, the message shows as a short pop-up instead.
- **Power modes for electrical defenses**: the mod switches each electrical defense's power for you, in one of four modes (see [Power modes](#power-modes)). Click one to see whether its power is on and to pick its own mode. The **Power** tab sets the mode for every defense of a type.
- **Settings tab**: the Layout screen buttons, the default power mode, the Proximity range, the crawler dodge chances and the device attack rate can be changed in the panel, and are saved to the config at once. The panel key shows there but is changed in the config.
- **Game hotkeys paused**: the game's own hotkeys are off while the panel is open and come back when it closes, so keys you press in the panel do not reach the game.
- **Device attack rate**: `DeviceAttackRate` in the config sets how often defense devices strike. 2 strikes twice as often. A powered device uses power on every strike, so a faster rate uses more power. It is 1, the game's own rate, by default.
- **Crawlers can be hit by the chainsaw**: in the base game crawler zombies always dodge the Mechanical Chainsaw, so its blade never hits them. With the mod they dodge it only 25% of the time. `CrawlerDodgeChance` and `ArmoredCrawlerDodgeChance` in the config set the chance for each kind of crawler, from 0 (every hit lands) to 1 (the game's own behavior).
- **UI size**: with Exiled's Toolkit 0.10.0 or newer, its UI size setting (`UiScale`) draws the panel and its pop-up messages bigger or smaller, as far as the panel still fits on the screen. The HUD button and the Layout screen buttons keep the game's size, like the game's own buttons around them. With Toolkit 0.11.0 or newer, Defense Manager's own `UiScale` under Interface sets a size for this mod alone; at 0, the default, it follows the Toolkit's.
- **Log file**: every press and what came of it is written to `BepInEx\defense-manager.log`.

Every repair and removal is the game's own action, the same one the defense's menu lines up, so the game's own walking, timing and material use apply. Only defenses outside your home are listed; doors and windows are not touched.

Defense Manager writes nothing into your save file. Its power settings are its own data, kept for each save in `BepInEx\config\com.dev.survivallog.defensemanager.power.txt`. The repairs and removals it queues are real: they use your materials and take defenses down, exactly as if you had used each defense's menu. You can remove the mod at any time; your defenses stay as they are, and each electrical defense keeps the power state the mod last switched it to, because the game saves that switch.

## Hotkeys

| Key | What it does |
| --- | --- |
| `F7` | Opens and closes the panel (at home in a loaded game, past the prologue). |
| `Esc` | Closes the panel. |

`F7` can be changed or turned off in the config. The HUD button works either way.

The mod's panel, buttons and messages follow the game's language: English or Simplified Chinese. Defense and material names, and the game's own refusal reasons, come from the game as it words them.

## How it works

Repairs and removals join the end of your character's action queue, so actions you queued yourself stay and run first. Pressing Repair all or Clear all broken again while an earlier batch is still queued lines up only what that batch did not cover, and says how many are still queued from before. If the game is ever in its replace-the-queue action mode while you have actions queued, a press does nothing but say "Finish or cancel your queued actions first.", so your actions are never thrown away.

Costs, the materials you have, and the game's own repair check are all read at the moment you press. Repair all keeps back the materials the repairs already queued will need.

### Power modes

Each electrical defense runs in one of four modes:

- **Wave only**: on during zombie waves and while zombies are at the base, such as a stray one between waves, off otherwise.
- **Proximity**: on while a living zombie is within `ProximityRange` (15 m by default, measured along the ground), and off once no zombie has been in range for 10 minutes of game time.
- **Always on**: always on.
- **Default**: the mod leaves the defense alone, and the game's own power switch decides, as if the mod were not installed.

The mode comes from three levels, and the first one that has a setting wins:

1. **This defense**: picked when you click the defense on the grid. **Same as every (name) (mode)** means it has no setting of its own and follows its type.
2. **Its type**: picked on the **Power** tab, and shared by every defense of that type in this save. **Global (mode)** means the type has no setting and follows the global mode. The tab also lists every defense of that type set on its own, with **Follow the type** to clear its setting.
3. **Global**: `DefaultMode` in the config or **Default power mode** on the Settings tab, **Wave only** unless you change it.

The mode picked shows highlighted. A setting belongs to the defense's type and the spot it stands on, so a defense you take down and build again in the same place keeps it.

The mod checks the defenses about twice a second whenever a game is loaded, with the panel open or closed and while you are away from home. It only switches a defense when its power is not already where the mode wants it, and each switch is written to the log with the reason. Only electrical defenses that are whole and working are switched: a broken one, or one the game counts as not working, is left as it is, and the panel says so when you click it. Settings are kept for each save, so a game is only switched once it has been saved.

## Known issues and limits

- Nothing can be repaired or removed during a zombie wave. The panel says so and its buttons wait.
- On an expedition the panel, the HUD button and the Layout buttons are hidden and `F7` does nothing, since your defenses are back home. They come back when you return home.
- The game is paused while the Layout screen is open, so repairs and removals you queue there wait. The character starts on them when you leave Layout (or, if you had paused the game before opening Layout, when you unpause). The message after each press says so.
- The Layout buttons sit at the top middle of the screen. If they cover something you need on your screen size, turn them off with `LayoutControls` in the config; `F7` and the HUD button still work.
- If something else uses the materials before the character reaches a defense, the game decides what happens to that repair, not the mod.
- Nearest-next is measured in a straight line from where the character stands, not along the path they walk.
- The walking order only applies to Repair all, Repair row and Clear all broken.
- Repair row counts a defense as in a row by the square it stands on, so a defense in the Not on the grid strip is only repaired by Repair all or by itself.
- A queued repair or removal cannot be cancelled from the panel. Cancel it from the game's action queue.
- Robot repair is not included.
- A removal you did not mean can be cancelled from the action queue before the character gets to it, like any other queued action. There is no confirmation box.
- A defense in any mode but Default is switched back to what its mode wants within a second if you flip its power by hand from its own menu. Set it to Default to switch it yourself.
- Nothing is switched while the Layout screen is open or while the game locks its power switches.
- Repairing an electrical defense makes the game switch it on, which costs one tick of power even when its mode has the mod switch it off again straight after.
- Power settings belong to a save file. A game that carries on under a new save file name, such as after an ending or in endless mode, starts with no defense or type settings of its own.
- The power modes do not show, and nothing is switched, in a new game until it has been saved once.

## Requirements

- Survival Log 1.1.19611. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start. With 0.6.0 or newer, the HUD's tool buttons are also laid out four high, in columns, when there are more than four. With 0.10.0 or newer, its UI size setting also sizes the panel.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Optional, to change settings without a restart: install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `DefenseManager-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-DefenseManager/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/54).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\DefenseManager.dll`

## Uninstall

Delete `DefenseManager.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, `BepInEx\config\com.dev.survivallog.defensemanager.power.txt` and `BepInEx\defense-manager.log`.

Each electrical defense stays in the power state the mod last switched it to, because the game saves that switch. Switch any you want changed from the defense's own menu.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed on the Mods screen, every setting takes effect at once except `DeviceAttackRate`, which takes effect at the next game start. All but `PanelKey` can also be changed on the panel's Settings tab, which saves to the same file and works the same with or without Exiled's Toolkit. Each setting in the cfg file is explained in English and Simplified Chinese. See [CONFIG.md](CONFIG.md) for every setting. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

## Troubleshooting

Tested on Survival Log 1.1.19611 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Defense Manager 0.7.4 started, device attack rate 1`, followed by `key F7, Layout controls on` and `power default WaveOnly, proximity range 15 m` (the words change with your settings, and a setting changed on the Mods screen writes its line again). The same lines, plus one line for every press saying what was queued and why anything was left, and one for every power switch with its reason, is in `BepInEx\defense-manager.log`, which survives restarts.

If an electrical defense is not switched, click it in the panel: a broken or not working defense is left alone, and a defense in Default mode is left to the game. If no defense shows power modes, look in `BepInEx\defense-manager.log` for a line about the power settings.

If `F7` does nothing, check that you are at home in a loaded game past the prologue, and that no other mod uses the same key. You can pick another key in the config, or use the HUD button.

If the Layout buttons do not show, check that `LayoutControls` is `true` in the config and that you are at home in a loaded game past the prologue. They can take a second to appear after Layout opens.

## Changelog

- 0.7.0: a new panel: the yard as its real grid of squares, Repair row, a Power tab for each type's power mode, a Settings tab, and a warm gold look.
- 0.6.0: settings can be changed on the Mod Manager's Mods screen without a restart (all but `DeviceAttackRate`). Works with Exiled's Toolkit 0.4.0 or newer, which is optional.
- 0.5.0: new config setting `DeviceAttackRate` sets how often defense devices strike.
- 0.4.0: the Mechanical Chainsaw now hits crawler zombies, which used to dodge it every time. They dodge it 25% of the time by default. New config settings `CrawlerDodgeChance` and `ArmoredCrawlerDodgeChance`.
- 0.3.3: Repair and Repair all no longer assume every repair costs 10 stamina. Repairs often cost less, so Repair all used to stop short. Now repairs are only held back when you have less than the 10 stamina a repair needs to start, and not at all when actions you queued run first. If your stamina runs out partway, the game refuses the rest.
- 0.3.2: the panel, the HUD button's tip, the Layout buttons and every message are now in Simplified Chinese when the game is.
- 0.3.1: Repair and Repair all now work. Before, every repair was refused as "can't be repaired right now" even when the game would allow the repair. Repair all also lines up only as many repairs as your stamina covers (each needs 10), so none are refused partway through.
- 0.3.0: power modes for electrical defenses (Wave only, Proximity, Always on, Default), set for each defense, for each type, or for all in the config. New config settings `DefaultMode` and `ProximityRange`.
- 0.2.2: plainer setting descriptions in the config file, now also in Simplified Chinese.
- 0.2.1: the panel, HUD button, Layout buttons and F7 are off on expeditions.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also post in the Posts tab of the mod's Nexus Mods page. When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\defense-manager.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/DefenseManager -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\DefenseManager\bin\Release\DefenseManager.dll`.

The unit tests cover the pure rules (which defenses each button acts on, the walking order, the material budget, the panel view, the Layout buttons, the messages, and the power modes and settings) and need nothing from the game:

```
dotnet test tests/DefenseManager.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
