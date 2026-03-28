using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class LogStreamViewModelTests
{
    [Fact]
    public void AppendEntries_FiltersAndCapsVisibleCollection()
    {
        LogStreamViewModel vm = new();
        List<LogEntry> entries = [];
        for (var index = 0; index < 2_500; index++)
        {
            entries.Add(MainWindowTestSupport.CreateEntry(
                $"Orders.Api.{index}",
                LogLevel.Info,
                $"Entry {index}"));
        }

        vm.AppendEntries(entries, _ => true);

        Assert.Equal(2_000, vm.VisibleEntries.Count);
        Assert.Equal("Entry 500", vm.VisibleEntries[0].Message);
        Assert.Equal("Entry 2499", vm.VisibleEntries[^1].Message);
    }

    [Fact]
    public void RebuildEntries_ReplacesVisibleCollectionUsingFilter()
    {
        LogStreamViewModel vm = new();
        IReadOnlyList<LogEntry> snapshot =
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "ok"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Warn, "warn"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "err")
        ];

        vm.RebuildEntries(snapshot, entry => entry.Level >= LogLevel.Warn);

        Assert.Equal(2, vm.VisibleEntries.Count);
        Assert.Equal(LogLevel.Warn, vm.VisibleEntries[0].Level);
        Assert.Equal(LogLevel.Error, vm.VisibleEntries[1].Level);
    }

    [Fact]
    public void ToggleDensity_UpdatesRowMetrics()
    {
        LogStreamViewModel vm = new();
        Assert.False(vm.IsCompactDensity);
        Assert.Equal("Density: Comfortable", vm.DensityButtonText);
        Assert.Equal(12, vm.LogRowFontSize);

        vm.ToggleDensityCommand.Execute(null);
        Assert.True(vm.IsCompactDensity);
        Assert.Equal("Density: Compact", vm.DensityButtonText);
        Assert.Equal(11, vm.LogRowFontSize);

        vm.ToggleDensityCommand.Execute(null);
        Assert.False(vm.IsCompactDensity);
        Assert.Equal("Density: Comfortable", vm.DensityButtonText);
    }

    [Fact]
    public void ColumnWidthCommands_AdjustWithinBoundsAndReset()
    {
        LogStreamViewModel vm = new();
        vm.DecreaseColumnWidthsCommand.Execute(null);
        Assert.Equal(164, vm.TimestampColumnWidth);
        Assert.Equal(74, vm.LevelColumnWidth);
        Assert.Equal(204, vm.LoggerColumnWidth);

        vm.IncreaseColumnWidthsCommand.Execute(null);
        Assert.Equal(180, vm.TimestampColumnWidth);
        Assert.Equal(90, vm.LevelColumnWidth);
        Assert.Equal(220, vm.LoggerColumnWidth);

        for (var index = 0; index < 30; index++)
        {
            vm.DecreaseColumnWidthsCommand.Execute(null);
        }

        Assert.Equal(100, vm.TimestampColumnWidth);
        Assert.Equal(70, vm.LevelColumnWidth);
        Assert.Equal(120, vm.LoggerColumnWidth);

        vm.ResetColumnWidthsCommand.Execute(null);
        Assert.Equal(180, vm.TimestampColumnWidth);
        Assert.Equal(90, vm.LevelColumnWidth);
        Assert.Equal(220, vm.LoggerColumnWidth);
    }

    [Fact]
    public void SelectionCommands_MoveSelectedEntryWithinBounds()
    {
        LogStreamViewModel vm = new();
        IReadOnlyList<LogEntry> snapshot =
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "first"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Warn, "second"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "third")
        ];

        vm.RebuildEntries(snapshot, _ => true);

        vm.SelectNextEntryCommand.Execute(null);
        Assert.Equal("first", vm.SelectedEntry?.Message);

        vm.SelectNextEntryCommand.Execute(null);
        Assert.Equal("second", vm.SelectedEntry?.Message);

        vm.SelectPreviousEntryCommand.Execute(null);
        Assert.Equal("first", vm.SelectedEntry?.Message);

        vm.SelectPreviousEntryCommand.Execute(null);
        Assert.Equal("first", vm.SelectedEntry?.Message);
    }
}
