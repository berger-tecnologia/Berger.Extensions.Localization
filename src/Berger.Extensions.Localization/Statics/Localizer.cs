using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using System.Collections.Concurrent;

namespace Berger.Extensions.Localization
{
    public static class Localizer
    {
        #region Properties
        public const string Fallback = "en";
        public static bool IsConfigured { get; private set; } = false;
        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertyCache = new();
        public static HashSet<string> Optional { get; private set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public static HashSet<string> Supported => new HashSet<string>(Required.Union(Optional), StringComparer.InvariantCultureIgnoreCase);
        public static HashSet<string> Required { get; private set; } = new HashSet<string>(new[] { Fallback }, StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Methods
        public static void Configure(IEnumerable<string> required, IEnumerable<string> optional)
        {
            Required = required.ToHashSet();
            Optional = optional.ToHashSet();

            IsConfigured = true;
        }
        public static string GetLanguageCode()
        {
            var code = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (!string.IsNullOrWhiteSpace(code))
            {
                return code;
            }

            return Fallback;
        }
        public static IEnumerable<T> Localize<T>(this IEnumerable<T> items, string language, Depth depth = Depth.Flat) where T : class, ILocalizable, new()
        {
            return items.Select(item => Localize(item, language, depth));
        }
        public static IEnumerable<T> Localize<T>(this IEnumerable<T> items, Depth depth = Depth.Flat) where T : class, ILocalizable, new()
        {
            var code = GetLanguageCode();

            return items.Select(item => Localize(item, code, depth));
        }
        public static T Localize<T>(this T item, in string code, in Depth depth = Depth.Flat) where T : class, ILocalizable
        {
            if (item is null)
                return null;

            var depthChain = new List<object>(32);

            LocalizeItem(item, Supported.Contains(code) ? code : Supported.First(), depthChain, depth);

            return item;
        }
        public static T Localize<T>(this T item, in Depth depth = Depth.Flat) where T : class, ILocalizable
        {
            if (item is null)
                return null;

            var code = GetLanguageCode();
            
            var depthChain = new List<object>(32);

            LocalizeItem(item, Supported.Contains(code) ? code : Supported.First(), depthChain, depth);

            return item;
        }
        private static void LocalizeItem(in object item, in string code, in List<object> depthChain, in Depth depth = Depth.Flat)
        {
            foreach (var property in _propertyCache.GetOrAdd(item.GetType(), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty)))
            {
                if (property.IsDefined(typeof(LocalizedAttribute), true))
                    TryLocalizeProperty(item, property, code);
                else if (depth != Depth.Flat)
                    TryLocalizeChildren(item, property, code, depthChain, depth);
            }
        }
        private static void TryLocalizeProperty(in object item, in PropertyInfo propertyInfo, in string code)
        {
            var propertyValue = propertyInfo.GetValue(item)?.ToString();

            if (string.IsNullOrWhiteSpace(propertyValue))
                return;

            if (!TryDeserialize(propertyValue, out var localizedContents))
                return;

            var content = GetContentForLanguage(localizedContents, code);

            propertyInfo.SetValue(item, content, null);
        }
        private static void TryLocalizeProperty(in object @base, in object member, in string code, in List<object> depthChain, Depth depth = Depth.Flat)
        {
            if (SkipItemLocalization(@base, member))
                return;

            TryAddToDepthChain(@base, depthChain);

            if (!TryAddToDepthChain(member, depthChain))
                return;

            if (depth == Depth.OneLevel)
                depth = Depth.Flat;

            LocalizeItem(member, code, depthChain, depth);
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
        private static void TryLocalizeChildren(in object item, in PropertyInfo property, in string code, in List<object> depthChain, in Depth depth)
        {
            if (typeof(ILocalizable).IsAssignableFrom(property.PropertyType))
                TryLocalizeProperty(item, property.GetValue(item, null), code, depthChain, depth);

            else if (typeof(IEnumerable<ILocalizable>).IsAssignableFrom(property.PropertyType))
            {
                foreach (var member in (IEnumerable<object>)property.GetValue(item, null))
                {
                    TryLocalizeProperty(item, member, code, depthChain, depth);
                }
            }
        }
        public static string Serialize(in IDictionary<string, string> content)
        {
            using (var sw = new StringWriter())
            using (var jw = new JsonTextWriter(sw))
            {
                jw.WriteStartObject();

                foreach (var code in Supported)
                {
                    if (content.TryGetValue(code, out var value))
                    {
                        jw.WritePropertyName(code);
                        jw.WriteValue(value);
                    }
                }

                jw.WriteEndObject();

                return sw.ToString();
            }
        }
        public static bool TryDeserialize(in string json, out IDictionary<string, string> content)
        {
            try
            {
                content = Deserialize(json);

                return true;
            }
            catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
            {
                content = new Dictionary<string, string>
                {
                    [Supported.First()] = string.Empty
                };

                return false;
            }
        }
        public static IDictionary<string, string> Deserialize(in string json)
        {
            var item = JsonConvert.DeserializeObject<IDictionary<string, string>>(json);

            foreach (var key in item.Keys)
            {
                if (!Supported.Contains(key))
                {
                    item.Remove(key);
                }
            }

            return item;
        }
        private static string GetContentForLanguage(in IDictionary<string, string> localized, in string code)
        {
            if (localized.Count == 0)
            {
                throw new ArgumentException("Cannot localize property.", nameof(localized));
            }

            if (localized.TryGetValue(code, out var content) && !string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            return GetContentForFirstLanguage(localized);
        }
        private static string GetContentForFirstLanguage(in IDictionary<string, string> localized)
        {
            return localized.TryGetValue(Supported.First(), out var content) ? content : localized.First().Value;
        }
        #endregion
    }
}