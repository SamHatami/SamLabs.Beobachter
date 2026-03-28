using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Core;

public sealed class LoggerNodeTests
{
    [Fact]
    public void GetOrCreatePath_BuildsTrieAndFullPath()
    {
        var root = LoggerNode.CreateRoot();
        var node = root.GetOrCreatePath("App.Services.Auth");

        Assert.Equal("Auth", node.Name);
        Assert.Equal("App.Services.Auth", node.FullPath);
        Assert.NotNull(node.Parent);
        Assert.Equal("Services", node.Parent!.Name);
    }

    [Fact]
    public void TryGetPath_ReturnsFalseWhenNodeMissing()
    {
        var root = LoggerNode.CreateRoot();
        root.GetOrCreatePath("App.Services.Auth");

        var found = root.TryGetPath("App.Services.Missing", out var node);

        Assert.False(found);
        Assert.Null(node);
    }

    [Fact]
    public void SetEnabled_WithRecursiveTrue_PropagatesToDescendants()
    {
        var root = LoggerNode.CreateRoot();
        var branch = root.GetOrCreatePath("App.Services.Auth");
        branch.GetOrCreatePath("Token");

        branch.SetEnabled(isEnabled: false, recursive: true);

        Assert.False(branch.IsEnabled);
        Assert.All(branch.EnumerateDepthFirst(includeSelf: false), child => Assert.False(child.IsEnabled));
    }

    [Fact]
    public void SetEnabled_WithRecursiveFalse_OnlyAffectsCurrentNode()
    {
        var root = LoggerNode.CreateRoot();
        var branch = root.GetOrCreatePath("App.Services.Auth");
        var child = branch.GetOrCreatePath("Token");

        branch.SetEnabled(isEnabled: false, recursive: false);

        Assert.False(branch.IsEnabled);
        Assert.True(child.IsEnabled);
    }

    [Fact]
    public void EnumerateDepthFirst_IncludesAllNodes()
    {
        var root = LoggerNode.CreateRoot();
        root.GetOrCreatePath("A.B");
        root.GetOrCreatePath("A.C");

        var names = root
            .EnumerateDepthFirst(includeSelf: false)
            .Select(x => x.FullPath)
            .ToArray();

        Assert.Contains("A", names);
        Assert.Contains("A.B", names);
        Assert.Contains("A.C", names);
    }
}
