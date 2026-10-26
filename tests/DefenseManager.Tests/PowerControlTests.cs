using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakePowerGame;

namespace DefenseManager.Tests;

public class PowerControlTests
{
    private const double Range = 15;

    private static PowerPolicy Everywhere(PowerMode mode) => new(PowerSettings.None, mode);

    private static FakePowerGame Home(params PowerUnit[] defenses)
    {
        var game = new FakePowerGame();
        game.Defenses.AddRange(defenses);

        return game;
    }

    // ===== Wave only =====

    [Fact]
    public void Wave_only_turns_defenses_on_when_a_wave_starts_and_off_when_it_ends()
    {
        var game = Home(Unit(1), Unit(2, x: 30));
        var control = new PowerControl();

        game.IsWaveRunning = true;
        control.Run(game, Everywhere(PowerMode.WaveOnly), Range);

        Assert.True(game.IsOn(1));
        Assert.True(game.IsOn(2));

        game.IsWaveRunning = false;
        control.Run(game, Everywhere(PowerMode.WaveOnly), Range);

        Assert.False(game.IsOn(1));
        Assert.False(game.IsOn(2));
    }

    [Fact]
    public void Wave_only_turns_a_defense_left_on_off_between_waves()
    {
        var game = Home(Unit(1, isOn: true));

        new PowerControl().Run(game, Everywhere(PowerMode.WaveOnly), Range);

        Assert.False(game.IsOn(1));
    }

    /// <summary>A stray zombie between waves is as much a threat as a wave's, wherever it is at the base.</summary>
    [Fact]
    public void Wave_only_turns_defenses_on_with_no_wave_while_a_zombie_is_alive_at_the_base()
    {
        var game = Home(Unit(1), Unit(2, x: 100));
        game.Zombies.Add(new Spot(200, 0, 0));

        var made = new PowerControl().Run(game, Everywhere(PowerMode.WaveOnly), Range);

        Assert.True(game.IsOn(1));
        Assert.True(game.IsOn(2));
        Assert.Equal("Wave only, zombies are at the base", made[0].Reason);
    }

    [Fact]
    public void Wave_only_turns_defenses_off_once_the_last_zombie_at_the_base_is_gone_and_no_wave_runs()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();
        game.Zombies.Add(new Spot(200, 0, 0));

        control.Run(game, Everywhere(PowerMode.WaveOnly), Range);
        game.Zombies.Clear();
        control.Run(game, Everywhere(PowerMode.WaveOnly), Range);

