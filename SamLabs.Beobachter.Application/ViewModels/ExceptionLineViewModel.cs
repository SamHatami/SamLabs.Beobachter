using System;
using System.Collections.Generic;

namespace SamLabs.Beobachter.Application.ViewModels;

public enum ExceptionLineKind
{
    Header,
    Frame,
    Separator
}

public sealed class ExceptionLineViewModel
{
    public ExceptionLineViewModel(string text, ExceptionLineKind kind)
    {
        Text = text;
        Kind = kind;
    }

    public string Text { get; }

    public ExceptionLineKind Kind { get; }

    public static IEnumerable<ExceptionLineViewModel> Parse(string? exception)
    {
        if (string.IsNullOrWhiteSpace(exception))
        {
            yield break;
        }

        string[] lines = exception.Split(["\r\n", "\n"], StringSplitOptions.None);

        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();
            ExceptionLineKind kind;

            if (trimmed.StartsWith("at ", StringComparison.Ordinal))
            {
                kind = ExceptionLineKind.Frame;
            }
            else if (trimmed.StartsWith("--- ", StringComparison.Ordinal))
            {
                kind = ExceptionLineKind.Separator;
            }
            else
            {
                kind = ExceptionLineKind.Header;
            }

            yield return new ExceptionLineViewModel(line, kind);
        }
    }
}
