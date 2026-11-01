using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HeartlessLullabyRU
{
    internal sealed class FontManager
    {
        private sealed class Recipe
        {
            public readonly string Match;
            public readonly string Resource;
            public readonly string Label;

            public Recipe(string match, string resource, string label)
            {
                Match = match;
                Resource = resource;
                Label = label;
            }
        }

        private static readonly Recipe[] Recipes = new Recipe[]
        {
            new Recipe("Long Shot", "OswaldSemiBold.ttf", "Oswald"),
            new Recipe("mytype", "PTMono-Regular.ttf", "PT Mono"),
            new Recipe("Fresco Stamp", "OldStandard-Regular.ttf", "Old Standard"),
            new Recipe("sleigher", "RubikWetPaint-Regular.ttf", "Rubik Wet Paint"),
            new Recipe("Vampire Wars", "RussoOne-Regular.ttf", "Russo One")
        };

        private const string Charset =
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "«»—–…№’‘“”„•";

        private readonly Dictionary<string, TMP_FontAsset> _customFallbacks = new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
        private readonly Dictionary<string, Font> _loadedFonts = new Dictionary<string, Font>(StringComparer.Ordinal);
        private readonly HashSet<string> _failedAssets = new HashSet<string>(StringComparer.Ordinal);
        private TMP_FontAsset _bookAntiquaFallback;
        private TMP_FontAsset _liberationFallback;
        private bool _initialized;
        private bool _refreshNeeded;

        public void EnsureApplied()
        {
            if (!_initialized)
            {
                if (!Initialize()) return;
                _initialized = true;
                _refreshNeeded = true;
            }

            if (ApplyFallbackTables()) _refreshNeeded = true;

            if (_refreshNeeded)
            {
                RefreshTextMeshes();
                _refreshNeeded = false;
            }
        }

        private bool Initialize()
        {
            _liberationFallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
            if (_liberationFallback == null) _liberationFallback = FindFontAsset("LiberationSans SDF - Fallback");

            Font bookAntiqua = Resources.Load<Font>("Fonts & Materials/BKANT");
            if (bookAntiqua == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                for (int i = 0; i < fonts.Length; i++)
                {
                    if (fonts[i] != null && string.Equals(fonts[i].name, "BKANT", StringComparison.OrdinalIgnoreCase))
                    {
                        bookAntiqua = fonts[i];
                        break;
                    }
                }
            }

            if (bookAntiqua != null)
            {
                try
                {
                    _bookAntiquaFallback = TMP_FontAsset.CreateFontAsset(bookAntiqua);
                    if (_bookAntiquaFallback != null)
                    {
                        _bookAntiquaFallback.name = "BKANT SDF RU Dynamic";
                        Plugin.Log.LogInfo("Created dynamic Book Antiqua fallback for Cyrillic.");
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Could not create Book Antiqua fallback: " + e.Message);
                }
            }

            bool ok = _liberationFallback != null || _bookAntiquaFallback != null;
            if (!ok) Plugin.Log.LogWarning("No Cyrillic TMP fallback font could be prepared.");
            return ok;
        }

        private bool ApplyFallbackTables()
        {
            bool changed = false;
            HashSet<string> usedFonts = CollectUsedFontKeys();
            TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                TMP_FontAsset asset = assets[i];
                if (asset == null || asset == _bookAntiquaFallback || asset == _liberationFallback) continue;
                if (asset.name != null && asset.name.StartsWith("RU ", StringComparison.Ordinal)) continue;

                string key = GetFontKey(asset);
                TMP_FontAsset customFallback = null;
                if (usedFonts.Contains(key))
                {
                    customFallback = GetCustomFallback(asset, key);
                }
                else
                {
                    _customFallbacks.TryGetValue(key, out customFallback);
                }

                List<TMP_FontAsset> fallbacks = asset.fallbackFontAssetTable;
                if (fallbacks == null)
                {
                    fallbacks = new List<TMP_FontAsset>();
                    asset.fallbackFontAssetTable = fallbacks;
                }

                if (customFallback != null)
                {
                    if (!fallbacks.Contains(customFallback))
                    {
                        fallbacks.Insert(0, customFallback);
                        changed = true;
                    }
                    if (_liberationFallback != null && !fallbacks.Contains(_liberationFallback))
                    {
                        fallbacks.Add(_liberationFallback);
                        changed = true;
                    }
                    if (_bookAntiquaFallback != null && !fallbacks.Contains(_bookAntiquaFallback))
                    {
                        fallbacks.Add(_bookAntiquaFallback);
                        changed = true;
                    }
                    continue;
                }

                bool isBookAntiqua = asset.name != null && asset.name.IndexOf("BKANT", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isBookAntiqua)
                {
                    if (_bookAntiquaFallback != null && !fallbacks.Contains(_bookAntiquaFallback))
                    {
                        fallbacks.Insert(0, _bookAntiquaFallback);
                        changed = true;
                    }
                    if (_liberationFallback != null && !fallbacks.Contains(_liberationFallback))
                    {
                        fallbacks.Add(_liberationFallback);
                        changed = true;
                    }
                }
                else
                {
                    if (_liberationFallback != null && !fallbacks.Contains(_liberationFallback))
                    {
                        fallbacks.Insert(0, _liberationFallback);
                        changed = true;
                    }
                    if (_bookAntiquaFallback != null && !fallbacks.Contains(_bookAntiquaFallback))
                    {
                        fallbacks.Add(_bookAntiquaFallback);
                        changed = true;
                    }
                }
            }

            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings != null)
            {
                FieldInfo field = typeof(TMP_Settings).GetField(
                    "m_fallbackFontAssets",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    List<TMP_FontAsset> globalFallbacks = field.GetValue(settings) as List<TMP_FontAsset>;
                    if (globalFallbacks == null)
                    {
                        globalFallbacks = new List<TMP_FontAsset>();
                        field.SetValue(settings, globalFallbacks);
                    }
                    if (_liberationFallback != null && !globalFallbacks.Contains(_liberationFallback))
                    {
                        globalFallbacks.Add(_liberationFallback);
                        changed = true;
                    }
                    if (_bookAntiquaFallback != null && !globalFallbacks.Contains(_bookAntiquaFallback))
                    {
                        globalFallbacks.Add(_bookAntiquaFallback);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static string GetFontKey(TMP_FontAsset asset)
        {
            return string.IsNullOrEmpty(asset.name) ? "#" + asset.GetInstanceID() : asset.name;
        }

        private static HashSet<string> CollectUsedFontKeys()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null) continue;
                TMP_FontAsset font = text.font;
                if (font == null) continue;
                keys.Add(GetFontKey(font));
            }
            return keys;
        }

        private TMP_FontAsset GetCustomFallback(TMP_FontAsset primary, string key)
        {
            TMP_FontAsset cached;
            if (_customFallbacks.TryGetValue(key, out cached)) return cached;
            if (_failedAssets.Contains(key)) return null;

            Recipe recipe = FindRecipe(primary.name);
            if (recipe == null) return null;

            TMP_FontAsset created = CreateFallback(primary, recipe);
            if (created == null)
            {
                _failedAssets.Add(key);
                return null;
            }
            _customFallbacks[key] = created;
            return created;
        }

        private static Recipe FindRecipe(string fontName)
        {
            if (fontName == null) return null;
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (fontName.IndexOf(Recipes[i].Match, StringComparison.OrdinalIgnoreCase) >= 0) return Recipes[i];
            }
            return null;
        }

        private TMP_FontAsset CreateFallback(TMP_FontAsset primary, Recipe recipe)
        {
            Font font;
            if (!_loadedFonts.TryGetValue(recipe.Resource, out font) || font == null)
            {
                string path = ExtractFont(recipe.Resource);
                if (path == null)
                {
                    Plugin.Log.LogWarning("Bundled font not found: " + recipe.Resource);
                    return null;
                }

                font = CreateFontFromPath(path);
                if (font == null)
                {
                    Plugin.Log.LogWarning("Could not load bundled font: " + recipe.Resource);
                    return null;
                }
                font.name = "RU " + recipe.Label;
                _loadedFonts[recipe.Resource] = font;
            }

            Material primaryMaterial = primary.material;
            int gradientScale = 6;
            if (primaryMaterial != null && primaryMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                gradientScale = Mathf.Clamp(Mathf.RoundToInt(primaryMaterial.GetFloat(ShaderUtilities.ID_GradientScale)), 2, 32);
            }
            int padding = gradientScale - 1;

            TMP_FontAsset asset = null;
            try
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    font,
                    90,
                    padding,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("CreateFontAsset failed for " + recipe.Resource + ": " + e.Message);
            }
            if (asset == null) return null;

            asset.name = "RU " + recipe.Label + " [" + primary.name + "]";

            if (primaryMaterial != null)
            {
                Material material = new Material(primaryMaterial);
                material.name = asset.name + " Material";
                material.SetTexture(ShaderUtilities.ID_MainTex, asset.atlasTexture);
                material.SetFloat(ShaderUtilities.ID_TextureWidth, asset.atlasWidth);
                material.SetFloat(ShaderUtilities.ID_TextureHeight, asset.atlasHeight);
                material.SetFloat(ShaderUtilities.ID_GradientScale, gradientScale);
                material.SetFloat(ShaderUtilities.ID_WeightNormal, asset.normalStyle);
                material.SetFloat(ShaderUtilities.ID_WeightBold, asset.boldStyle);
                asset.material = material;
                asset.materialHashCode = TMP_TextUtilities.GetSimpleHashCode(material.name);
            }

            string missing;
            asset.TryAddCharacters(GetCharset(), out missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Plugin.Log.LogWarning(asset.name + " is missing characters: " + missing);
            }

            var assetFallbacks = new List<TMP_FontAsset>(2);
            if (_liberationFallback != null) assetFallbacks.Add(_liberationFallback);
            if (_bookAntiquaFallback != null) assetFallbacks.Add(_bookAntiquaFallback);
            if (assetFallbacks.Count > 0) asset.fallbackFontAssetTable = assetFallbacks;

            Plugin.Log.LogInfo("Created Cyrillic fallback " + asset.name + " (padding " + padding + ", gradient " + gradientScale + ").");
            return asset;
        }

        private static string _charset;

        private static string GetCharset()
        {
            if (_charset != null) return _charset;

            var seen = new HashSet<char>();
            var sb = new StringBuilder(512);
            AppendChars(sb, seen, Charset);

            TranslationDataHandle data = Plugin.Data;
            if (data != null)
            {
                foreach (EntryTranslation entry in data.Dialogue.Values)
                {
                    AppendChars(sb, seen, entry.Text);
                    AppendChars(sb, seen, entry.Menu);
                }
                foreach (string value in data.UiMap.Values) AppendChars(sb, seen, value);
                foreach (string value in data.Actors.Values) AppendChars(sb, seen, value);
                foreach (string value in data.Items.Values) AppendChars(sb, seen, value);
            }

            _charset = sb.ToString();
            return _charset;
        }

        private static void AppendChars(StringBuilder sb, HashSet<char> seen, string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (c < 32) continue;
                if (c >= '\uD800' && c <= '\uDFFF') continue;
                if (seen.Add(c)) sb.Append(c);
            }
        }

        private static Font CreateFontFromPath(string path)
        {
            try
            {
                return new Font(path);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not load font from '" + path + "': " + e.Message);
                return null;
            }
        }

        private static string ExtractFont(string fileName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string resourceName = null;
            string[] names = asm.GetManifestResourceNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].EndsWith("." + fileName, StringComparison.Ordinal))
                {
                    resourceName = names[i];
                    break;
                }
            }
            if (resourceName == null) return null;

            byte[] data;
            try
            {
                using (Stream source = asm.GetManifestResourceStream(resourceName))
                {
                    if (source == null) return null;
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        source.CopyTo(buffer);
                        data = buffer.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not read bundled font " + fileName + ": " + e.Message);
                return null;
            }

            var directories = new List<string>(2);
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!string.IsNullOrEmpty(common))
            {
                directories.Add(Path.Combine(Path.Combine(common, "HeartlessLullabyRU"), "fonts"));
            }
            directories.Add(Path.Combine(Path.Combine(Path.GetTempPath(), "HeartlessLullabyRU"), "fonts"));

            for (int i = 0; i < directories.Count; i++)
            {
                string destination = Path.Combine(directories[i], fileName);
                try
                {
                    Directory.CreateDirectory(directories[i]);
                    FileInfo existing = new FileInfo(destination);
                    if (!existing.Exists || existing.Length != data.Length)
                    {
                        File.WriteAllBytes(destination, data);
                    }
                    return destination;
                }
                catch (Exception e)
                {
                    if (i == directories.Count - 1)
                    {
                        Plugin.Log.LogWarning("Could not extract " + fileName + " to '" + directories[i] + "': " + e.Message);
                    }
                }
            }
            return null;
        }

        private static TMP_FontAsset FindFontAsset(string name)
        {
            TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null && assets[i].name == name) return assets[i];
            }
            return null;
        }

        private static void RefreshTextMeshes()
        {
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null) continue;
                try
                {
                    text.ForceMeshUpdate();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
