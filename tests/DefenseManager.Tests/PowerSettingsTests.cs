using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class PowerSettingsTests
{
    private const int Coil = FakePowerGame.Coil;
    private const int Lamp = FakePowerGame.Lamp;

    private static readonly DefenseKey CoilAtGate = DefenseKey.Of(Coil, new Spot(10, 0, 4));
    private static readonly DefenseKey CoilAtWall = DefenseKey.Of(Coil, new Spot(-20, 0, 4));
    private static readonly DefenseKey LampAtGate = DefenseKey.Of(Lamp, new Spot(12, 0, 4));

    // ===== Which mode a defense runs in =====

    [Fact]
    public void A_defenses_own_setting_beats_its_type_and_its_type_beats_the_global_default()
    {
        var settings = PowerSettings.None.WithType(Coil, PowerMode.Proximity).WithDefense(CoilAtGate, PowerMode.AlwaysOn);
        var policy = new PowerPolicy(settings, PowerMode.WaveOnly);

        Assert.Equal(PowerMode.AlwaysOn, policy.ModeOf(CoilAtGate));
        Assert.Equal(PowerMode.Proximity, policy.ModeOf(CoilAtWall));
        Assert.Equal(PowerMode.WaveOnly, policy.ModeOf(LampAtGate));
    }

    [Fact]
    public void Clearing_a_defenses_own_setting_puts_it_back_on_its_type_setting()
    {
        var settings = PowerSettings.None.WithType(Coil, PowerMode.Proximity).WithDefense(CoilAtGate, PowerMode.AlwaysOn).WithDefense(CoilAtGate, null);

        Assert.Equal(PowerMode.Proximity, new PowerPolicy(settings, PowerMode.WaveOnly).ModeOf(CoilAtGate));
        Assert.Empty(settings.Defenses);
    }

    [Fact]
    public void Clearing_a_type_setting_puts_its_defenses_back_on_the_global_default()
    {
        var settings = PowerSettings.None.WithType(Coil, PowerMode.Proximity).WithType(Coil, null);

        Assert.Equal(PowerMode.WaveOnly, new PowerPolicy(settings, PowerMode.WaveOnly).ModeOf(CoilAtWall));
        Assert.Empty(settings.Types);
    }

    [Fact]
    public void A_defense_with_its_own_setting_keeps_it_when_its_type_setting_changes()
    {
        var settings = PowerSettings.None.WithDefense(CoilAtGate, PowerMode.GameDefault).WithType(Coil, PowerMode.AlwaysOn);

        Assert.Equal(PowerMode.GameDefault, new PowerPolicy(settings, PowerMode.WaveOnly).ModeOf(CoilAtGate));
    }

    // ===== What the buttons say =====

    [Fact]
    public void A_defense_with_no_setting_of_its_own_says_it_follows_its_type_and_what_that_gives()
    {
        var settings = PowerSettings.None.WithType(Coil, PowerMode.Proximity).WithDefense(CoilAtGate, PowerMode.AlwaysOn);
        var policy = new PowerPolicy(settings, PowerMode.WaveOnly);

        Assert.Equal("Always on", policy.DefenseLabel(CoilAtGate));
        Assert.Equal("Type setting (Proximity)", policy.DefenseLabel(CoilAtWall));
        Assert.Equal("Type setting (Wave only)", policy.DefenseLabel(LampAtGate));
    }

    [Fact]
    public void A_type_with_no_setting_says_it_follows_the_global_default_and_what_that_is()
    {
        var policy = new PowerPolicy(PowerSettings.None.WithType(Coil, PowerMode.GameDefault), PowerMode.Proximity);

        Assert.Equal("Default", policy.TypeLabel(Coil));
        Assert.Equal("Global setting (Proximity)", policy.TypeLabel(Lamp));
    }

    // ===== The cycle =====

    [Fact]
    public void A_cycle_runs_always_on_wave_only_proximity_default_then_lets_go()
    {
        var seen = new List<PowerMode?>();
        PowerMode? setting = null;

        for (var press = 0; press < 5; press++)
        {
            setting = PowerModes.Next(setting);
            seen.Add(setting);
        }

        Assert.Equal(new PowerMode?[] { PowerMode.AlwaysOn, PowerMode.WaveOnly, PowerMode.Proximity, PowerMode.GameDefault, null }, seen);
    }

    // ===== Names in the config and the file =====

    [Theory]
    [InlineData("WaveOnly", PowerMode.WaveOnly)]
    [InlineData("AlwaysOn", PowerMode.AlwaysOn)]
    [InlineData("Proximity", PowerMode.Proximity)]
    [InlineData("Default", PowerMode.GameDefault)]
    [InlineData(" alwayson ", PowerMode.AlwaysOn)]
    [InlineData("DEFAULT", PowerMode.GameDefault)]
    public void A_config_name_reads_as_its_mode_case_aside(string name, PowerMode mode)
    {
        Assert.Equal(mode, PowerModes.Parse(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Off")]
    [InlineData("Wave only")]
    public void Anything_else_is_no_mode(string? name)
    {
        Assert.Null(PowerModes.Parse(name));
    }

    /// <summary>BepInEx puts a config value it does not accept back to the first one listed, so that must be the default.</summary>
    [Fact]
    public void The_config_lists_every_mode_by_a_name_it_reads_back_with_wave_only_first()
    {
        Assert.Equal("WaveOnly", PowerModes.ConfigNames[0]);
        Assert.Equal(Enum.GetValues<PowerMode>().OrderBy(mode => mode), PowerModes.ConfigNames.Select(name => PowerModes.Parse(name) ?? throw new InvalidOperationException(name)).OrderBy(mode => mode));
    }

    // ===== Which defense a setting belongs to =====

    [Fact]
    public void A_defense_is_known_by_its_type_and_where_it_stands_to_a_tenth_of_a_metre()
    {
        Assert.Equal(DefenseKey.Of(Coil, new Spot(1.04, 0, 2)), DefenseKey.Of(Coil, new Spot(0.96, 0, 2)));
        Assert.NotEqual(DefenseKey.Of(Coil, new Spot(1.0, 0, 2)), DefenseKey.Of(Coil, new Spot(1.1, 0, 2)));
        Assert.NotEqual(DefenseKey.Of(Coil, new Spot(1.0, 0, 2)), DefenseKey.Of(Lamp, new Spot(1.0, 0, 2)));
    }

    [Fact]
    public void A_defense_key_reads_back_from_its_text()
    {
        var key = DefenseKey.Of(Coil, new Spot(-12.25, 0.5, 1e6));

        Assert.True(DefenseKey.TryParse(key.ToString(), out var read));
        Assert.Equal(key, read);
    }

    [Theory]
    [InlineData("")]
    [InlineData("36101:1:2")]
    [InlineData("36101:1:2:3:4")]
    [InlineData("coil:1:2:3")]
    [InlineData("36101:1.5:2:3")]
    public void Text_that_is_not_a_defense_key_does_not_read_as_one(string text)
    {
        Assert.False(DefenseKey.TryParse(text, out _));
    }
}
