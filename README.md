> **AI-written from operator direction.** The intent is the operator's; the wording and scope are an agent's.
> `session: 127889d4-f8d8-4d17-8ded-f6f46da456fc | 2026-10-12 | source: op:2026-10-11-0818-c1e8`

# Defense Manager

A BepInEx 6 (IL2CPP) plugin for Survival Log: one panel for the traps and barricades outside your home. Repair them all in one press, clear away the broken ones, or repair and remove them one by one without walking to each and clicking through its menu.

## What it does

- **Repair all** queues a repair for every damaged outdoor defense your materials cover. The character walks them nearest-next from where they stand, so the round does not zigzag across the yard. A defense the materials left over cannot cover is skipped and the walk goes on to the next nearest one they can, so one costly repair does not hold back the cheap ones.
- Whatever Repair all had to leave is listed in the message: which defenses are short and exactly what each is missing, plus any the game itself refuses to repair right now, with the game's reason.
- **Clear all broken** queues the removal of every broken defense, nearest-next. A broken defense you queued for repair is kept: you chose to fix it.
- Each defense gets its own row with its name, icon, condition bar, state (broken, damaged or intact), repair cost, and a **Repair** and a **Remove** button. A button that would be refused is greyed out and says why underneath it (not enough materials, the game's own reason, already queued).
- **Remove** takes the defense down without the game's are-you-sure box. The box belongs to the defense's menu; the removal itself is the same action the menu queues.
- The top bar shows each repair material as free / needed: what is left after the repairs already queued, against what the repairs not yet queued would take. A material you are short of is outlined in red.
- Every repair and removal is the game's own action, the same one the defense's menu lines up, so the game's own walking, timing and material use apply. Only outdoor defenses are listed; doors and windows are not touched.

## Controls

- `F7` opens and closes the panel in a loaded game. On the title screen and in the prologue it does nothing.
- `Esc` closes the panel.
- **HUD button**: a shield at the top of the tool column in the bottom-left corner of the screen, above the game's own buttons there. It opens and closes the panel the same as `F7`, and hovering it shows its name and key. It is only shown in a loaded game past the prologue, and it hides with the rest of the column in build mode.
- In the panel: **Repair all (n)** and **Clear all broken (n)** at the top (n is how many each would act on now), **Repair** and **Remove** on each row, and the close button in the top-right corner.

Repairs and removals join the end of your character's action queue, so actions you queued yourself stay and run first. Pressing Repair all or Clear all broken again while an earlier batch is still queued lines up only what that batch did not cover, and says how many are still queued from before. If the game is ever in its replace-the-queue action mode while you have actions queued, a press does nothing but say to finish or cancel them first, so your actions are never thrown away.

The game's own hotkeys are off while the panel is open, and come back when it closes. `F7` was picked to stay clear of the keys other mods use: `F8` (Storage Hub), `F9` (Unpack All) and `F10` (Settings Hub).

## Config

`BepInEx/config/com.dev.survivallog.defensemanager.cfg`:

| Setting | Default | What it does |
| --- | --- | --- |
| `Controls/PanelKey` | `F7` | Key that opens and closes the panel. `None` turns the key off; the HUD button still works. |

Log: `BepInEx/defense-manager.log`.

## Known limits

- Nothing can be repaired or removed during a zombie wave or while you are away from home; the panel says so and its buttons wait.
- Costs and the game's repair check are read when you press. If something else uses the materials before the character reaches a defense, the game decides what happens to that repair, not the mod.
- Nearest-next is measured in a straight line from where the character stands, not along the path they walk.
- Rows are listed by name. The walking order only applies to the two buttons at the top.
- Robot repair is not included.
- A removal you did not mean can be cancelled from the action queue before the character gets to it, like any other queued action.
- The game's refusal reasons are shown as the game words them, which can be Chinese. Colour and other rich-text tags in them are removed.

## Install

Copy `DefenseManager.dll` into `BepInEx/plugins`. Needs BepInEx 6 (IL2CPP build).

## Build and test

```sh
dotnet build src/DefenseManager -c Release -p:GameDir="D:\path\to\Survival Log"
dotnet test tests/DefenseManager.Tests
```

The build references BepInEx and the game's interop assemblies under `GameDir`, so BepInEx must be installed and the game launched once. The tests compile only the pure `Core` code and need nothing from the game.

## License

MIT, see [LICENSE](LICENSE).

## Provenance

Operator, 2026-10-11 (op:2026-10-11-0818-c1e8):

> A mod that queues up repairing all defenses would be amazing too, not doors/windows, but the traps outside, as I find that VERY tedious. Easier removal would be a great mod too.

From the same message, on what the buttons should be:

> A clear all broken defenses could exist, but they are repairable as well so we'd need clear all and repair all. Per defense repair buttons would be exceptional!
