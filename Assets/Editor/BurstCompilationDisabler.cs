#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TiraWantToCross.Editor
{
    [InitializeOnLoad]
    internal static class BurstCompilationDisabler
    {
        private const string BurstCompilationEditorPrefKey = "BurstCompilation";

        static BurstCompilationDisabler()
        {
            DisableBurstCompilation();
        }

        [MenuItem("TiraWantToCross/Maintenance/Disable Burst Compilation")]
        private static void DisableBurstCompilationFromMenu()
        {
            DisableBurstCompilation();
            Debug.Log("[BurstCompilationDisabler] Burst editor compilation is disabled.");
        }

        private static void DisableBurstCompilation()
        {
            EditorPrefs.SetBool(BurstCompilationEditorPrefKey, false);
            TrySetBurstEditorOption(false);
        }

        private static void TrySetBurstEditorOption(bool enabled)
        {
            try
            {
                var optionsType = Type.GetType("Unity.Burst.Editor.BurstEditorOptions, Unity.Burst.Editor");
                var property = optionsType?.GetProperty(
                    "EnableBurstCompilation",
                    BindingFlags.Public | BindingFlags.Static);
                property?.SetValue(null, enabled);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BurstCompilationDisabler] Could not update Burst option immediately: {exception.Message}");
            }
        }
    }
}
#endif
