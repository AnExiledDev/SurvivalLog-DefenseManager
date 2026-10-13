using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class PowerSlotTests
{
    private const int Coil = FakePowerGame.Coil;

    private static readonly DefenseKey CoilAtGate = DefenseKey.Of(Coil, new Spot(10, 0, 4));

    private static PowerSlot Loaded(FakePowerFile file, string save)
    {
        var slot = new PowerSlot(file);

        slot.Follow(save);
        slot.Load();

        return slot;
    }

    [Fact]
    public void Defense_and_type_settings_are_read_back_when_the_save_is_loaded_again()
    {
        var file = new FakePowerFile();
        var slot = Loaded(file, "Save_01");

        Assert.Equal(SettingsEdit.Saved, slot.SetDefense(CoilAtGate, PowerMode.AlwaysOn));
        Assert.Equal(SettingsEdit.Saved, slot.SetType(Coil, PowerMode.Proximity));

        var later = new PowerSlot(file);
        later.Follow("Save_01");

        Assert.Equal(SettingsLoad.Read, later.Load());
        Assert.Equal(PowerMode.AlwaysOn, later.Settings.DefenseSetting(CoilAtGate));
        Assert.Equal(PowerMode.Proximity, later.Settings.TypeSetting(Coil));
    }

    [Fact]
    public void A_cleared_setting_stays_cleared_after_a_reload()
    {
        var file = new FakePowerFile();
        var slot = Loaded(file, "Save_01");

        slot.SetType(Coil, PowerMode.Proximity);
        slot.SetType(Coil, null);

        Assert.Null(Loaded(file, "Save_01").Settings.TypeSetting(Coil));
    }

    [Fact]
    public void Each_save_keeps_its_own_settings()
    {
        var file = new FakePowerFile();
        var slot = Loaded(file, "Save_01");

        slot.SetType(Coil, PowerMode.AlwaysOn);
        slot.Follow("Save_02");

        Assert.Equal(SettingsLoad.Read, slot.Load());
        Assert.Null(slot.Settings.TypeSetting(Coil));

        slot.SetType(Coil, PowerMode.GameDefault);

        Assert.Equal(PowerMode.AlwaysOn, Loaded(file, "Save_01").Settings.TypeSetting(Coil));
        Assert.Equal(PowerMode.GameDefault, Loaded(file, "Save_02").Settings.TypeSetting(Coil));
    }

    [Fact]
    public void Writing_one_saves_settings_keeps_every_other_line_of_the_file()
    {
        var file = new FakePowerFile { Text = "# kept by hand\nSave_02\ttype\t36102\tAlwaysOn\n" };

        Loaded(file, "Save_01").SetType(Coil, PowerMode.Proximity);

        Assert.Contains("# kept by hand\n", file.Text);
        Assert.Contains("Save_02\ttype\t36102\tAlwaysOn\n", file.Text);
    }

    [Fact]
    public void A_save_name_with_a_tab_or_a_line_break_stays_on_its_own_lines()
    {
        var file = new FakePowerFile();
        const string save = "Home\tbase\nTwo";

        Loaded(file, save).SetType(Coil, PowerMode.AlwaysOn);

        Assert.Equal(PowerMode.AlwaysOn, Loaded(file, save).Settings.TypeSetting(Coil));
        Assert.Null(Loaded(file, "Home").Settings.TypeSetting(Coil));
    }

    [Fact]
    public void A_line_with_a_mode_the_mod_does_not_know_is_skipped_and_the_rest_still_read()
    {
        var file = new FakePowerFile { Text = "Save_01\ttype\t36101\tTurbo\nSave_01\tdefense\t" + CoilAtGate + "\tProximity\n" };

        var slot = Loaded(file, "Save_01");

        Assert.Null(slot.Settings.TypeSetting(Coil));
        Assert.Equal(PowerMode.Proximity, slot.Settings.DefenseSetting(CoilAtGate));
    }

    /// <summary>A mode from a newer version, or a hand edit, must survive this version writing the file.</summary>
    [Fact]
    public void Lines_the_mod_cannot_read_are_kept_when_it_writes_the_file_and_the_ones_it_read_are_replaced()
    {
        const string turboType = "Save_01\ttype\t36101\tTurbo\n";
        const string unknownKind = "Save_01\tzone\t7\tAlwaysOn\n";
        const string badKey = "Save_01\tdefense\tnot-a-key\tAlwaysOn\n";
        var otherSaveTurbo = "Save_02\tdefense\t" + CoilAtGate + "\tTurbo\n";
        var file = new FakePowerFile { Text = turboType + unknownKind + badKey + otherSaveTurbo + "Save_01\tdefense\t" + CoilAtGate + "\tProximity\n" };

        Assert.Equal(SettingsEdit.Saved, Loaded(file, "Save_01").SetDefense(CoilAtGate, PowerMode.AlwaysOn));

        Assert.Contains(turboType, file.Text);
        Assert.Contains(unknownKind, file.Text);
        Assert.Contains(badKey, file.Text);
        Assert.Contains(otherSaveTurbo, file.Text);
        Assert.DoesNotContain("\tProximity\n", file.Text);
        Assert.Equal(PowerMode.AlwaysOn, Loaded(file, "Save_01").Settings.DefenseSetting(CoilAtGate));
    }

    /// <summary>The file holds every save's settings, so writing over one that could not be read would wipe them all.</summary>
    [Fact]
    public void A_file_that_cannot_be_read_is_never_written_over()
    {
        var file = new FakePowerFile { Text = "Save_02\ttype\t36102\tAlwaysOn\n", IsUnreadable = true };
        var slot = new PowerSlot(file);
        slot.Follow("Save_01");

        Assert.Equal(SettingsLoad.Unreadable, slot.Load());
        Assert.Equal(SettingsEdit.Unreadable, slot.SetDefense(CoilAtGate, PowerMode.AlwaysOn));
        Assert.Equal(SettingsEdit.Unreadable, slot.SetType(Coil, PowerMode.AlwaysOn));
        Assert.Equal("Save_02\ttype\t36102\tAlwaysOn\n", file.Text);
        Assert.Null(slot.Settings.TypeSetting(Coil));
    }

    [Fact]
    public void A_file_that_turns_unreadable_after_loading_is_not_written_over_and_the_setting_stays_as_it_was()
    {
        var file = new FakePowerFile();
        var slot = Loaded(file, "Save_01");

        slot.SetType(Coil, PowerMode.Proximity);

        var before = file.Text;
        file.IsUnreadable = true;

        Assert.Equal(SettingsEdit.Unreadable, slot.SetType(Coil, PowerMode.AlwaysOn));
        Assert.Equal(before, file.Text);
        Assert.Equal(PowerMode.Proximity, slot.Settings.TypeSetting(Coil));
    }

    [Fact]
    public void A_file_that_can_be_read_again_is_read_on_the_next_load()
    {
        var file = new FakePowerFile { Text = "Save_01\ttype\t36101\tAlwaysOn\n", IsUnreadable = true };
        var slot = new PowerSlot(file);
        slot.Follow("Save_01");
        slot.Load();
        file.IsUnreadable = false;

        Assert.Equal(SettingsLoad.Read, slot.Load());
        Assert.Equal(PowerMode.AlwaysOn, slot.Settings.TypeSetting(Coil));
    }

    [Fact]
    public void With_no_save_loaded_nothing_is_read_or_written()
    {
        var file = new FakePowerFile();
        var slot = new PowerSlot(file);

        Assert.Equal(SettingsLoad.NoSave, slot.Load());
        Assert.Equal(SettingsEdit.NoSave, slot.SetType(Coil, PowerMode.AlwaysOn));
        Assert.Equal("", file.Text);
    }

    [Fact]
    public void A_setting_that_could_not_be_written_still_holds_for_the_session()
    {
        var file = new FakePowerFile { FailsToWrite = true };
        var slot = Loaded(file, "Save_01");

        Assert.Equal(SettingsEdit.NotSaved, slot.SetDefense(CoilAtGate, PowerMode.AlwaysOn));
        Assert.Equal(PowerMode.AlwaysOn, slot.Settings.DefenseSetting(CoilAtGate));
        Assert.Equal("", file.Text);
    }

    /// <summary>The save name can read as missing for a moment; only a real move to another save resets the pass.</summary>
    [Fact]
    public void Only_a_move_to_another_save_counts_as_a_new_save()
    {
        var slot = new PowerSlot(new FakePowerFile());

        Assert.False(slot.Follow("Save_01"));
        Assert.False(slot.Follow(null));
        Assert.False(slot.Follow("Save_01"));
        Assert.True(slot.Follow("Save_02"));
    }
}
