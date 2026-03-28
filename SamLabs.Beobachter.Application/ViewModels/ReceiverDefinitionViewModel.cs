using CommunityToolkit.Mvvm.ComponentModel;

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

    [ObservableProperty]
    private string _displayNameValidationError = string.Empty;

    [ObservableProperty]
    private string _idValidationError = string.Empty;

    [ObservableProperty]
    private string _bindAddressValidationError = string.Empty;

    [ObservableProperty]
    private string _portValidationError = string.Empty;

    [ObservableProperty]
    private string _filePathValidationError = string.Empty;

    [ObservableProperty]
    private string _pollIntervalValidationError = string.Empty;

    [ObservableProperty]
    private string _parserOrderValidationError = string.Empty;

    public bool IsUdp => Kind == ReceiverKinds.Udp;

    public bool IsTcp => Kind == ReceiverKinds.Tcp;

    public bool IsFile => Kind == ReceiverKinds.File;

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

    partial void OnDisplayNameValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasDisplayNameValidationError));
    }

    partial void OnIdValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasIdValidationError));
    }

    partial void OnBindAddressValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasBindAddressValidationError));
    }

    partial void OnPortValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasPortValidationError));
    }

    partial void OnFilePathValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasFilePathValidationError));
    }

    partial void OnPollIntervalValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasPollIntervalValidationError));
    }

    partial void OnParserOrderValidationErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasParserOrderValidationError));
    }
}

public static class ReceiverKinds
{
    public const string Udp = "UDP";
    public const string Tcp = "TCP";
    public const string File = "FILE";
}
