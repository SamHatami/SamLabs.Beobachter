namespace SamLabs.Beobachter.Core.Models;

public sealed class LoggerNode
{
    private readonly Dictionary<string, LoggerNode> _children =
        new(StringComparer.Ordinal);

    private LoggerNode(string name, string fullPath, LoggerNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        Parent = parent;
    }

    public string Name { get; }

    public string FullPath { get; }

    public LoggerNode? Parent { get; }

    public bool IsEnabled { get; private set; } = true;

    public IReadOnlyDictionary<string, LoggerNode> Children => _children;

    public static LoggerNode CreateRoot(string name = "Root")
    {
        return new LoggerNode(name, string.Empty, parent: null);
    }

    public LoggerNode GetOrCreatePath(string loggerPath, char separator = '.')
    {
        if (string.IsNullOrWhiteSpace(loggerPath))
        {
            return this;
        }

        var current = this;
        foreach (var segment in SplitPath(loggerPath, separator))
        {
            if (!current._children.TryGetValue(segment, out var child))
            {
                var fullPath = string.IsNullOrEmpty(current.FullPath)
                    ? segment
                    : $"{current.FullPath}{separator}{segment}";

                child = new LoggerNode(segment, fullPath, current);
                current._children[segment] = child;
            }

            current = child;
        }

        return current;
    }

    public bool TryGetPath(string loggerPath, out LoggerNode? node, char separator = '.')
    {
        node = this;
        if (string.IsNullOrWhiteSpace(loggerPath))
        {
            return true;
        }

        foreach (var segment in SplitPath(loggerPath, separator))
        {
            if (node is null || !node._children.TryGetValue(segment, out node))
            {
                return false;
            }
        }

        return true;
    }

    public void SetEnabled(bool isEnabled, bool recursive)
    {
        IsEnabled = isEnabled;

        if (!recursive)
        {
            return;
        }

        foreach (var child in _children.Values)
        {
            child.SetEnabled(isEnabled, recursive: true);
        }
    }

    public IEnumerable<LoggerNode> EnumerateDepthFirst(bool includeSelf = true)
    {
        if (includeSelf)
        {
            yield return this;
        }

        foreach (var child in _children.Values.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            foreach (var descendant in child.EnumerateDepthFirst(includeSelf: true))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<string> SplitPath(string loggerPath, char separator)
    {
        return loggerPath
            .Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
