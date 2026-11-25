using Rimhunter.AutoCutBlight;
using Xunit;

namespace AutoCutBlight.Tests;

public class HandledBlightsTests
{
    [Fact]
    public void A_new_blight_is_recorded_once() {
        var handled = new HandledBlights();
        Assert.True(handled.TryRecord(7));
        Assert.False(handled.TryRecord(7));
        Assert.True(handled.TryRecord(8));
        Assert.Equal(new[] { 7, 8 }, handled.Ids.OrderBy(i => i));
    }

    [Fact]
    public void Trim_drops_the_blights_that_no_longer_exist_and_keeps_the_rest() {
        var handled = new HandledBlights();
        foreach (var id in new[] { 3, 5, 9, 12 }) handled.TryRecord(id);
        var removed = handled.Trim(new[] { 5, 12, 40 });
        Assert.Equal(2, removed);
        Assert.Equal(new[] { 5, 12 }, handled.Ids.OrderBy(i => i));
    }

    [Fact]
    public void Trim_never_adds_a_live_blight_it_has_not_handled() {
        var handled = new HandledBlights();
        handled.TryRecord(1);
        handled.Trim(new[] { 1, 2 });
        Assert.True(handled.TryRecord(2));
    }

    [Fact]
    public void A_trimmed_blight_id_can_be_recorded_again() {
        var handled = new HandledBlights();
        handled.TryRecord(4);
        handled.Trim(new int[0]);
        Assert.Empty(handled.Ids);
        Assert.True(handled.TryRecord(4));
    }

    [Fact]
    public void Replace_restores_a_saved_list() {
        var handled = new HandledBlights();
        handled.Replace(new[] { 10, 11, 10 });
        Assert.Equal(new[] { 10, 11 }, handled.Ids.OrderBy(i => i));
        Assert.False(handled.TryRecord(11));
    }
}
