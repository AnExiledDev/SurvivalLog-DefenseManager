Name: Defense Manager
Summary: Repair every damaged trap and barricade in one press, clear all the broken ones in another, or handle them one by one from a single panel on F7 or a HUD button. Electrical defenses switch on only when zombies show up, and crawlers dodge the chainsaw 25% of the time instead of always. Repair all is on the Layout screen too.

Description (BBCode):
Keeping your outdoor defenses up means walking to each one and clicking through its menu. Defense Manager lists them all in one panel, so you can repair or clear them in one press.

[size=5]Main features[/size]
[list]
[*][b]Repair all[/b]: queues a repair for every damaged defense your materials cover, nearest first.[/*]
[*][b]Clear all broken[/b]: queues the removal of every broken defense. One you already queued for repair is kept.[/*]
[*][b]Per-defense rows[/b]: name, state, condition bar, repair cost, and Repair and Remove buttons.[/*]
[*][b]Buttons say why not[/b]: a greyed-out button shows the reason, like "Needs 3 more Wood".[/*]
[*][b]Materials bar[/b]: each repair material as free / needed, outlined red when you are short.[/*]
[*][b]Remove without the are-you-sure box[/b]: the same removal as the defense's own menu.[/*]
[*][b]HUD button[/b]: a shield at the top of the bottom-left tool column opens the panel.[/*]
[*][b]Layout screen buttons[/b]: while the game's Layout screen is open, Repair all, Clear all broken and Defense list sit at the top middle of the screen, styled like Layout's own buttons. Defense list opens the panel. Can be turned off in the config.[/*]
[*][b]Game's own actions[/b]: repairs and removals join the end of your action queue, so your own queued actions run first.[/*]
[*][b]Power modes[/b]: electrical defenses are switched for you. [b]Wave only[/b] keeps them on during waves and while zombies are at the base, and off otherwise, [b]Proximity[/b] turns them on while a zombie is in range (15 m by default) and off after 10 game minutes with none, [b]Always on[/b] keeps them on, and [b]Default[/b] leaves them to the game's own switch.[/*]
[*][b]Per defense, per type, or for all[/b]: each electrical row has a [b]This one[/b] button for that defense and an [b]Every (name)[/b] button for all of its type. Anything without a setting follows its type, and a type without one follows the mode in the config (Wave only unless you change it).[/*]
[*][b]Crawlers can be hit by the chainsaw[/b]: in the base game crawlers always dodge the Mechanical Chainsaw. With the mod they dodge it 25% of the time, and the chance for each kind of crawler can be set in the config.[/*]
[/list]

[size=5]Hotkeys[/size]
[list]
[*][b]F7[/b]: open or close the panel (at home in a loaded game, past the prologue).[/*]
[*][b]Esc[/b]: close the panel.[/*]
[/list]
F7 can be changed or turned off in the config. The game's own hotkeys are off while the panel is open.

The mod writes nothing into your save file; its power settings are its own data, kept for each save in [font=Courier New]BepInEx\config\com.dev.survivallog.defensemanager.power.txt[/font]. The repairs and removals it queues are real: they use your materials and take defenses down. You can remove it at any time; your defenses stay as they are, and each electrical defense keeps the power state the mod last switched it to, because the game saves that switch.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]Nothing can be repaired or removed during a zombie wave.[/*]
[*]On an expedition the panel, the HUD button and the Layout buttons are hidden, since your defenses are back home.[/*]
[*]The game is paused while the Layout screen is open, so anything you queue there starts when you leave Layout (or when you unpause, if you had paused before).[/*]
[*]The Layout buttons sit at the top middle of the screen. If they cover something on your screen size, turn them off in the config.[/*]
[*]Nearest first is measured in a straight line, not along the path the character walks.[/*]
[*]Robot repair is not included.[/*]
[*]Remove has no confirmation box. A removal you did not mean can be cancelled from the action queue.[/*]
[*]A defense in any mode but Default is switched back if you flip its power by hand. Set it to Default to switch it yourself.[/*]
[*]Nothing is switched while the Layout screen is open. Power settings are kept per save, so a new game is only switched once it has been saved.[/*]
[*]Repairing an electrical defense makes the game switch it on, which costs one tick of power even when its mode has the mod switch it off again straight after.[/*]
[*]Power settings belong to a save file. A game that carries on under a new save file name, such as after an ending or in endless mode, starts with no defense or type settings of its own.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19131. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\DefenseManager.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]DefenseManager.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. Each electrical defense stays in the power state the mod last switched it to; switch any you want changed from the defense's own menu.

[size=5]Configuration[/size]
To change the key, turn off the Layout screen buttons, or set the global power mode ([font=Courier New]DefaultMode[/font]) and the Proximity range ([font=Courier New]ProximityRange[/font]), edit [font=Courier New]BepInEx\config\com.dev.survivallog.defensemanager.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once ([font=Courier New]DeviceAttackRate[/font] at the next game start) (see [url=https://github.com/AnExiledDev/SurvivalLog-DefenseManager/blob/main/CONFIG.md]CONFIG.md[/url]; the cfg file explains each setting in English and Chinese). Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\defense-manager.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Coming soon: a Survival Log wiki with detailed documentation for all of my mods.

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-DefenseManager]github.com/AnExiledDev/SurvivalLog-DefenseManager[/url]

[line]
[size=5]简体中文[/size]

维护家外面的防御设施，意味着要走到每一个跟前点开它的菜单。Defense Manager 把它们全部列在一个面板里，一键就能修理或清除。

[size=5]主要功能[/size]
[list]
[*][b]全部修理[/b]：为材料够用的每个受损防御设施排队修理，最近的优先。[/*]
[*][b]清除全部损毁[/b]：为每个损毁的防御设施排队拆除。已经排队修理的会保留。[/*]
[*][b]每个防御设施一行[/b]：名称、状态、耐久条、修理费用，以及「修理」和「拆除」按钮。[/*]
[*][b]按钮会说明原因[/b]：灰掉的按钮下方会显示原因，例如「还差 3 个木材」。[/*]
[*][b]材料栏[/b]：每种修理材料显示为 可用 / 所需，不够时用红框标出。[/*]
[*][b]拆除时不弹确认框[/b]：和防御设施自己菜单里的拆除完全一样。[/*]
[*][b]HUD 按钮[/b]：左下角工具栏最上方的盾牌按钮可以打开面板。[/*]
[*][b]「规划模式」界面按钮[/b]：打开游戏的「规划模式」界面时，「全部修理」「清除全部损毁」「防御列表」三个按钮会出现在屏幕顶部中间，样式和「规划模式」自己的按钮一致。「防御列表」会打开面板。可以在配置中关闭。[/*]
[*][b]使用游戏自己的行动[/b]：修理和拆除会排到你的行动队列末尾，你自己排好的行动先执行。[/*]
[*][b]供电模式[/b]：模组会替你切换电力防御设施。[b]仅尸潮[/b]在尸潮期间和基地有僵尸时开启，其余时间关闭；[b]僵尸靠近[/b]在有僵尸进入范围（默认 15 米）时开启，连续 10 分钟游戏时间没有僵尸后关闭；[b]常开[/b]一直开启；[b]默认[/b]交给游戏自己的开关。[/*]
[*][b]单个、同类或全部[/b]：每个电力防御设施的一行都有「这个」按钮设置这一个，和「所有（名称）」按钮设置同类的全部。没有单独设置的跟随它的类型，类型也没有设置的跟随配置里的模式（默认是仅尸潮）。[/*]
[*][b]电锯能打到爬行者[/b]：原版游戏中爬行者总能闪避「机械电锯」。本模组把闪避概率降到 25%，每种爬行者的概率都可以在配置里设置。[/*]
[/list]

[size=5]快捷键[/size]
[list]
[*][b]F7[/b]：打开或关闭面板（在家中、已载入游戏且过了序章时）。[/*]
[*][b]Esc[/b]：关闭面板。[/*]
[/list]
F7 可以在配置中修改或停用。面板打开时游戏自己的快捷键不起作用。

模组不会往存档里写入任何数据；它的供电设置是它自己的数据，按存档分别保存在 [font=Courier New]BepInEx\config\com.dev.survivallog.defensemanager.power.txt[/font]。它排队的修理和拆除都是真实的：会消耗你的材料，也会拆掉防御设施。你可以随时移除它；防御设施保持原样，每个电力防御设施会保持模组最后一次切换的供电状态，因为游戏会保存这个开关。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]尸潮期间无法修理或拆除任何防御设施。[/*]
[*]外出探索时，面板、HUD 按钮和「规划模式」界面上的按钮都会隐藏，因为你的防御设施在家里。[/*]
[*]「规划模式」界面打开时游戏是暂停的，所以在那里排队的行动会在离开「规划模式」后开始（如果之前已经暂停，则在取消暂停后开始）。[/*]
[*]「规划模式」界面上的按钮在屏幕顶部中间。如果在你的屏幕尺寸下挡住了东西，可以在配置中关闭。[/*]
[*]「最近优先」按直线距离计算，而不是角色实际走的路线。[/*]
[*]不包括机器人修理。[/*]
[*]拆除没有确认框。误拆的可以在行动队列中取消。[/*]
[*]除「默认」以外的模式下，你手动切换防御设施的电源后，模组会把它切换回来。想自己切换的话，把它设为「默认」。[/*]
[*]「规划模式」界面打开时不会切换任何东西。供电设置按存档保存，所以新游戏要先保存一次才会开始切换。[/*]
[*]修理电力防御设施时游戏会把它打开，即使它的模式随后让模组马上关闭它，也会消耗一次电量。[/*]
[*]供电设置属于某个存档文件。以新存档文件名继续的游戏（例如结局之后或无尽模式）开始时没有任何单个或类型设置。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19131。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\DefenseManager.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]DefenseManager.dll[/font]。每个电力防御设施会保持模组最后一次切换的供电状态；想改的话，用该防御设施自己的菜单切换。

[size=5]配置[/size]
如需修改按键、关闭「规划模式」界面上的按钮，或设置全局供电模式（[font=Courier New]DefaultMode[/font]）和靠近触发的范围（[font=Courier New]ProximityRange[/font]），可以在游戏内 Mod Manager 的「模组」界面修改（立即生效，[font=Courier New]DeviceAttackRate[/font] 在下次启动游戏时生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.defensemanager.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-DefenseManager/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。没装 Exiled's Toolkit 的话，在「模组」界面的改动改为在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\defense-manager.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
即将推出：Survival Log 维基，收录我所有模组的详细说明。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-DefenseManager]github.com/AnExiledDev/SurvivalLog-DefenseManager[/url]
