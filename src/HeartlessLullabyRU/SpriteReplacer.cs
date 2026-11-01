using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HeartlessLullabyRU
{
    internal sealed class SpriteReplacer
    {
        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly HashSet<string> _missing = new HashSet<string>(StringComparer.Ordinal);
        private Assembly _assembly;

        public SpriteReplacer()
        {
            _assembly = Assembly.GetExecutingAssembly();
        }

        public Sprite Get(Sprite original)
        {
            if (original == null || original.name == null) return null;
            if (original.name.StartsWith("RU_", StringComparison.Ordinal)) return null;

            string fileName;
            if (Plugin.Data == null || !Plugin.Data.SpriteFiles.TryGetValue(original.name, out fileName)) return null;

            Sprite cached;
            if (_cache.TryGetValue(original.name, out cached)) return cached;
            if (_missing.Contains(original.name)) return null;

            try
            {
                Sprite created = LoadSprite(original, fileName);
                if (created == null)
                {
                    _missing.Add(original.name);
                    return null;
                }
                _cache[original.name] = created;
                return created;
            }
            catch (Exception e)
            {
                _missing.Add(original.name);
                Plugin.Log.LogWarning("Could not replace sprite '" + original.name + "': " + e.Message);
                return null;
            }
        }

        private Sprite LoadSprite(Sprite original, string fileName)
        {
            string resourceName = null;
            foreach (string name in _assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("." + fileName, StringComparison.Ordinal))
                {
                    resourceName = name;
                    break;
                }
            }
            if (resourceName == null) return null;

            byte[] bytes;
            using (Stream stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    bytes = memory.ToArray();
                }
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes)) return null;
            texture.name = "RU_" + original.name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, true);

            Rect originalRect = original.rect;
            Vector2 pivot = new Vector2(
                originalRect.width > 0f ? original.pivot.x / originalRect.width : 0.5f,
                originalRect.height > 0f ? original.pivot.y / originalRect.height : 0.5f);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                pivot,
                original.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                original.border);
            sprite.name = "RU_" + original.name;
            return sprite;
        }

        public void ScanAll()
        {
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) continue;

                if (image.sprite != null)
                {
                    Sprite replacement = Get(image.sprite);
                    if (replacement != null) image.sprite = replacement;
                }

                if (image.overrideSprite != null)
                {
                    Sprite replacement = Get(image.overrideSprite);
                    if (replacement != null) image.overrideSprite = replacement;
                }
            }
        }
    }
}
