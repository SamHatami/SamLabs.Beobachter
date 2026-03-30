using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class ReceiverDefinitionViewModel : ObservableObject
{
    private const string Log4jXmlParserName = "Log4jXmlParser";
    private const string JsonLogParserName = "JsonLogParser";
    private const string CsvParserName = "CsvParser";
    private const string PlainTextParserName = "PlainTextParser";
    private static readonly string[] DefaultParserOrder = ["Log4jXmlParser", "JsonLogParser", "CsvParser", "PlainTextParser"];
    private bool _isSyncingParserSelection;

    public ReceiverDefinitionViewModel(string kind)
    {
        Kind = kind;
    }

    public string Kind { get; }

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [NotifyPropertyChangedFor(nameof(IsStatusRunning))]
    [NotifyPropertyChangedFor(nameof(IsStatusFaulted))]
    [NotifyPropertyChangedFor(nameof(IsStatusDisabled))]
    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _bindAddress = "0.0.0.0";

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private int _pollIntervalMs = 150;

    [ObservableProperty]
    private string _parserOrderText = string.Join(", ", DefaultParserOrder);

    [ObservableProperty]
    private bool _isLog4jXmlParserEnabled = true;

    [ObservableProperty]
    private bool _isJsonLogParserEnabled = true;

    [ObservableProperty]
    private bool _isCsvParserEnabled = true;

    [ObservableProperty]
    private bool _isPlainTextParserEnabled = true;

    [NotifyPropertyChangedFor(nameof(HasDisplayNameValidationError))]
    [ObservableProperty]
    private string _displayNameValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasIdValidationError))]
    [ObservableProperty]
    private string _idValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasBindAddressValidationError))]
    [ObservableProperty]
    private string _bindAddressValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasPortValidationError))]
    [ObservableProperty]
    private string _portValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasFilePathValidationError))]
    [ObservableProperty]
    private string _filePathValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasPollIntervalValidationError))]
    [ObservableProperty]
    private string _pollIntervalValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasParserOrderValidationError))]
    [ObservableProperty]
    private string _parserOrderValidationError = string.Empty;

    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(IsFaulted))]
    [NotifyPropertyChangedFor(nameof(IsStopped))]
    [NotifyPropertyChangedFor(nameof(IsStatusRunning))]
    [NotifyPropertyChangedFor(nameof(IsStatusFaulted))]
    [ObservableProperty]
    private ReceiverRunState _runState = ReceiverRunState.Stopped;

    public bool IsUdp => Kind == ReceiverKinds.Udp;

    public bool IsTcp => Kind == ReceiverKinds.Tcp;

    public bool IsFile => Kind == ReceiverKinds.File;

    public bool IsRunning => RunState == ReceiverRunState.Running;

    public bool IsFaulted => RunState == ReceiverRunState.Faulted;

    public bool IsStopped => RunState == ReceiverRunState.Stopped;

    public bool IsStatusRunning => Enabled && !IsFaulted;

    public bool IsStatusFaulted => Enabled && IsFaulted;

    public bool IsStatusDisabled => !Enabled;

    public bool HasDisplayNameValidationError => DisplayNameValidationError.Length > 0;

    public bool HasIdValidationError => IdValidationError.Length > 0;

    public bool HasBindAddressValidationError => BindAddressValidationError.Length > 0;

    public bool HasPortValidationError => PortValidationError.Length > 0;

    public bool HasFilePathValidationError => FilePathValidationError.Length > 0;

    public bool HasPollIntervalValidationError => PollIntervalValidationError.Length > 0;

    public bool HasParserOrderValidationError => ParserOrderValidationError.Length > 0;

    public void ClearValidationErrors()
    {
        DisplayNameValidationError = string.Empty;
        IdValidationError = string.Empty;
        BindAddressValidationError = string.Empty;
        PortValidationError = string.Empty;
        FilePathValidationError = string.Empty;
        PollIntervalValidationError = string.Empty;
        ParserOrderValidationError = string.Empty;
    }

    partial void OnParserOrderTextChanged(string value)
    {
        if (_isSyncingParserSelection)
        {
            return;
        }

        ApplyParserSelectionFromParserOrder(value);
    }

    partial void OnIsLog4jXmlParserEnabledChanged(bool value)
    {
        SyncParserOrderFromSelection();
    }

    partial void OnIsJsonLogParserEnabledChanged(bool value)
    {
        SyncParserOrderFromSelection();
    }

    partial void OnIsCsvParserEnabledChanged(bool value)
    {
        SyncParserOrderFromSelection();
    }

    partial void OnIsPlainTextParserEnabledChanged(bool value)
    {
        SyncParserOrderFromSelection();
    }

    private void ApplyParserSelectionFromParserOrder(string value)
    {
        HashSet<string> selected = ParseParserNames(value);

        _isSyncingParserSelection = true;
        try
        {
            IsLog4jXmlParserEnabled = selected.Contains(Log4jXmlParserName);
            IsJsonLogParserEnabled = selected.Contains(JsonLogParserName);
            IsCsvParserEnabled = selected.Contains(CsvParserName);
            IsPlainTextParserEnabled = selected.Contains(PlainTextParserName);
        }
        finally
        {
            _isSyncingParserSelection = false;
        }
    }

    private void SyncParserOrderFromSelection()
    {
        if (_isSyncingParserSelection)
        {
            return;
        }

        List<string> parserOrder = [];
        if (IsLog4jXmlParserEnabled)
        {
            parserOrder.Add(Log4jXmlParserName);
        }

        if (IsJsonLogParserEnabled)
        {
            parserOrder.Add(JsonLogParserName);
        }

        if (IsCsvParserEnabled)
        {
            parserOrder.Add(CsvParserName);
        }

        if (IsPlainTextParserEnabled)
        {
            parserOrder.Add(PlainTextParserName);
        }

        _isSyncingParserSelection = true;
        try
        {
            ParserOrderText = string.Join(", ", parserOrder);
        }
        finally
        {
            _isSyncingParserSelection = false;
        }
    }

    private static HashSet<string> ParseParserNames(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static parserName => parserName.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
