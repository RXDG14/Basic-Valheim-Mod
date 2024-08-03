using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ValheimMod.Patches;


namespace ValheimMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "ValheimMod_v1";
        private const string modName = "ValheimMod";
        private const string modVersion = "1.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static Plugin Instance;

        internal ManualLogSource m;

        void Awake()
        {
            if(Instance == null)
            {
                Instance = this; 
            }

            m = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            m.LogInfo("Test mod active");

            harmony.PatchAll();
        }

        private void CreateModMenuObject()
        {
            var gameObject = new GameObject("ModMenu");
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.AddComponent<ModMenu>();
        }
    }
}