        Assert.False(game.IsOn(1));
    }

    // ===== Proximity =====

    [Fact]
    public void Proximity_turns_on_only_the_defenses_a_living_zombie_is_in_range_of()
    {
        var game = Home(Unit(1), Unit(2, x: 100, isOn: true));
        game.Zombies.Add(new Spot(10, 0, 0));

        new PowerControl().Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));
        Assert.False(game.IsOn(2));
    }

    [Fact]
    public void Proximity_counts_a_zombie_right_on_the_range_as_in_range_and_one_just_past_it_as_out()
    {
        var onTheEdge = Home(Unit(1));
        var justPast = Home(Unit(1));
        onTheEdge.Zombies.Add(new Spot(15, 0, 0));
        justPast.Zombies.Add(new Spot(15.1, 0, 0));

        new PowerControl().Run(onTheEdge, Everywhere(PowerMode.Proximity), Range);
        new PowerControl().Run(justPast, Everywhere(PowerMode.Proximity), Range);

        Assert.True(onTheEdge.IsOn(1));
        Assert.False(justPast.IsOn(1));
    }

    /// <summary>A zombie on a slope or a roof above a defense is still next to it.</summary>
    [Fact]
    public void Proximity_measures_the_range_along_the_ground_so_height_does_not_count()
    {
        var game = Home(Unit(1, y: 0));
        game.Zombies.Add(new Spot(10, 40, 10));

        new PowerControl().Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));
    }

    [Fact]
    public void Proximity_holds_a_defense_on_for_ten_game_minutes_after_the_last_zombie_left_its_range()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();

        game.Clock = 1000;
        game.Zombies.Add(new Spot(5, 0, 0));
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));

        game.Zombies.Clear();
        game.Clock = 1300;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));

        game.Clock = 1599;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));

        game.Clock = 1600;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.False(game.IsOn(1));
    }

    [Fact]
    public void Proximity_keeps_the_hold_running_from_the_last_time_a_zombie_was_in_range()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();

        game.Zombies.Add(new Spot(5, 0, 0));
        game.Clock = 1000;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);
        game.Clock = 1500;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);
        game.Zombies.Clear();
        game.Clock = 2000;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));
    }

    [Fact]
    public void Proximity_with_no_zombies_about_turns_a_defense_off_and_leaves_an_off_one_off()
    {
        var game = Home(Unit(1, isOn: true), Unit(2));

        new PowerControl().Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.False(game.IsOn(1));
        Assert.False(game.IsOn(2));
        Assert.Equal(0, game.PowerUpsOf(2));
    }

    /// <summary>An earlier save loaded under the same name has its clock go back; that must not stretch the hold.</summary>
    [Fact]
    public void A_clock_that_went_back_ends_the_hold()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();

        game.Clock = 1000;
        game.Zombies.Add(new Spot(5, 0, 0));
        control.Run(game, Everywhere(PowerMode.Proximity), Range);
        game.Zombies.Clear();
        game.Clock = 900;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.False(game.IsOn(1));
    }

    [Fact]
    public void A_new_save_starts_with_no_hold_from_the_last_one()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();

        game.Clock = 1000;
        game.Zombies.Add(new Spot(5, 0, 0));
        control.Run(game, Everywhere(PowerMode.Proximity), Range);
        control.Forget();
        game.Zombies.Clear();
        game.Clock = 1100;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.False(game.IsOn(1));
    }

    /// <summary>
    /// Loading the same save file again is no move to another save, but the new load hands its instance ids out
    /// afresh, so a hold kept by id would switch on whichever defense now has that id.
    /// </summary>
    [Fact]
    public void Loading_the_game_again_drops_every_hold_even_from_the_same_save_file()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();

        game.Clock = 1000;
        game.Zombies.Add(new Spot(5, 0, 0));
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.True(game.IsOn(1));

        game.Load = new GameLoad(2, 2);
        game.Defenses.Clear();
        game.Defenses.Add(Unit(1, x: 50));
        game.Zombies.Clear();
        game.Clock = 1100;
        control.Run(game, Everywhere(PowerMode.Proximity), Range);

        Assert.False(game.IsOn(1));
    }

    // ===== Always on and Default =====

    [Fact]
    public void Always_on_turns_a_defense_on_with_no_wave_and_no_zombie_about()
    {
        var game = Home(Unit(1));

        new PowerControl().Run(game, Everywhere(PowerMode.AlwaysOn), Range);

        Assert.True(game.IsOn(1));
    }

    [Fact]
    public void Default_leaves_every_switch_as_the_game_has_it_through_waves_and_zombies()
    {
        var game = Home(Unit(1, isOn: true), Unit(2));
        var control = new PowerControl();
        var made = new List<PowerSwitch>();

        made.AddRange(control.Run(game, Everywhere(PowerMode.GameDefault), Range));
        game.IsWaveRunning = true;
        game.Zombies.Add(new Spot(1, 0, 0));
        made.AddRange(control.Run(game, Everywhere(PowerMode.GameDefault), Range));

        Assert.True(game.IsOn(1));
        Assert.False(game.IsOn(2));
        Assert.Empty(made);
    }

    // ===== Which defenses, and when =====

    [Fact]
    public void Only_electrical_defenses_that_are_whole_and_working_are_switched()
    {
        var game = Home(Unit(1), Unit(2, isBroken: true), Unit(3, isOperational: false), Unit(4, configId: Barricade, isElectrical: false, name: "Barricade"));

        new PowerControl().Run(game, Everywhere(PowerMode.AlwaysOn), Range);

        Assert.True(game.IsOn(1));
        Assert.False(game.IsOn(2));
        Assert.False(game.IsOn(3));
        Assert.False(game.IsOn(4));
    }

    [Fact]
    public void A_broken_defense_left_on_is_not_switched_off_either()
    {
        var game = Home(Unit(1, isOn: true, isBroken: true));

        new PowerControl().Run(game, Everywhere(PowerMode.WaveOnly), Range);

        Assert.True(game.IsOn(1));
    }

    /// <summary>Switching on costs the grid a power-up, so a defense already in the wanted state is never switched again.</summary>
    [Fact]
    public void A_defense_already_in_the_wanted_state_is_not_switched_again()
    {
        var game = Home(Unit(1, isOn: true), Unit(2));
        var control = new PowerControl();

        var first = control.Run(game, Everywhere(PowerMode.AlwaysOn), Range);
        var second = control.Run(game, Everywhere(PowerMode.AlwaysOn), Range);

        Assert.Equal(0, game.PowerUpsOf(1));
        Assert.Equal(1, game.PowerUpsOf(2));
        Assert.Equal(new long[] { 2 }, first.Select(made => made.Defense.Id));
        Assert.Empty(second);
    }

    [Fact]
    public void Nothing_is_switched_while_the_game_locks_its_power_switches()
    {
        var game = Home(Unit(1));
        game.IsLocked = true;

        var made = new PowerControl().Run(game, Everywhere(PowerMode.AlwaysOn), Range);

        Assert.False(game.IsOn(1));
        Assert.Empty(made);
    }

    [Fact]
    public void Nothing_happens_with_no_game_loaded()
    {
        var game = Home(Unit(1));
        game.IsNoGame = true;

        Assert.Empty(new PowerControl().Run(game, Everywhere(PowerMode.AlwaysOn), Range));
    }

    /// <summary>The pass runs twice a second; a defense that will not switch must not fill the log at that rate.</summary>
    [Fact]
    public void A_switch_the_game_refuses_is_reported_once_and_again_once_it_takes()
    {
        var game = Home(Unit(1));
        var control = new PowerControl();
        game.Refuses.Add(1);

        var first = control.Run(game, Everywhere(PowerMode.AlwaysOn), Range);
        var second = control.Run(game, Everywhere(PowerMode.AlwaysOn), Range);

        game.Refuses.Clear();

        var third = control.Run(game, Everywhere(PowerMode.AlwaysOn), Range);

        Assert.False(Assert.Single(first).IsDone);
        Assert.Empty(second);
        Assert.True(Assert.Single(third).IsDone);
        Assert.True(game.IsOn(1));
    }

    [Fact]
    public void Each_defense_runs_in_its_own_mode()
    {
        var game = Home(Unit(1), Unit(2, x: 30), Unit(3, configId: Lamp, x: 60, name: "Flood lamp"));
        var settings = PowerSettings.None.WithDefense(game.Defenses[0].Key, PowerMode.AlwaysOn).WithType(Lamp, PowerMode.AlwaysOn);

        new PowerControl().Run(game, new PowerPolicy(settings, PowerMode.WaveOnly), Range);

        Assert.True(game.IsOn(1));
        Assert.False(game.IsOn(2));
        Assert.True(game.IsOn(3));
    }

    [Fact]
    public void A_switch_says_which_mode_asked_for_it_and_why_for_the_log()
    {
        var game = Home(Unit(1));
        game.IsWaveRunning = true;

        var made = Assert.Single(new PowerControl().Run(game, Everywhere(PowerMode.WaveOnly), Range));

        Assert.True(made.IsOn);
        Assert.Equal("Wave only, a wave is running", made.Reason);
    }

    // ===== Cost of a pass =====

    /// <summary>
    /// The pass runs twice a second, and reading each zombie's position is a game call per zombie, so a mode that does
    /// not ask where the zombies are must never read them; the switch it makes is still the right one.
    /// </summary>
    [Theory]
    [InlineData(PowerMode.AlwaysOn, false, true)]
    [InlineData(PowerMode.GameDefault, false, false)]
    [InlineData(PowerMode.WaveOnly, true, true)]
    public void A_mode_that_does_not_ask_about_zombies_never_reads_them(PowerMode mode, bool isWaveRunning, bool isOnAfter)
    {
        var game = Home(Unit(1));
        game.IsWaveRunning = isWaveRunning;
        game.Zombies.Add(new Spot(5, 0, 0));

        new PowerControl().Run(game, Everywhere(mode), Range);

        Assert.Equal(0, game.ZombieReads);
        Assert.Equal(isOnAfter, game.IsOn(1));
    }

    /// <summary>The zombies are read once per pass, however many defenses ask about them.</summary>
    [Fact]
    public void Proximity_and_wave_only_between_waves_read_the_zombies_once_per_pass()
    {
        var game = Home(Unit(1), Unit(2, x: 30), Unit(3, configId: Lamp, x: 60));
        var settings = PowerSettings.None.WithType(Lamp, PowerMode.WaveOnly);
        game.Zombies.Add(new Spot(5, 0, 0));

        new PowerControl().Run(game, new PowerPolicy(settings, PowerMode.Proximity), Range);

        Assert.Equal(1, game.ZombieReads);
        Assert.True(game.IsOn(1));
        Assert.False(game.IsOn(2));
        Assert.True(game.IsOn(3));
    }
}
