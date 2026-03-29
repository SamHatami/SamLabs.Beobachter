using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class ReceiverSetupViewModel : ViewModelBase
{
    private const string BindAddressValidationMessage = "Bind address must be a literal IPv4 or IPv6 address (for example 0.0.0.0 or ::1).";
    private static readonly StringComparer ParserNameComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] DefaultParserOrder =
    [
        "Log4jXmlParser",
        "JsonLogParser",
        "CsvParser",
        "PlainTextParser"
    ];
    private static readonly HashSet<string> KnownParserNames = new(ParserNameComparer)
    {
        "Log4jXmlParser",
        "JsonLogParser",
        "CsvParser",
        "PlainTextParser"
    };
    private static readonly HashSet<string> ReceiverEditablePropertyNames = new(StringComparer.Ordinal)
    {
        nameof(ReceiverDefinitionViewModel.Id),
        nameof(ReceiverDefinitionViewModel.DisplayName),
        nameof(ReceiverDefinitionViewModel.Enabled),
        nameof(ReceiverDefinitionViewModel.BindAddress),
        nameof(ReceiverDefinitionViewModel.Port),
        nameof(ReceiverDefinitionViewModel.FilePath),
        nameof(ReceiverDefinitionViewModel.PollIntervalMs),
        nameof(ReceiverDefinitionViewModel.ParserOrderText)
    };

    private readonly ISettingsStore _settingsStore;
    private readonly IIngestionSession _ingestionSession;

    [ObservableProperty]
    private ReceiverDefinitionViewModel? _selectedReceiverDefinition;

    [ObservableProperty]
    private string _receiverSetupStatus = string.Empty;

    public ReceiverSetupViewModel(
        ISettingsStore settingsStore,
        IIngestionSession ingestionSession)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
    }

    public ObservableCollection<ReceiverDefinitionViewModel> ReceiverDefinitions { get; } = [];

    public async Task LoadAsync()
    {
        await LoadReceiverDefinitionsAsync().ConfigureAwait(false);
    }

    public void TrySelectReceiverById(string? receiverId)
    {
        if (string.IsNullOrWhiteSpace(receiverId) || ReceiverDefinitions.Count == 0)
        {
            return;
        }

        SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault(x =>
            x.Id.Equals(receiverId, StringComparison.OrdinalIgnoreCase))
            ?? SelectedReceiverDefinition;
    }

    [RelayCommand]
    private void AddUdpReceiver()
    {
        ReceiverDefinitionViewModel vm = new(ReceiverKinds.Udp)
        {
            Id = BuildUniqueReceiverId("udp"),
            DisplayName = $"UDP {ReceiverDefinitions.Count(x => x.IsUdp) + 1}",
            BindAddress = "0.0.0.0",
            Port = 7071
        };
        AttachReceiverDefinition(vm);
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private void AddTcpReceiver()
    {
        ReceiverDefinitionViewModel vm = new(ReceiverKinds.Tcp)
        {
            Id = BuildUniqueReceiverId("tcp"),
            DisplayName = $"TCP {ReceiverDefinitions.Count(x => x.IsTcp) + 1}",
            BindAddress = "0.0.0.0",
            Port = 4505
        };
        AttachReceiverDefinition(vm);
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private void AddFileReceiver()
    {
        ReceiverDefinitionViewModel vm = new(ReceiverKinds.File)
        {
            Id = BuildUniqueReceiverId("file"),
            DisplayName = $"File {ReceiverDefinitions.Count(x => x.IsFile) + 1}",
            FilePath = string.Empty,
            PollIntervalMs = 150
        };
        AttachReceiverDefinition(vm);
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private void RemoveSelectedReceiver()
    {
        if (SelectedReceiverDefinition is null)
        {
            return;
        }

        ReceiverDefinitionViewModel toRemove = SelectedReceiverDefinition;
        DetachReceiverDefinition(toRemove);
        ReceiverDefinitions.Remove(toRemove);
        SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault();
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private async Task SaveReceiverSetupAsync()
    {
        if (!TryValidateReceiverDefinitions(out string validationError))
        {
            ReceiverSetupStatus = $"Validation failed: {validationError}";
            return;
        }

        ReceiverDefinitions mapped = MapToReceiverDefinitions();
        await _settingsStore.SaveReceiverDefinitionsAsync(mapped);
        ReceiverReloadResult reloadResult = await _ingestionSession.ReloadReceiversAsync();
        ReceiverSetupStatus = BuildReloadStatusMessage(
            $"Saved {ReceiverDefinitions.Count} receiver(s).",
            reloadResult);
    }

    [RelayCommand]
    private async Task ReloadReceiverSetupAsync()
    {
        await LoadReceiverDefinitionsAsync();
        ReceiverReloadResult reloadResult = await _ingestionSession.ReloadReceiversAsync();
        ReceiverSetupStatus = BuildReloadStatusMessage(
            $"Reloaded {ReceiverDefinitions.Count} receiver(s) from settings.",
            reloadResult);
    }

    private void OnReceiverDefinitionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || ReceiverEditablePropertyNames.Contains(e.PropertyName))
        {
            TryValidateReceiverDefinitions(out _);
        }
    }

    private async Task LoadReceiverDefinitionsAsync()
    {
        ReceiverDefinitions definitions = await _settingsStore.LoadReceiverDefinitionsAsync().ConfigureAwait(false);

        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            ApplyReceiverDefinitions(definitions);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ApplyReceiverDefinitions(definitions));
    }

    private void ApplyReceiverDefinitions(ReceiverDefinitions definitions)
    {
        foreach (ReceiverDefinitionViewModel existing in ReceiverDefinitions)
        {
            DetachReceiverDefinition(existing);
        }

        ReceiverDefinitions.Clear();
        foreach (UdpReceiverDefinition udp in definitions.UdpReceivers)
        {
            ReceiverDefinitionViewModel receiver = new(ReceiverKinds.Udp)
            {
                Id = udp.Id,
                DisplayName = udp.DisplayName,
                Enabled = udp.Enabled,
                BindAddress = udp.BindAddress,
                Port = udp.Port,
                ParserOrderText = FormatParserOrder(udp.ParserOrder)
            };
            AttachReceiverDefinition(receiver);
            ReceiverDefinitions.Add(receiver);
        }

        foreach (TcpReceiverDefinition tcp in definitions.TcpReceivers)
        {
            ReceiverDefinitionViewModel receiver = new(ReceiverKinds.Tcp)
            {
                Id = tcp.Id,
                DisplayName = tcp.DisplayName,
                Enabled = tcp.Enabled,
                BindAddress = tcp.BindAddress,
                Port = tcp.Port,
                ParserOrderText = FormatParserOrder(tcp.ParserOrder)
            };
            AttachReceiverDefinition(receiver);
            ReceiverDefinitions.Add(receiver);
        }

        foreach (FileTailReceiverDefinition file in definitions.FileTailReceivers)
        {
            ReceiverDefinitionViewModel receiver = new(ReceiverKinds.File)
            {
                Id = file.Id,
                DisplayName = file.DisplayName,
                Enabled = file.Enabled,
                FilePath = file.FilePath,
                PollIntervalMs = file.PollIntervalMs,
                ParserOrderText = FormatParserOrder(file.ParserOrder)
            };
            AttachReceiverDefinition(receiver);
            ReceiverDefinitions.Add(receiver);
        }

        SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault();
        TryValidateReceiverDefinitions(out _);
    }

    private ReceiverDefinitions MapToReceiverDefinitions()
    {
        UdpReceiverDefinition[] udp = ReceiverDefinitions
            .Where(static x => x.IsUdp)
            .Select(x => new UdpReceiverDefinition
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Enabled = x.Enabled,
                BindAddress = string.IsNullOrWhiteSpace(x.BindAddress) ? "0.0.0.0" : x.BindAddress.Trim(),
                Port = x.Port <= 0 ? 7071 : x.Port,
                DefaultLoggerName = x.DisplayName,
                ParserOrder = ParseParserOrder(x.ParserOrderText)
            })
            .ToArray();

        TcpReceiverDefinition[] tcp = ReceiverDefinitions
            .Where(static x => x.IsTcp)
            .Select(x => new TcpReceiverDefinition
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Enabled = x.Enabled,
                BindAddress = string.IsNullOrWhiteSpace(x.BindAddress) ? "0.0.0.0" : x.BindAddress.Trim(),
                Port = x.Port <= 0 ? 4505 : x.Port,
                DefaultLoggerName = x.DisplayName,
                ParserOrder = ParseParserOrder(x.ParserOrderText)
            })
            .ToArray();

        FileTailReceiverDefinition[] file = ReceiverDefinitions
            .Where(static x => x.IsFile)
            .Select(x => new FileTailReceiverDefinition
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Enabled = x.Enabled,
                FilePath = x.FilePath.Trim(),
                PollIntervalMs = x.PollIntervalMs <= 0 ? 150 : x.PollIntervalMs,
                DefaultLoggerName = x.DisplayName,
                ParserOrder = ParseParserOrder(x.ParserOrderText)
            })
            .ToArray();

        return new ReceiverDefinitions
        {
            UdpReceivers = udp,
            TcpReceivers = tcp,
            FileTailReceivers = file
        };
    }

    private bool TryValidateReceiverDefinitions(out string error)
    {
        error = string.Empty;
        var isValid = true;
        Dictionary<string, List<ReceiverDefinitionViewModel>> ids = new(StringComparer.OrdinalIgnoreCase);
        string? firstError = null;

        foreach (ReceiverDefinitionViewModel receiver in ReceiverDefinitions)
        {
            receiver.ClearValidationErrors();
        }

        void RegisterError(string message)
        {
            isValid = false;
            if (firstError is null)
            {
                firstError = message;
            }
        }

        for (var index = 0; index < ReceiverDefinitions.Count; index++)
        {
            ReceiverDefinitionViewModel receiver = ReceiverDefinitions[index];
            var label = $"{receiver.Kind} #{index + 1}";

            if (string.IsNullOrWhiteSpace(receiver.Id))
            {
                receiver.IdValidationError = "Id is required.";
                RegisterError($"{label} requires a non-empty Id.");
            }
            else
            {
                var normalizedId = receiver.Id.Trim();
                if (!ids.TryGetValue(normalizedId, out List<ReceiverDefinitionViewModel>? matching))
                {
                    matching = [];
                    ids[normalizedId] = matching;
                }

                matching.Add(receiver);
            }

            if (string.IsNullOrWhiteSpace(receiver.DisplayName))
            {
                receiver.DisplayNameValidationError = "Display name is required.";
                RegisterError($"{label} requires a display name.");
            }

            if (!IsValidPort(receiver.Port))
            {
                receiver.PortValidationError = "Port must be between 1 and 65535.";
                RegisterError($"{label} port must be between 1 and 65535.");
            }

            if (receiver.Kind == ReceiverKinds.Udp || receiver.Kind == ReceiverKinds.Tcp)
            {
                if (!IsValidBindAddress(receiver.BindAddress))
                {
                    receiver.BindAddressValidationError = BindAddressValidationMessage;
                    RegisterError($"{label}: {BindAddressValidationMessage}");
                }
            }

            if (receiver.Kind == ReceiverKinds.File)
            {
                if (string.IsNullOrWhiteSpace(receiver.FilePath))
                {
                    receiver.FilePathValidationError = "File path is required.";
                    RegisterError($"{label} file path is required.");
                }

                if (receiver.PollIntervalMs <= 0)
                {
                    receiver.PollIntervalValidationError = "Poll interval must be greater than zero.";
                    RegisterError($"{label} poll interval must be greater than zero.");
                }
            }

            IReadOnlyList<string> parserOrder = ParseParserOrder(receiver.ParserOrderText);
            if (parserOrder.Count == 0)
            {
                receiver.ParserOrderValidationError = "Parser order cannot be empty.";
                RegisterError($"{label} parser order cannot be empty.");
            }
            else
            {
                string? unknown = parserOrder.FirstOrDefault(p => !KnownParserNames.Contains(p));
                if (!string.IsNullOrEmpty(unknown))
                {
                    receiver.ParserOrderValidationError = $"Unknown parser '{unknown}'.";
                    RegisterError($"{label} uses unknown parser '{unknown}'");
                }
            }
        }

        foreach (KeyValuePair<string, List<ReceiverDefinitionViewModel>> entry in ids)
        {
            if (entry.Value.Count > 1)
            {
                foreach (ReceiverDefinitionViewModel receiver in entry.Value)
                {
                    receiver.IdValidationError = "Id must be unique.";
                }

                RegisterError($"Receiver Id '{entry.Key}' is duplicated.");
            }
        }

        error = firstError ?? string.Empty;
        return isValid;
    }

    private static bool IsValidPort(int value)
    {
        return value is >= 1 and <= 65535;
    }

    private static bool IsValidBindAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return IPAddress.TryParse(value.Trim(), out _);
    }

    private static string BuildReloadStatusMessage(
        string operationPrefix,
        ReceiverReloadResult reloadResult)
    {
        if (reloadResult.FailedCount == 0)
        {
            return $"{operationPrefix} Started {reloadResult.SuccessfulCount} receiver(s).";
        }

        ReceiverStartupResult firstFailure = reloadResult.ReceiverStartupResults.First(static x => !x.Succeeded);
        return $"{operationPrefix} Started {reloadResult.SuccessfulCount} receiver(s), {reloadResult.FailedCount} failed. " +
               $"First failure: {firstFailure.DisplayName} ({firstFailure.ReceiverId}) - {firstFailure.ErrorMessage}";
    }

    private static string FormatParserOrder(IReadOnlyList<string> parserOrder)
    {
        return parserOrder.Count == 0
            ? string.Join(", ", DefaultParserOrder)
            : string.Join(", ", parserOrder);
    }

    private static IReadOnlyList<string> ParseParserOrder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultParserOrder;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static name => name.Length > 0)
            .Distinct(ParserNameComparer)
            .ToArray();
    }

    private void AttachReceiverDefinition(ReceiverDefinitionViewModel receiver)
    {
        receiver.PropertyChanged += OnReceiverDefinitionPropertyChanged;
    }

    private void DetachReceiverDefinition(ReceiverDefinitionViewModel receiver)
    {
        receiver.PropertyChanged -= OnReceiverDefinitionPropertyChanged;
    }

    private string BuildUniqueReceiverId(string prefix)
    {
        var next = 1;
        HashSet<string> existing = new(
            ReceiverDefinitions.Select<ReceiverDefinitionViewModel, string>(x => x.Id),
            StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var candidate = $"{prefix}-{next}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }

            next++;
        }
    }
}
