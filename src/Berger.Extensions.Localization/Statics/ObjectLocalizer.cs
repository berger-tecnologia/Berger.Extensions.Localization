using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using System.Collections.Concurrent;

namespace Berger.Extensions.Localization
{
    public static class ObjectLocalizer
    {
        public static void Configure(IEnumerable<string> requiredLanguages, IEnumerable<string> optionalLanguages)
        {
            RequiredLanguages = requiredLanguages.ToHashSet();
            OptionalLanguages = optionalLanguages.ToHashSet();

            IsConfigured = true;
        }

        public const string FallbackLanguageCode = "en";

        public static bool IsConfigured { get; private set; } = false;
        public static HashSet<string> RequiredLanguages { get; private set; } = new HashSet<string>(new[] { FallbackLanguageCode }, StringComparer.InvariantCultureIgnoreCase);
        public static HashSet<string> OptionalLanguages { get; private set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public static HashSet<string> SupportedLanguages => new HashSet<string>(RequiredLanguages.Union(OptionalLanguages), StringComparer.InvariantCultureIgnoreCase);

        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertyCache = new();

        public static string GetLanguageCode()
        {
            var languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                return languageCode;
            }

            return FallbackLanguageCode;
        }

        public static IEnumerable<T> Localize<T>(this IEnumerable<T> items, string languageCode, LocalizationDepth depth = LocalizationDepth.Shallow)
            where T : class, ILocalizable, new()
        {
            return items.Select(item => Localize(item, languageCode, depth));
        }

        public static IEnumerable<T> Localize<T>(this IEnumerable<T> items, LocalizationDepth depth = LocalizationDepth.Shallow)
            where T : class, ILocalizable, new()
        {
            var languageCode = GetLanguageCode();

            return items.Select(item => Localize(item, languageCode, depth));
        }

        public static T Localize<T>(this T item, in string languageCode, in LocalizationDepth depth = LocalizationDepth.Shallow)
            where T : class, ILocalizable
        {
            if (item is null)
                return null;

            var depthChain = new List<object>(32);

            LocalizeItem(item, SupportedLanguages.Contains(languageCode) ? languageCode : SupportedLanguages.First(), depthChain, depth);

            return item;
        }

        public static T Localize<T>(this T item, in LocalizationDepth depth = LocalizationDepth.Shallow)
            where T : class, ILocalizable
        {
            if (item is null)
                return null;

            var languageCode = GetLanguageCode();
            
            var depthChain = new List<object>(32);

            LocalizeItem(item, SupportedLanguages.Contains(languageCode) ? languageCode : SupportedLanguages.First(), depthChain, depth);

            return item;
        }

        private static void LocalizeItem(in object item, in string languageCode, in List<object> depthChain, in LocalizationDepth depth = LocalizationDepth.Shallow)
        {
            foreach (var property in _propertyCache.GetOrAdd(item.GetType(), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty)))
            {
                if (property.IsDefined(typeof(LocalizedAttribute), true))
                    TryLocalizeProperty(item, property, languageCode);
                else if (depth != LocalizationDepth.Shallow)
                    TryLocalizeChildren(item, property, languageCode, depthChain, depth);
            }
        }
        private static void TryLocalizeProperty(in object item, in PropertyInfo propertyInfo, in string languageCode)
        {
            var propertyValue = propertyInfo.GetValue(item)?.ToString();

            if (string.IsNullOrWhiteSpace(propertyValue))
                return;

            if (!TryDeserialize(propertyValue, out var localizedContents))
                return;

            var contentForLanguage = GetContentForLanguage(localizedContents, languageCode);

            propertyInfo.SetValue(item, contentForLanguage, null);
        }

        private static void TryLocalizeProperty(in object @base, in object member, in string languageCode, in List<object> depthChain, LocalizationDepth depth = LocalizationDepth.Shallow)
        {
            if (SkipItemLocalization(@base, member))
                return;

            TryAddToDepthChain(@base, depthChain);

            if (!TryAddToDepthChain(member, depthChain))
                return;

            if (depth == LocalizationDepth.OneLevel)
                depth = LocalizationDepth.Shallow;

            LocalizeItem(member, languageCode, depthChain, depth);
        }

        private static bool SkipItemLocalization(in object @base, in object member)
        {
            if (@base is null || member is null)
                return true;

            return ReferenceEquals(@base, member);
        }

        private static bool TryAddToDepthChain(object item, in List<object> depthChain)
        {
            if (item is null)
                return false;

            if (depthChain.AsParallel().Any(x => ReferenceEquals(item, x)))
                return false;

            depthChain.Add(item);

            return true;
        }

        private static void TryLocalizeChildren(in object item, in PropertyInfo property, in string languageCode, in List<object> depthChain, in LocalizationDepth depth)
        {
            if (typeof(ILocalizable).IsAssignableFrom(property.PropertyType))
                TryLocalizeProperty(item, property.GetValue(item, null), languageCode, depthChain, depth);

            else if (typeof(IEnumerable<ILocalizable>).IsAssignableFrom(property.PropertyType))
            {
                foreach (var member in (IEnumerable<object>)property.GetValue(item, null))
                {
                    TryLocalizeProperty(item, member, languageCode, depthChain, depth);
                }
            }
        }

        public static string Serialize(in IDictionary<string, string> content)
        {
            using (var sw = new StringWriter())
            using (var jw = new JsonTextWriter(sw))
            {
                jw.WriteStartObject();

                foreach (var languageCode in SupportedLanguages)
                {
                    if (content.TryGetValue(languageCode, out var value))
                    {
                        jw.WritePropertyName(languageCode);
                        jw.WriteValue(value);
                    }
                }

                jw.WriteEndObject();

                return sw.ToString();
            }
        }

        public static bool TryDeserialize(in string json, out IDictionary<string, string> localizedContent)
        {
            try
            {
                localizedContent = Deserialize(json);

                return true;
            }
            catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
            {
                localizedContent = new Dictionary<string, string>
                {
                    [SupportedLanguages.First()] = string.Empty
                };

                return false;
            }
        }

        public static IDictionary<string, string> Deserialize(in string json)
        {
            var item = JsonConvert.DeserializeObject<IDictionary<string, string>>(json);

            foreach (var key in item.Keys)
            {
                if (!SupportedLanguages.Contains(key))
                {
                    item.Remove(key);
                }
            }

            return item;
        }

        private static string GetContentForLanguage(in IDictionary<string, string> localizedContents, in string languageCode)
        {
            if (localizedContents.Count == 0)
            {
                throw new ArgumentException("Cannot localize property, no localized property values exist.", nameof(localizedContents));
            }

            if (localizedContents.TryGetValue(languageCode, out var content) && !string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            return GetContentForFirstLanguage(localizedContents);
        }

        private static string GetContentForFirstLanguage(in IDictionary<string, string> localizedContents)
        {
            return localizedContents.TryGetValue(SupportedLanguages.First(), out var content) ? content : localizedContents.First().Value;
        }
    }
}