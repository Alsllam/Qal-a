using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace Qala.Framework.Application.Localization;

/// <summary>
/// Loads <c>Resources/{culture}.json</c> files embedded in the registered assemblies (framework + modules) and merges
/// them per culture. Nested objects are flattened with <c>:</c>, so <c>{"General":{"Business":{"NotFound":"…"}}}</c>
/// is the key <c>General:Business:NotFound</c>.
/// </summary>
public sealed class JsonLocalizationStore
{
    public const string DefaultCulture = "en";

    private readonly IReadOnlyList<Assembly> _assemblies;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public JsonLocalizationStore(IEnumerable<Assembly> assemblies)
    {
        _assemblies = assemblies.Distinct().ToList();
    }

    /// <summary>All strings of <paramref name="culture"/> (two-letter name).</summary>
    public IReadOnlyDictionary<string, string> GetStrings(string culture) => _cache.GetOrAdd(culture, Load);

    private Dictionary<string, string> Load(string culture)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var suffix = $".Resources.{culture}.json";
        foreach (var assembly in _assemblies)
        {
            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var document = JsonDocument.Parse(stream);
                Flatten(document.RootElement, string.Empty, result);
            }
        }

        return result;
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> target)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix.Length == 0 ? property.Name : $"{prefix}:{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(property.Value, key, target);
            }
            else
            {
                target[key] = property.Value.GetString() ?? string.Empty;
            }
        }
    }
}

/// <summary>Localizes keys for <see cref="CultureInfo.CurrentUICulture"/>, falling back to English, then to the key.</summary>
public sealed class JsonStringLocalizer(JsonLocalizationStore store) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var value = Find(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = Find(name);
            var format = value ?? name;
            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, format, arguments), resourceNotFound: value is null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        store.GetStrings(CurrentCulture).Select(kv => new LocalizedString(kv.Key, kv.Value, false));

    private static string CurrentCulture => CurrentLanguage;

    /// <summary>
    /// The UI language (<c>ar</c>, <c>en</c>, …) taken from the culture name, which also works in
    /// globalization-invariant mode where <see cref="CultureInfo.TwoLetterISOLanguageName"/> is not reliable.
    /// </summary>
    public static string CurrentLanguage
    {
        get
        {
            var name = CultureInfo.CurrentUICulture.Name;
            var dash = name.IndexOf('-', StringComparison.Ordinal);
#pragma warning disable CA1308 // Language codes are lower case.
            return (dash < 0 ? name : name[..dash]).ToLowerInvariant();
#pragma warning restore CA1308
        }
    }

    private string? Find(string name) =>
        store.GetStrings(CurrentCulture).TryGetValue(name, out var value)
        || store.GetStrings(JsonLocalizationStore.DefaultCulture).TryGetValue(name, out value)
            ? value
            : null;
}

/// <summary>Every <see cref="IStringLocalizer{T}"/> shares the same merged JSON resources.</summary>
public sealed class JsonStringLocalizerFactory(JsonLocalizationStore store) : IStringLocalizerFactory
{
    public IStringLocalizer Create(Type resourceSource) => new JsonStringLocalizer(store);

    public IStringLocalizer Create(string baseName, string location) => new JsonStringLocalizer(store);
}

/// <summary>Shared localization keys (in Framework.Application/Resources).</summary>
public static class LocalizationKeys
{
    public const string NotFound = "General:Business:NotFound";
    public const string Forbidden = "General:Security:Forbidden";
    public const string Unauthorized = "General:Security:Unauthorized";
    public const string UnexpectedError = "General:Errors:Unexpected";
    public const string ServiceUnavailable = "General:Errors:ServiceUnavailable";
    public const string InvalidRequest = "General:Errors:InvalidRequest";
    public const string ConcurrencyConflict = "General:Errors:ConcurrencyConflict";
    public const string Required = "General:Fields:Required";
    public const string InvalidCharacters = "General:Fields:InvalidCharacters";
    public const string MaxLength = "General:Fields:MaxLength";
    public const string MinLength = "General:Fields:MinLength";
    public const string AlreadyExist = "General:Fields:AlreadyExist";
    public const string InvalidValue = "General:Fields:InvalidValue";
    public const string RateLimited = "General:Errors:RateLimited";
}
