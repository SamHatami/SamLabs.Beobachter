using CommunityToolkit.Mvvm.ComponentModel;

namespace SamLabs.Beobachter.ViewModels;

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

    public bool IsUdp => Kind == ReceiverKinds.Udp;

    public bool IsTcp => Kind == ReceiverKinds.Tcp;

    public bool IsFile => Kind == ReceiverKinds.File;
}

public static class ReceiverKinds
{
    public const string Udp = "UDP";
    public const string Tcp = "TCP";
    public const string File = "FILE";
}
