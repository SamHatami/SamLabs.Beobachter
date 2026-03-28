namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed record class CsvParserOptions
{
    public char Delimiter { get; init; } = ',';

    public char Quote { get; init; } = '"';

    public string[] ColumnNames { get; init; } =
    [
        "sequence",
        "time",
        "level",
        "thread",
        "class",
        "method",
        "message",
        "exception",
        "file"
    ];

    public string[] TimestampFormats { get; init; } =
    [
        "yyyy/MM/dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fffK",
        "O"
    ];
}
