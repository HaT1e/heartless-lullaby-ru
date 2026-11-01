using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HeartlessLullabyRU
{
    internal sealed class EntryTranslation
    {
        public string Text;
        public string Menu;
    }

    internal static class TranslationData
    {
        public static Dictionary<string, EntryTranslation> Dialogue;
        public static Dictionary<string, string> UiMap;
        public static Dictionary<int, string> Actors;
        public static Dictionary<int, string> Items;
        public static Dictionary<string, string> SpriteFiles;

        public static TranslationDataHandle Loaded;

        public static TranslationDataHandle Load()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            JObject dialogueJson = JObject.Parse(ReadText(asm, "ru_dialogue.json"));
            var dialogue = new Dictionary<string, EntryTranslation>(StringComparer.Ordinal);
            JToken lines = dialogueJson["lines"];
            if (lines != null)
            {
                foreach (JProperty property in lines.Children<JProperty>())
                {
                    JToken line = property.Value;
                    dialogue[property.Name] = new EntryTranslation
                    {
                        Text = (string)line["text"],
                        Menu = (string)line["menu"]
                    };
                }
            }

            JObject uiJson = JObject.Parse(ReadText(asm, "ru_ui.json"));
            var uiMap = new Dictionary<string, string>(StringComparer.Ordinal);
            AddMap(uiMap, uiJson["uis"]);
            AddMap(uiMap, uiJson["tmp"]);
            AddMap(uiMap, uiJson["questJournal"]);

            var actors = new Dictionary<int, string>();
            AddIntMap(actors, uiJson["actors"]);

            var items = new Dictionary<int, string>();
            AddIntMap(items, uiJson["items"]);

            var spriteFiles = new Dictionary<string, string>(StringComparer.Ordinal);
            string spriteJsonText = ReadTextOrNull(asm, "ru_sprites.json");
            if (spriteJsonText != null)
            {
                JObject spriteJson = JObject.Parse(spriteJsonText);
                JToken map = spriteJson["map"];
                if (map != null)
                {
                    foreach (JProperty property in map.Children<JProperty>())
                    {
                        spriteFiles[property.Name] = (string)property.Value;
                    }
                }
            }

            Dialogue = dialogue;
            UiMap = uiMap;
            Actors = actors;
            Items = items;
            SpriteFiles = spriteFiles;

            var result = new TranslationDataHandle(dialogue, uiMap, actors, items, spriteFiles);
            Loaded = result;
            return result;
        }

        private static void AddMap(Dictionary<string, string> target, JToken token)
        {
            if (token == null) return;
            foreach (JProperty property in token.Children<JProperty>())
            {
                target[property.Name] = (string)property.Value;
            }
        }

        private static void AddIntMap(Dictionary<int, string> target, JToken token)
        {
            if (token == null) return;
            foreach (JProperty property in token.Children<JProperty>())
            {
                int id;
                if (int.TryParse(property.Name, out id))
                {
                    target[id] = (string)property.Value;
                }
            }
        }

        private static string ReadText(Assembly asm, string suffix)
        {
            string text = ReadTextOrNull(asm, suffix);
            if (text == null) throw new FileNotFoundException("Embedded resource not found: " + suffix);
            return text;
        }

        private static string ReadTextOrNull(Assembly asm, string suffix)
        {
            string resourceName = null;
            foreach (string name in asm.GetManifestResourceNames())
            {
                if (name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    resourceName = name;
                    break;
                }
            }
            if (resourceName == null) return null;
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    internal sealed class TranslationDataHandle
    {
        public readonly Dictionary<string, EntryTranslation> Dialogue;
        public readonly Dictionary<string, string> UiMap;
        public readonly Dictionary<int, string> Actors;
        public readonly Dictionary<int, string> Items;
        public readonly Dictionary<string, string> SpriteFiles;

        public TranslationDataHandle(
            Dictionary<string, EntryTranslation> dialogue,
            Dictionary<string, string> uiMap,
            Dictionary<int, string> actors,
            Dictionary<int, string> items,
            Dictionary<string, string> spriteFiles)
        {
            Dialogue = dialogue;
            UiMap = uiMap;
            Actors = actors;
            Items = items;
            SpriteFiles = spriteFiles;
        }
    }
}
