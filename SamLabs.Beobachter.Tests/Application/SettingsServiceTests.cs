using System.Threading;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Core.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class SettingsServiceTests
{
    [Fact]
    public void InitializeAsync_WhenCalledSynchronouslyWithBlockingContext_Completes()
    {
        var settingsStore = new AsyncSettingsStore();
        var themeService = new TestThemeService();
        var sut = new SettingsService(settingsStore, themeService, new LogLevelColorResourceService());

        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new BlockingSynchronizationContext());

            try
            {
                sut.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(2)));
        Assert.Null(thrown);
        Assert.Equal(AppThemeMode.Dark, themeService.CurrentMode);
    }

    [Fact]
    public void UpdateAppSettingsAsync_WhenCalledSynchronouslyWithBlockingContext_Completes()
    {
        var settingsStore = new AsyncSettingsStore();
        var themeService = new TestThemeService();
        var sut = new SettingsService(settingsStore, themeService, new LogLevelColorResourceService());
        var updated = new AppSettings { ThemeMode = nameof(AppThemeMode.Light) };

        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new BlockingSynchronizationContext());

            try
            {
                sut.UpdateAppSettingsAsync(updated).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(2)));
        Assert.Null(thrown);
        Assert.Equal(AppThemeMode.Light, themeService.CurrentMode);
        Assert.Same(updated, settingsStore.LastSavedAppSettings);
    }

    private sealed class BlockingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            // Intentionally does not execute callbacks to emulate a blocked UI context.
        }
    }

    private sealed class TestThemeService : IThemeService
    {
        public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.System;

        public void SetTheme(AppThemeMode mode)
        {
            CurrentMode = mode;
        }
    }

    private sealed class AsyncSettingsStore : ISettingsStore
    {
        public AppSettings LastSavedAppSettings { get; private set; } = new();

        public async ValueTask<AppSettings> LoadAppSettingsAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return new AppSettings { ThemeMode = nameof(AppThemeMode.Dark) };
        }

        public ValueTask<ReceiverDefinitions> LoadReceiverDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new ReceiverDefinitions());
        }

        public ValueTask<WorkspaceSettings> LoadWorkspaceSettingsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new WorkspaceSettings());
        }

        public ValueTask<UiLayoutSettings> LoadUiLayoutSettingsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new UiLayoutSettings());
        }

        public async ValueTask SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            LastSavedAppSettings = settings;
        }

        public ValueTask SaveReceiverDefinitionsAsync(ReceiverDefinitions settings, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveUiLayoutSettingsAsync(UiLayoutSettings settings, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
