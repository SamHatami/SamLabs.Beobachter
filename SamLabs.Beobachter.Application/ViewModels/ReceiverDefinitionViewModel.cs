using CommunityToolkit.Mvvm.ComponentModel;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class ReceiverDefinitionViewModel : ObservableObject
{
    private static readonly string[] DefaultParserOrder = ["Log4jXmlParser", "JsonLogParser", "CsvParser", "PlainTextParser"];

    public ReceiverDefinitionViewModel(string kind)
    {
        Kind = kind;
    }

    public string Kind { get; }

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

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
    [ObservableProperty]
    private ReceiverRunState _runState = ReceiverRunState.Stopped;

    public bool IsUdp => Kind == ReceiverKinds.Udp;

    public bool IsTcp => Kind == ReceiverKinds.Tcp;

    public bool IsFile => Kind == ReceiverKinds.File;

    public bool IsRunning => RunState == ReceiverRunState.Running;

    public bool IsFaulted => RunState == ReceiverRunState.Faulted;

    public bool IsStopped => RunState == ReceiverRunState.Stopped;

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
}
