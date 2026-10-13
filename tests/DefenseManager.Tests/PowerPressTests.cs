using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakePowerGame;

namespace DefenseManager.Tests;

public class PowerPressTests
{
    private const PowerMode Global = PowerMode.WaveOnly;

    private static FakePowerGame Home() => new()
    {
        Defenses =
        {
            Unit(1, x: 10, z: 4),
            Unit(2, x: -20, z: 4),
            Unit(3, configId: Lamp, x: 12, z: 4, name: "Flood lamp"),
            Unit(4, configId: Barricade, x: 0, isElectrical: false, name: "Barricade"),
        },
    };

    private static PowerSlot Slot(FakePowerFile file)
    {
        var slot = new PowerSlot(file);

        slot.Follow("Save_01");
        slot.Load();

        return slot;
    }

    private static string Press(PowerTarget target, long id, FakePowerGame game, PowerSlot slot) => PowerPress.Cycle(target, id, game.ReadPower(), slot, Global);

    private static PowerUnit UnitOf(FakePowerGame game, long id) => game.Defenses.Single(defense => defense.Id == id);

    [Fact]
    public void The_defense_button_cycles_that_one_defense_through_every_mode_and_back_to_its_type_setting()
    {
        var game = Home();
        var slot = Slot(new FakePowerFile());
        var labels = new List<string>();

        for (var press = 0; press < 5; press++)
        {
            Press(PowerTarget.Defense, 1, game, slot);
            labels.Add(new PowerPolicy(slot.Settings, Global).DefenseLabel(UnitOf(game, 1).Key));
        }

        Assert.Equal(new[] { "Always on", "Wave only", "Proximity", "Default", "Type setting (Wave only)" }, labels);
    }

    [Fact]
    public void The_defense_button_changes_only_that_defense()
    {
        var game = Home();
        var slot = Slot(new FakePowerFile());

        Press(PowerTarget.Defense, 1, game, slot);

        var policy = new PowerPolicy(slot.Settings, Global);

        Assert.Equal(PowerMode.AlwaysOn, policy.ModeOf(UnitOf(game, 1).Key));
        Assert.Equal(Global, policy.ModeOf(UnitOf(game, 2).Key));
    }

    [Fact]
    public void The_type_button_cycles_every_defense_of_the_type_and_back_to_the_global_setting()
    {
        var game = Home();
        var slot = Slot(new FakePowerFile());
        var labels = new List<string>();

        for (var press = 0; press < 5; press++)
        {
            Press(PowerTarget.Type, 1, game, slot);
            labels.Add(new PowerPolicy(slot.Settings, Global).TypeLabel(Coil));
        }

        Assert.Equal(new[] { "Always on", "Wave only", "Proximity", "Default", "Global setting (Wave only)" }, labels);
    }

    [Fact]
    public void The_type_button_reaches_every_defense_of_that_type_and_no_other()
    {
        var game = Home();
        var slot = Slot(new FakePowerFile());

        Press(PowerTarget.Type, 1, game, slot);

        var policy = new PowerPolicy(slot.Settings, Global);

        Assert.Equal(PowerMode.AlwaysOn, policy.ModeOf(UnitOf(game, 1).Key));
        Assert.Equal(PowerMode.AlwaysOn, policy.ModeOf(UnitOf(game, 2).Key));
        Assert.Equal(Global, policy.ModeOf(UnitOf(game, 3).Key));
    }

    /// <summary>
    /// The page sends an instance id; the setting is filed under the type and spot of the defense standing there at the
    /// press, so a defense built again in the same spot (with a new id) keeps it.
    /// </summary>
    [Fact]
    public void A_setting_lands_on_the_spot_of_the_defense_pressed_and_holds_for_one_rebuilt_there()
    {
        var game = Home();
        var slot = Slot(new FakePowerFile());

        Press(PowerTarget.Defense, 2, game, slot);

        var rebuilt = Unit(9, x: -20, z: 4);

        Assert.Equal(PowerMode.AlwaysOn, new PowerPolicy(slot.Settings, Global).ModeOf(rebuilt.Key));
    }

    [Fact]
    public void Each_press_says_what_the_setting_is_now()
    {
        var game = Home();
        var slot = Slot(new FakePowerFile());

        Assert.Equal("Tesla coil set to Always on.", Press(PowerTarget.Defense, 1, game, slot));
        Assert.Equal("Every Tesla coil set to Always on.", Press(PowerTarget.Type, 1, game, slot));

        for (var press = 0; press < 3; press++) Press(PowerTarget.Defense, 1, game, slot);

        Assert.Equal("Tesla coil now follows its type setting (Always on).", Press(PowerTarget.Defense, 1, game, slot));

        for (var press = 0; press < 3; press++) Press(PowerTarget.Type, 1, game, slot);

        Assert.Equal("Every Tesla coil now follows the global setting (Wave only).", Press(PowerTarget.Type, 1, game, slot));
    }

    [Fact]
    public void A_defense_with_no_power_switch_or_one_that_is_gone_changes_nothing()
    {
        var game = Home();
        var file = new FakePowerFile();
        var slot = Slot(file);

        Assert.Equal(PowerPress.NoSwitchText, Press(PowerTarget.Defense, 4, game, slot));
        Assert.Equal(PowerPress.NoSwitchText, Press(PowerTarget.Type, 4, game, slot));
        Assert.Equal(PowerPress.NoSwitchText, Press(PowerTarget.Defense, 99, game, slot));
        Assert.Empty(slot.Settings.Defenses);
        Assert.Empty(slot.Settings.Types);
        Assert.Equal("", file.Text);
    }

    [Fact]
    public void With_no_game_loaded_a_press_asks_for_one()
    {
        var game = Home();
        game.IsNoGame = true;

        Assert.Equal(PowerPress.NoGameText, Press(PowerTarget.Defense, 1, game, Slot(new FakePowerFile())));
    }

    [Fact]
    public void A_game_with_no_save_yet_is_asked_to_save_first()
    {
        Assert.Equal(PowerPress.NoSaveText, PowerPress.Cycle(PowerTarget.Defense, 1, Home().ReadPower(), new PowerSlot(new FakePowerFile()), Global));
    }

    [Fact]
    public void A_settings_file_that_cannot_be_read_is_reported_and_nothing_changes()
    {
        var file = new FakePowerFile { IsUnreadable = true };
        var slot = Slot(file);

        Assert.Equal(PowerPress.UnreadableText, Press(PowerTarget.Type, 1, Home(), slot));
        Assert.Empty(slot.Settings.Types);
    }

    [Fact]
    public void A_setting_that_could_not_be_written_says_it_lasts_only_until_the_game_closes()
    {
        var slot = Slot(new FakePowerFile { FailsToWrite = true });

        var message = Press(PowerTarget.Defense, 1, Home(), slot);

        Assert.StartsWith("Tesla coil set to Always on.", message);
        Assert.EndsWith("so it lasts until the game is closed.", message);
    }
}
