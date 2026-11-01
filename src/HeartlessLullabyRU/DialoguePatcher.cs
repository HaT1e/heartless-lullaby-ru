using System;
using System.Reflection;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace HeartlessLullabyRU
{
    internal static class DialoguePatcher
    {
        private static DialogueDatabase _appliedDatabase;
        private static bool _luaSynced;
        private static int _luaSyncAttempts;
        private const int LuaSyncLogLimit = 5;

        public static void EnsureApplied()
        {
            if (!DialogueManager.hasInstance) return;
            DialogueDatabase database = DialogueManager.masterDatabase;
            if (database == null) return;
            if (!ReferenceEquals(database, _appliedDatabase))
            {
                Apply(database);
                _appliedDatabase = database;
                _luaSynced = false;
                _luaSyncAttempts = 0;
                Plugin.Log.LogInfo("Dialogue database patched.");
            }
            if (!_luaSynced) TrySyncLua(database);
        }

        private static void TrySyncLua(DialogueDatabase database)
        {
            try
            {
                int syncedActors = 0;
                int syncedItems = 0;
                string missingActor = null;
                string missingValue = null;

                for (int i = 0; i < database.actors.Count; i++)
                {
                    Actor actor = database.actors[i];
                    string name;
                    if (!Plugin.Data.Actors.TryGetValue(actor.id, out name)) continue;
                    string actorName = actor.Name;
                    if (string.IsNullOrEmpty(actorName)) continue;

                    DialogueLua.SetActorField(actorName, "Display Name", name);
                    DialogueLua.SetActorField(actorName, "Display Name en", name);
                    DialogueLua.SetActorField(actorName, "Display Name es", name);
                    DialogueLua.SetLocalizedActorField(actorName, "Display Name", name);
                    syncedActors++;

                    if (string.Equals(actorName, name, StringComparison.Ordinal)) continue;
                    string current = DialogueLua.GetLocalizedActorField(actorName, "Display Name").asString;
                    if (!string.Equals(current, name, StringComparison.Ordinal) && missingActor == null)
                    {
                        missingActor = actorName;
                        missingValue = current;
                    }
                }

                for (int i = 0; i < database.items.Count; i++)
                {
                    Item item = database.items[i];
                    string name;
                    if (!Plugin.Data.Items.TryGetValue(item.id, out name)) continue;
                    string itemName = item.Name;
                    if (string.IsNullOrEmpty(itemName)) continue;

                    DialogueLua.SetItemField(itemName, "Display Name", name);
                    DialogueLua.SetItemField(itemName, "Display Name en", name);
                    DialogueLua.SetItemField(itemName, "Display Name es", name);
                    DialogueLua.SetLocalizedItemField(itemName, "Display Name", name);
                    syncedItems++;
                }

                if (missingActor != null)
                {
                    _luaSyncAttempts++;
                    if (_luaSyncAttempts <= LuaSyncLogLimit)
                    {
                        Plugin.Log.LogWarning("Lua actor '" + missingActor + "' still not localized ('"
                            + missingValue + "'), attempt " + _luaSyncAttempts + ".");
                    }
                    return;
                }

                _luaSynced = true;
                Plugin.Log.LogInfo("Lua display names synced: " + syncedActors + " actors, " + syncedItems
                    + " items (language '" + Localization.Language + "').");
            }
            catch (Exception e)
            {
                _luaSyncAttempts++;
                if (_luaSyncAttempts <= LuaSyncLogLimit)
                {
                    Plugin.Log.LogWarning("Lua display name sync not ready: " + e.Message);
                }
            }
        }

        private static void Apply(DialogueDatabase database)
        {
            int patchedEntries = 0;

            for (int c = 0; c < database.conversations.Count; c++)
            {
                Conversation conversation = database.conversations[c];
                for (int e = 0; e < conversation.dialogueEntries.Count; e++)
                {
                    DialogueEntry entry = conversation.dialogueEntries[e];
                    string key = conversation.id + ":" + entry.id;
                    EntryTranslation translation;
                    if (!Plugin.Data.Dialogue.TryGetValue(key, out translation)) continue;

                    Field.SetValue(entry.fields, "Dialogue Text", translation.Text);
                    Field.SetValue(entry.fields, "en", translation.Text);
                    Field.SetValue(entry.fields, "es", translation.Text);

                    if (!string.IsNullOrEmpty(translation.Menu))
                    {
                        Field.SetValue(entry.fields, "Menu Text", translation.Menu);
                        Field.SetValue(entry.fields, "Menu Text en", translation.Menu);
                        Field.SetValue(entry.fields, "Menu Text es", translation.Menu);
                    }
                    patchedEntries++;
                }
            }

            for (int i = 0; i < database.actors.Count; i++)
            {
                Actor actor = database.actors[i];
                string name;
                if (!Plugin.Data.Actors.TryGetValue(actor.id, out name)) continue;
                Field.SetValue(actor.fields, "Display Name", name);
                Field.SetValue(actor.fields, "Display Name en", name);
                Field.SetValue(actor.fields, "Display Name es", name);
            }

            for (int i = 0; i < database.items.Count; i++)
            {
                Item item = database.items[i];
                string name;
                if (!Plugin.Data.Items.TryGetValue(item.id, out name)) continue;
                Field.SetValue(item.fields, "Display Name", name);
                Field.SetValue(item.fields, "Display Name en", name);
                Field.SetValue(item.fields, "Display Name es", name);
            }

            Plugin.Log.LogInfo("Patched dialogue entries: " + patchedEntries + "/" + Plugin.Data.Dialogue.Count);
        }

        public static void PatchQuestLogWindows()
        {
            QuestLogWindow[] windows = Resources.FindObjectsOfTypeAll<QuestLogWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                QuestLogWindow window = windows[i];
                if (window == null) continue;
                SetField(window, "noActiveQuestsText", "Нет активных заданий");
                SetField(window, "noCompletedQuestsText", "Нет завершённых заданий");
            }
        }

        private static void SetField(object target, string fieldName, string value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            if (field != null && field.FieldType == typeof(string))
            {
                field.SetValue(target, value);
            }
        }
    }
}
