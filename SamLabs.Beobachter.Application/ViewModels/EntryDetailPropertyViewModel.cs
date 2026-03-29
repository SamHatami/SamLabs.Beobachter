namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class EntryDetailPropertyViewModel
{
    public EntryDetailPropertyViewModel(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public string Value { get; }
}
