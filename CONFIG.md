# Defense Manager settings

Settings live in `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

## Controls

| Setting | Default | What it does |
| --- | --- | --- |
| `PanelKey` | `F7` | Key that opens and closes the Defense Manager panel. `None` turns the key off; the HUD button still works. |
| `LayoutControls` | `true` | Shows Repair all, Clear all broken and a Defense list button at the top middle of the game's Layout screen. `false` hides them; the panel, `F7` and the HUD button work either way. |

For `PanelKey`, the value is a Unity key name, for example `F7`, `F11`, `Insert` or `Keypad5`. `None` turns the key off. `Esc` always closes the open panel and cannot be changed. The panel, the HUD button and the Layout screen's Defense list button show the key you pick.

The game is paused while the Layout screen is open, so anything queued from its buttons starts when you leave Layout.
