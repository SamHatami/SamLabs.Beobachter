using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class EntryDetailsViewModel : ViewModelBase
{
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private LogEntry? _selectedEntry;

    [ObservableProperty]
    private string _selectedDetailsText = "No entry selected.";

    [ObservableProperty]
    private string _copyStatus = string.Empty;

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
}
