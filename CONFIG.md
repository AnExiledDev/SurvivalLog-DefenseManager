# Defense Manager settings

Settings live in `BepInEx\config\com.dev.survivallog.defensemanager.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

## Controls

| Setting | Default | What it does |
| --- | --- | --- |
| `PanelKey` | `F7` | Key that opens and closes the Defense Manager panel. `None` turns the key off; the shield button in the bottom-left corner of the screen still opens the panel. |
| `LayoutControls` | `true` | Shows Repair all, Clear all broken and Defense list buttons at the top middle of the game's Layout screen. `false` hides them; the panel key and the shield button still work. |

For `PanelKey`, the value is a Unity key name, for example `F7`, `F11`, `Insert` or `Keypad5`. `None` turns the key off. `Esc` always closes the open panel and cannot be changed. The panel, the shield button and the Layout screen's Defense list button show the key you pick.

The game is paused while the Layout screen is open, so anything queued from its buttons starts when you leave Layout.

## Power

| Setting | Default | What it does |
| --- | --- | --- |
| `DefaultMode` | `WaveOnly` | How electrical defenses are powered when neither the defense nor its type has a setting of its own in the panel. `WaveOnly`: on during waves and while zombies are at the base, off otherwise. `AlwaysOn`: always on. `Proximity`: on while a living zombie is within `ProximityRange`, off once none has been in range for 10 minutes of game time. `Default`: the mod leaves the defense alone and the game's own power switch decides. |
| `ProximityRange` | `15` | How close, in meters, a living zombie must come to turn on a defense set to Proximity. Measured along the ground; height does not count. Any number from 1 to 100. |

A defense's own setting wins over its type's, and a type's setting wins over `DefaultMode`. Those two are set with the buttons on each electrical defense's row in the panel, not in this file. They are kept for each save in `BepInEx\config\com.dev.survivallog.defensemanager.power.txt`, which the mod writes itself; edit it only while the game is closed, or delete it to clear every defense and type setting.

Changing `DefaultMode` here changes every defense and type that has no setting of its own.

Repairing an electrical defense makes the game switch it on, which costs one tick of power even when its mode has the mod switch it off again straight after.

The defense and type settings belong to a save file. A game that carries on under a new save file name, such as after an ending or in endless mode, starts with none of its own and follows `DefaultMode`.

## Combat

| Setting | Default | What it does |
| --- | --- | --- |
| `DefensesHitCrawlers` | `true` | Every defense that does damage also hits crawler zombies. In the base game crawlers dodge some defenses, and the chainsaw never hits them. `false` keeps the game's dodging. |
