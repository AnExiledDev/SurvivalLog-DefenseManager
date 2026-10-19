# Defense Manager

One panel for the traps and barricades outside your home.

Keeping your outdoor defenses up means walking to each one and clicking through its menu. Defense Manager lists them all in one panel: repair every damaged one in one press, clear away the broken ones, or repair and remove them one by one. It can also switch your electrical defenses on and off for you: on during waves and while zombies are at the base, on only when a zombie comes near, or always on. And it lets the Mechanical Chainsaw hit crawler zombies, which dodge it every time in the base game.

## Features

- **Panel key**: press `F7` to open and close the panel at home in a loaded game, past the prologue. `Esc` or the close button in the top-right corner also closes it.
- **HUD button**: a shield at the top of the tool column in the bottom-left corner of the screen, above the game's own buttons there. It opens and closes the panel like `F7`, and hovering it shows "Defense Manager" with the key and "Repair or remove your outdoor defenses". It only shows at home in a loaded game past the prologue, so it is hidden on expeditions, and it hides with the rest of the column on the Layout screen.
- **Layout screen buttons**: while the game's Layout screen is open, three buttons sit at the top middle of the screen, styled like Layout's own buttons: **Repair all (n)**, **Clear all broken (n)** and **Defense list (F7)**. The first two do exactly what the panel's buttons of the same name do. Defense list opens the panel with every defense on its own row. When the buttons are greyed out, the reason shows under them. They can be turned off in the config.
- **Repair all (n)**: queues a repair for every damaged defense your materials cover (n is how many it would repair now). The character goes nearest-next from where they stand, so the round does not zigzag across the yard. A defense the remaining materials cannot cover is skipped and the round goes on to the next one they can.
- **Clear all broken (n)**: queues the removal of every broken defense, nearest-next. A broken defense you already queued for repair is kept: you chose to fix it.
- **One row per defense**: icon, name, state (Broken, Damaged or Intact), a condition bar, the repair cost, and a **Repair** and a **Remove** button. A defense already queued shows "Repair queued" or "Removal queued".
- **Buttons say why not**: a button that would be refused is greyed out and the reason shows under it: "Needs 3 more Wood", the game's own reason, or "Repair queued: let it finish or cancel it first".
- **Remove without the are-you-sure box**: Remove queues the same removal action as the defense's own menu, without the confirmation box.
- **Materials bar**: each repair material shows as free / needed: what is left after the repairs already queued, against what the repairs not yet queued would take. A material you are short of is outlined in red.
- **Clear messages**: after each press a line says what was queued and what was left and why. Repair all names up to three defenses that are short of materials and exactly what each is missing. When the panel is closed, the message shows as a short pop-up instead.
- **Power modes for electrical defenses**: the mod switches each electrical defense's power for you, in one of four modes (see [Power modes](#power-modes)). Its row shows whether the power is on, the mode it runs in, and two buttons: **This one: ...** changes the mode of that one defense, and **Every (name): ...** changes it for every defense of that type. Each button shows its current setting.
- **Game hotkeys paused**: the game's own hotkeys are off while the panel is open and come back when it closes, so keys you press in the panel do not reach the game.
- **Device attack rate**: `DeviceAttackRate` in the config sets how often defense devices strike. 2 strikes twice as often. A powered device uses power on every strike, so a faster rate uses more power. It is 1, the game's own rate, by default.
- **Crawlers can be hit by the chainsaw**: in the base game crawler zombies always dodge the Mechanical Chainsaw, so its blade never hits them. With the mod they dodge it only 25% of the time. `CrawlerDodgeChance` and `ArmoredCrawlerDodgeChance` in the config set the chance for each kind of crawler, from 0 (every hit lands) to 1 (the game's own behavior).
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

1. **This defense**: set with the row's **This one** button. A defense with no setting of its own shows **Type setting (mode)**, with the mode it gets from its type.
2. **Its type**: set with the row's **Every (name)** button, and shared by every defense of that type in this save. A type with no setting shows **Global setting (mode)**, with the mode from the config.
3. **Global**: `DefaultMode` in the config, **Wave only** unless you change it.

Each button steps through its own setting: This one goes Type setting, Always on, Wave only, Proximity, Default, then back to Type setting. Every (name) goes Global setting, Always on, Wave only, Proximity, Default, then back to Global setting. A setting belongs to the defense's type and the spot it stands on, so a defense you take down and build again in the same place keeps it.

The mod checks the defenses about twice a second whenever a game is loaded, with the panel open or closed and while you are away from home. It only switches a defense when its power is not already where the mode wants it, and each switch is written to the log with the reason. Only electrical defenses that are whole and working are switched: a broken one, or one the game counts as not working, is left as it is, and its row says so. Settings are kept for each save, so a game is only switched once it has been saved.

## Known issues and limits

- Nothing can be repaired or removed during a zombie wave. The panel says so and its buttons wait.
- On an expedition the panel, the HUD button and the Layout buttons are hidden and `F7` does nothing, since your defenses are back home. They come back when you return home.
- The game is paused while the Layout screen is open, so repairs and removals you queue there wait. The character starts on them when you leave Layout (or, if you had paused the game before opening Layout, when you unpause). The message after each press says so.
- The Layout buttons sit at the top middle of the screen. If they cover something you need on your screen size, turn them off with `LayoutControls` in the config; `F7` and the HUD button still work.
- If something else uses the materials before the character reaches a defense, the game decides what happens to that repair, not the mod.
- Nearest-next is measured in a straight line from where the character stands, not along the path they walk.
- Rows are listed by name. The walking order only applies to Repair all and Clear all broken.
- Robot repair is not included.
- A removal you did not mean can be cancelled from the action queue before the character gets to it, like any other queued action. There is no confirmation box.
- A defense in any mode but Default is switched back to what its mode wants within a second if you flip its power by hand from its own menu. Set it to Default to switch it yourself.
- Nothing is switched while the Layout screen is open or while the game locks its power switches.
- Repairing an electrical defense makes the game switch it on, which costs one tick of power even when its mode has the mod switch it off again straight after.
- Power settings belong to a save file. A game that carries on under a new save file name, such as after an ending or in endless mode, starts with no defense or type settings of its own.
- The power buttons do not show, and nothing is switched, in a new game until it has been saved once.

## Requirements

- Survival Log 1.1.18902. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer, which applies changed settings at once. Defense Manager does not load without it.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit: download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from Nexus Mods, and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `DefenseManager-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-DefenseManager/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/54).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\DefenseManager.dll`

## Uninstall

Delete `DefenseManager.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, `BepInEx\config\com.dev.survivallog.defensemanager.power.txt` and `BepInEx\defense-manager.log`.

Each electrical defense stays in the power state the mod last switched it to, because the game saves that switch. Switch any you want changed from the defense's own menu.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed on the Mods screen, every setting takes effect at once except `DeviceAttackRate`, which takes effect at the next game start. Each setting in the cfg file is explained in English and Simplified Chinese. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Tested on Survival Log 1.1.18902 with BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Defense Manager 0.6.0 started, device attack rate 1`, followed by `key F7, Layout controls on` and `power default WaveOnly, proximity range 15 m` (the words change with your settings, and a setting changed on the Mods screen writes its line again). The same lines, plus one line for every press saying what was queued and why anything was left, and one for every power switch with its reason, is in `BepInEx\defense-manager.log`, which survives restarts.

If an electrical defense is not switched, check its row in the panel: a broken or not working defense is left alone, and a defense in Default mode is left to the game. If no row has power buttons, look in `BepInEx\defense-manager.log` for a line about the power settings.

If `F7` does nothing, check that you are at home in a loaded game past the prologue, and that no other mod uses the same key. You can pick another key in the config, or use the HUD button.

If the Layout buttons do not show, check that `LayoutControls` is `true` in the config and that you are at home in a loaded game past the prologue. They can take a second to appear after Layout opens.

## Changelog

- 0.6.0: settings can be changed on the Mod Manager's Mods screen without a restart (all but `DeviceAttackRate`). Needs Exiled's Toolkit 0.4.0 or newer.
- 0.5.0: new config setting `DeviceAttackRate` sets how often defense devices strike.
- 0.4.0: the Mechanical Chainsaw now hits crawler zombies, which used to dodge it every time. They dodge it 25% of the time by default. New config settings `CrawlerDodgeChance` and `ArmoredCrawlerDodgeChance`.
- 0.3.3: Repair and Repair all no longer assume every repair costs 10 stamina. Repairs often cost less, so Repair all used to stop short. Now repairs are only held back when you have less than the 10 stamina a repair needs to start, and not at all when actions you queued run first. If your stamina runs out partway, the game refuses the rest.
- 0.3.2: the panel, the HUD button's tip, the Layout buttons and every message are now in Simplified Chinese when the game is.
- 0.3.1: Repair and Repair all now work. Before, every repair was refused as "can't be repaired right now" even when the game would allow the repair. Repair all also lines up only as many repairs as your stamina covers (each needs 10), so none are refused partway through.
- 0.3.0: power modes for electrical defenses (Wave only, Proximity, Always on, Default), set for each defense, for each type, or for all in the config. New config settings `DefaultMode` and `ProximityRange`.
- 0.2.2: plainer setting descriptions in the config file, now also in Simplified Chinese.
- 0.2.1: the panel, HUD button, Layout buttons and F7 are off on expeditions.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-DefenseManager/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\defense-manager.log`.

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
