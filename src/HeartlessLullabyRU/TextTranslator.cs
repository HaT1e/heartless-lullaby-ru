using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;

namespace HeartlessLullabyRU
{
    internal static class TextTranslator
    {
        private static readonly Regex TimeAm = new Regex(@"([0-9]{1,2}:[0-9]{2})\s*AM\b", RegexOptions.Compiled);
        private static readonly Regex TimePm = new Regex(@"([0-9]{1,2}:[0-9]{2})\s*PM\b", RegexOptions.Compiled);
        private static readonly Regex ColoredNamePrefix = new Regex(
            @"^(<color(?:=[^>]+)?>)([^<]+)(</color>)(:?)([\s\S]*)$", RegexOptions.Compiled);

        private static Dictionary<string, string> _actorNames;
        private static DialogueDatabase _actorNamesDatabase;

        public static string Translate(string value)
        {
            if (string.IsNullOrEmpty(value) || Plugin.Data == null) return value;

            string mapped;
            if (Plugin.Data.UiMap.TryGetValue(value, out mapped)) return mapped;

            string trimmed = value.Trim();
            if (trimmed.Length != value.Length && trimmed.Length > 0 && Plugin.Data.UiMap.TryGetValue(trimmed, out mapped))
            {
                int index = value.IndexOf(trimmed, StringComparison.Ordinal);
                return value.Substring(0, index) + mapped + value.Substring(index + trimmed.Length);
            }

            if (_actorNames != null && _actorNames.TryGetValue(value, out mapped)) return mapped;

            if (_actorNames != null)
            {
                int colon = value.IndexOf(": ", StringComparison.Ordinal);
                if (colon > 0 && _actorNames.TryGetValue(value.Substring(0, colon), out mapped))
                {
                    return mapped + value.Substring(colon);
                }

                Match colored = ColoredNamePrefix.Match(value);
                if (colored.Success)
                {
                    string inner = colored.Groups[2].Value;
                    int end = inner.Length;
                    while (end > 0 && (inner[end - 1] == ':' || inner[end - 1] == ' ')) end--;
                    string core = inner.Substring(0, end);
                    if (core.Length > 0
                        && (_actorNames.TryGetValue(core, out mapped) || Plugin.Data.UiMap.TryGetValue(core, out mapped)))
                    {
                        return colored.Groups[1].Value + mapped + inner.Substring(end)
                            + colored.Groups[3].Value + colored.Groups[4].Value + colored.Groups[5].Value;
                    }
                }
            }

            if (value.StartsWith("Alert ", StringComparison.Ordinal))
            {
                return "Внимание: " + Translate(value.Substring(6));
            }

            if (value.IndexOf(':') >= 0 && (value.IndexOf(" AM", StringComparison.Ordinal) >= 0 || value.IndexOf(" PM", StringComparison.Ordinal) >= 0))
            {
                string result = TimeAm.Replace(value, "$1 УТРА");
                result = TimePm.Replace(result, "$1 ВЕЧЕРА");
                if (result != value) return result;
            }

            return value;
        }

        private static void EnsureActorNames()
        {
            if (!DialogueManager.hasInstance) return;
            DialogueDatabase database = DialogueManager.masterDatabase;
            if (database == null || ReferenceEquals(database, _actorNamesDatabase)) return;

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < database.actors.Count; i++)
            {
                Actor actor = database.actors[i];
                string name;
                if (!Plugin.Data.Actors.TryGetValue(actor.id, out name)) continue;
                if (string.IsNullOrEmpty(actor.Name)) continue;

                map[actor.Name] = name;
                string upper = actor.Name.ToUpperInvariant();
                if (!string.Equals(upper, actor.Name, StringComparison.Ordinal))
                {
                    map[upper] = name.ToUpperInvariant();
                }
            }
            _actorNames = map;
            _actorNamesDatabase = database;
        }

        public static void ScanAll()
        {
            EnsureActorNames();
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null) continue;
                try
                {
                    string current = text.text;
                    string translated = Translate(current);
                    if (!string.Equals(current, translated, StringComparison.Ordinal))
                    {
                        text.text = translated;
                    }

                    if (string.Equals(text.text, "ПЯТНИЦА, 5-Е", StringComparison.Ordinal) && text.enableWordWrapping)
                    {
                        text.enableWordWrapping = false;
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
