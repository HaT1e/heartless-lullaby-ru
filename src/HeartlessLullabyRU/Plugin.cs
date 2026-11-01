using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HeartlessLullabyRU
{
    [BepInPlugin(Guid, "Heartless Lullaby - Russian Localization", "1.1.5")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "ru.heartless.lullaby";

        internal static ManualLogSource Log;
        internal static TranslationDataHandle Data;
        internal static FontManager Fonts;
        internal static SpriteReplacer Sprites;
        internal static Harmony HarmonyInstance;

        private void Awake()
        {
            Log = Logger;
            try
            {
                Data = TranslationData.Load();
                Fonts = new FontManager();
                Sprites = new SpriteReplacer();
                HarmonyInstance = new Harmony(Guid);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("Russian localization loaded: " + Data.Dialogue.Count + " dialogue entries, "
                    + Data.UiMap.Count + " UI strings, " + Data.SpriteFiles.Count + " sprite replacements.");
                StartCoroutine(ApplyLoop());
            }
            catch (Exception e)
            {
                Log.LogError("Failed to initialize Russian localization: " + e);
            }
        }

        private IEnumerator ApplyLoop()
        {
            yield return null;
            while (true)
            {
                try
                {
                    DialoguePatcher.EnsureApplied();
                    Fonts.EnsureApplied();
                    DialoguePatcher.PatchQuestLogWindows();
                    Sprites.ScanAll();
                    TextTranslator.ScanAll();
                }
                catch (Exception e)
                {
                    Log.LogError("ApplyLoop: " + e);
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
}

namespace HeartlessLullabyRU
{
    using TMPro;
    using UnityEngine;

    [HarmonyPatch(typeof(TMP_Text), "set_text")]
    internal static class TmpTextPatch
    {
        private static void Prefix(ref string value)
        {
            value = TextTranslator.Translate(value);
        }
    }
}

namespace HeartlessLullabyRU
{
    using UnityEngine;
    using UnityEngine.UI;

    [HarmonyPatch(typeof(Image), "set_sprite")]
    internal static class ImageSpritePatch
    {
        private static void Prefix(ref Sprite value)
        {
            if (Plugin.Sprites == null || value == null) return;
            Sprite replacement = Plugin.Sprites.Get(value);
            if (replacement != null) value = replacement;
        }
    }

    [HarmonyPatch(typeof(Image), "set_overrideSprite")]
    internal static class ImageOverrideSpritePatch
    {
        private static void Prefix(ref Sprite value)
        {
            if (Plugin.Sprites == null || value == null) return;
            Sprite replacement = Plugin.Sprites.Get(value);
            if (replacement != null) value = replacement;
        }
    }
}
