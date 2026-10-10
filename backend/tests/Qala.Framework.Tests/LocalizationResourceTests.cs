using System.Text.Json;

namespace Qala.Framework.Tests;

/// <summary>Every user-facing key must exist in both ar.json and en.json, and every key the code uses must exist.</summary>
public class LocalizationResourceTests
{
    public static TheoryData<string> ResourceFolders() => new(Folders());

    /// <summary>Every <c>Resources</c> folder (relative to backend/) that has an <c>en.json</c>.</summary>
    public static List<string> Folders() =>
        Directory.EnumerateFiles(BackendRoot(), "en.json", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(Path.GetDirectoryName(f)) == "Resources"
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(BackendRoot(), Path.GetDirectoryName(f)!))
            .Order(StringComparer.Ordinal)
            .ToList();

    [Theory]
    [MemberData(nameof(ResourceFolders))]
    public void ArabicAndEnglish_ShouldHaveTheSameKeys_WhenBothFilesExist(string folder)
    {
        var en = Keys(Path.Combine(BackendRoot(), folder, "en.json"));
        var ar = Keys(Path.Combine(BackendRoot(), folder, "ar.json"));

        Assert.Empty(en.Except(ar));
        Assert.Empty(ar.Except(en));
    }

    [Fact]
    public void ResourceFolders_ShouldIncludeFrameworkAndModules_WhenScanned()
    {
        var folders = Folders();

        Assert.Contains(folders, f => f.Contains("Qala.Framework.Application", StringComparison.Ordinal));
        Assert.Contains(folders, f => f.Contains("Qala.Game.Matches.Application", StringComparison.Ordinal));
        Assert.Contains(folders, f => f.Contains("Qala.Game.Players.Application", StringComparison.Ordinal));
        Assert.Contains(folders, f => f.Contains("Qala.Game.Auth.Host", StringComparison.Ordinal));
    }

    [Fact]
    public void ErrorKeysUsedInCode_ShouldExistInEnglishResources_WhenDeclared()
    {
        var all = Folders()
            .SelectMany(folder => Keys(Path.Combine(BackendRoot(), folder, "en.json")))
            .ToHashSet(StringComparer.Ordinal);
        var declared = new[]
            {
                typeof(Qala.Framework.Application.Localization.LocalizationKeys),
                typeof(Qala.Game.Matches.Domain.Constants.MatchErrors),
            }
            .SelectMany(t => t.GetFields())
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);

        Assert.All(declared, key => Assert.Contains(key, all));
    }

    private static HashSet<string> Keys(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var keys = new HashSet<string>(StringComparer.Ordinal);
        Walk(document.RootElement, string.Empty, keys);
        return keys;
    }

    private static void Walk(JsonElement element, string prefix, HashSet<string> keys)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix.Length == 0 ? property.Name : $"{prefix}:{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Walk(property.Value, key, keys);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString()), $"{key} is empty in {element}");
                keys.Add(key);
            }
        }
    }

    private static string BackendRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Qala.Game.sln")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Qala.Game.sln not found");
    }
}
