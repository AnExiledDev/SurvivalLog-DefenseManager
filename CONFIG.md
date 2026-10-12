# Defense Manager settings

Settings live in `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

## Controls

| Setting | Default | What it does |
| --- | --- | --- |
| `PanelKey` | `F7` | Key that opens and closes the Defense Manager panel. `None` turns the key off; the HUD button still works. |

The value is a Unity key name, for example `F7`, `F11`, `Insert` or `Keypad5`. `None` turns the key off. `Esc` always closes the open panel and cannot be changed. The panel and the HUD button show the key you pick.
