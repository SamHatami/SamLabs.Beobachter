using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class EntryDetailsViewModel : ViewModelBase
{
    public event Action<string, string>? FilterByPropertyRequested;

    private readonly IClipboardService _clipboardService;
    private string _rawPayloadText = string.Empty;
    private string _formattedPayloadText = string.Empty;

    [ObservableProperty]
    private LogEntry? _selectedEntry;

    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [ObservableProperty]
    private bool _hasSelectedEntry;

    [ObservableProperty]
    private string _selectedDetailsText = "No entry selected.";

    [ObservableProperty]
    private string _copyStatus = string.Empty;

    [ObservableProperty]
    private string _headerLevelText = string.Empty;

    [ObservableProperty]
    private LogLevel _headerLevel = LogLevel.Info;

    [ObservableProperty]
    private string _headerTimestampText = string.Empty;

    [ObservableProperty]
    private string _headerMessage = "No entry selected.";

    [ObservableProperty]
    private string _headerSourceText = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasHeaderContext))]
    [ObservableProperty]
    private string _headerContextText = string.Empty;

    [ObservableProperty]
    private string _payloadText = "No payload available.";

    [ObservableProperty]
    private bool _hasPayload;

    [ObservableProperty]
    private string _exceptionText = string.Empty;

    [ObservableProperty]
    private bool _hasException;

    [ObservableProperty]
    private bool _isExceptionExpanded;

    [NotifyPropertyChangedFor(nameof(HasNoAttributes))]
    [ObservableProperty]
    private bool _hasAttributes;

    [NotifyPropertyChangedFor(nameof(IsRawPayloadView))]
    [ObservableProperty]
    private bool _isJsonPayloadView = true;

    public ObservableCollection<EntryDetailPropertyViewModel> Attributes { get; } = [];

    public bool IsEmptyStateVisible => !HasSelectedEntry;

    public bool HasHeaderContext => !string.IsNullOrWhiteSpace(HeaderContextText);

    public bool HasNoAttributes => !HasAttributes;

    public bool IsRawPayloadView => !IsJsonPayloadView;

    public EntryDetailsViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
    }

    [RelayCommand]
    private async Task CopySelectedMessageAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        await _clipboardService.SetTextAsync(SelectedEntry.Message).ConfigureAwait(false);
        CopyStatus = "Message copied.";
    }

    [RelayCommand]
    private async Task CopySelectedDetailsAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        await _clipboardService.SetTextAsync(SelectedDetailsText).ConfigureAwait(false);
        CopyStatus = "Details copied.";
    }

    partial void OnSelectedEntryChanged(LogEntry? value)
    {
        SelectedDetailsText = BuildDetailsText(value);
        CopyStatus = string.Empty;
        ApplyProjection(value);
    }

    partial void OnIsJsonPayloadViewChanged(bool value)
    {
        UpdatePayloadText();
    }

    private static string BuildDetailsText(LogEntry? entry)
    {
        if (entry is null)
        {
            return "No entry selected.";
        }

        StringBuilder builder = new();
        builder.AppendLine($"Timestamp: {entry.Timestamp:O}");
        builder.AppendLine($"Level: {entry.Level}");
        builder.AppendLine($"Receiver: {entry.ReceiverId}");
        builder.AppendLine($"Logger: {entry.LoggerName}");
        builder.AppendLine($"Thread: {entry.ThreadName}");
        builder.AppendLine($"Message: {entry.Message}");
        if (!string.IsNullOrWhiteSpace(entry.MessageTemplate))
        {
            builder.AppendLine($"MessageTemplate: {entry.MessageTemplate}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Exception))
        {
            builder.AppendLine();
            builder.AppendLine("Exception:");
            builder.AppendLine(entry.Exception);
        }

        if (!string.IsNullOrWhiteSpace(entry.SourceFileName) || entry.SourceFileLineNumber.HasValue)
        {
            builder.AppendLine();
            builder.AppendLine($"Source: {entry.SourceFileName}:{entry.SourceFileLineNumber}");
        }

        if (entry.Properties.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Properties:");
            foreach (KeyValuePair<string, string> pair in entry.Properties.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"- {pair.Key}: {pair.Value}");
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.StructuredPayloadJson))
        {
            builder.AppendLine();
            builder.AppendLine("StructuredPayload:");
            builder.AppendLine(entry.StructuredPayloadJson);
        }

        return builder.ToString();
    }

    [RelayCommand]
    private void ShowJsonPayload()
    {
        if (!HasPayload)
        {
            return;
        }

        IsJsonPayloadView = true;
    }

    [RelayCommand]
    private void ShowRawPayload()
    {
        if (!HasPayload)
        {
            return;
        }

        IsJsonPayloadView = false;
    }

    private void ApplyProjection(LogEntry? entry)
    {
        Attributes.Clear();

        if (entry is null)
        {
            HasSelectedEntry = false;
            HeaderLevel = LogLevel.Info;
            HeaderLevelText = string.Empty;
            HeaderTimestampText = string.Empty;
            HeaderMessage = "No entry selected.";
            HeaderSourceText = string.Empty;
            HeaderContextText = string.Empty;
            ExceptionText = string.Empty;
            HasException = false;
            IsExceptionExpanded = false;
            HasAttributes = false;
            HasPayload = false;
            _rawPayloadText = string.Empty;
            _formattedPayloadText = string.Empty;
            PayloadText = "No payload available.";
            return;
        }

        HasSelectedEntry = true;
        HeaderLevel = entry.Level;
        HeaderLevelText = entry.Level.ToString().ToUpperInvariant();
        HeaderTimestampText = entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
        HeaderMessage = entry.Message;
        HeaderSourceText = entry.LoggerName;
        HeaderContextText = BuildContextText(entry);

        foreach (KeyValuePair<string, string> pair in entry.Properties.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            Attributes.Add(new EntryDetailPropertyViewModel(pair.Key, pair.Value, (k, v) => FilterByPropertyRequested?.Invoke(k, v)));
        }
        HasAttributes = Attributes.Count > 0;

        ExceptionText = entry.Exception ?? string.Empty;
        HasException = !string.IsNullOrWhiteSpace(ExceptionText);
        IsExceptionExpanded = HasException;

        _rawPayloadText = entry.StructuredPayloadJson ?? string.Empty;
        _formattedPayloadText = FormatPayloadJson(_rawPayloadText);
        HasPayload = !string.IsNullOrWhiteSpace(_rawPayloadText);
        IsJsonPayloadView = true;
        UpdatePayloadText();
    }

    private void UpdatePayloadText()
    {
        if (!HasPayload)
        {
            PayloadText = "No payload available.";
            return;
        }

        PayloadText = IsJsonPayloadView ? _formattedPayloadText : _rawPayloadText;
    }

    private static string BuildContextText(LogEntry entry)
    {
        List<string> parts = new(5);

        if (!string.IsNullOrWhiteSpace(entry.ReceiverId))
        {
            parts.Add($"Receiver: {entry.ReceiverId}");
        }

        if (!string.IsNullOrWhiteSpace(entry.ThreadName))
        {
            parts.Add($"Thread: {entry.ThreadName}");
        }

        if (!string.IsNullOrWhiteSpace(entry.HostName))
        {
            parts.Add($"Host: {entry.HostName}");
        }

        if (!string.IsNullOrWhiteSpace(entry.SourceFileName) || entry.SourceFileLineNumber.HasValue)
        {
            parts.Add($"Source: {entry.SourceFileName}:{entry.SourceFileLineNumber}");
        }

        return string.Join(" | ", parts);
    }

    private static string FormatPayloadJson(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (JsonException)
        {
            return payload;
        }
    }
}
