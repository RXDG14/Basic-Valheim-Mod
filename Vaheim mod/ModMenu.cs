using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimMod.Patches
{
    internal class ModMenu : MonoBehaviour
    {
        private static GameObject canvasGameObject;
        private static GameObject panelGameObject;

        private static Button superJumpButton;
        private static Button superSpeedButton;
        private static Button invincibilityButton;
        private static Button superPunchButton;

        private static bool showModMenu = false;
        private static bool menuCreated = false;
        private static bool enableCursor = false;

        private static bool superJumpEnabled = false;
        private static bool superSpeedEnabled = false;
        private static bool superPunchEnabled = false;
        private static bool invincibilityEnabled = false;
        private static bool unlimitedStaminaEnabled = false;


        static Player player = FindObjectOfType<Player>();
        static Character character = FindObjectOfType<Character>();

        internal static ManualLogSource logSource = BepInEx.Logging.Logger.CreateLogSource("ModMenu");

        public static void InitializeModMenu()
        {
            if (!menuCreated)
            {
                CreateModMenu();
                menuCreated = true;
            }
        }

        [HarmonyPatch(typeof(Game), "Awake")]
        class Patch_Game
        {
            [HarmonyPrefix]
            static void PrefixGame(Game __instance)
            {
                FieldInfo isModdedField = typeof(Game).GetField("isModded", BindingFlags.Static | BindingFlags.Public);
                if (isModdedField != null)
                {
                    isModdedField.SetValue(__instance, true);
                }

                ModMenu.InitializeModMenu();
            }
        }


        [HarmonyPatch(typeof(Game), "Update")]
        class Patch_GamePlayerAbilities
        {
            [HarmonyPostfix]
            static void PosfixGamePlayer(Game __instance)
            {
                FieldInfo playerDamageRate = typeof(Game).GetField("m_playerDamageRate", BindingFlags.Static | BindingFlags.Public);
                if (playerDamageRate != null)
                {
                    if (superPunchEnabled)
                    {
                        playerDamageRate.SetValue(__instance, 9999f);
                    }
                    else
                    {
                        playerDamageRate.SetValue(__instance, 1f);
                    }

                }
            }
        }


        [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
        class Patch_cursor
        {
            [HarmonyPostfix]
            static void PatchCursor(GameCamera __instance)
            {
                FieldInfo mouseCaptureField = typeof(GameCamera).GetField("m_mouseCapture", BindingFlags.NonPublic | BindingFlags.Instance);

                if (mouseCaptureField != null)
                {
                    if (enableCursor)
                    {
                        mouseCaptureField.SetValue(__instance, enableCursor);
                        //logSource.LogInfo("mouse enabled");
                    }
                    else
                    {
                        mouseCaptureField.SetValue(__instance, enableCursor);
                        //logSource.LogInfo("mouse disabled");
                    }
                }
            }
        }


        [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
        class Patch_cursor2
        {
            [HarmonyPostfix]
            static void PatchCursor2()
            {
                if (enableCursor)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                if (Hud.InRadial() || InventoryGui.IsVisible() ||
                    TextInput.IsVisible() || Menu.IsVisible() ||
                    Minimap.IsOpen() || StoreGui.IsVisible() ||
                    Hud.IsPieceSelectionVisible() || PlayerCustomizaton.BarberBlocksLook() ||
                    UnifiedPopup.IsVisible() || showModMenu)
                {
                    enableCursor = true;
                }
                else
                {
                    enableCursor = false;
                }
            }
        }


        [HarmonyPatch(typeof(Player), "Update")]
        class Patch_ModMenu
        {
            [HarmonyPostfix]
            static void ShowModMenu()
            {
                if (Input.GetKeyDown(KeyCode.F4))// Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    logSource.LogInfo("Mod menu key pressed");
                    showModMenu = !showModMenu;
                    enableCursor = !enableCursor;

                    /*if (!menuCreated)
                    {
                        CreateModMenu();
                        menuCreated = true;
                    }*/

                    ToggleModMenu(showModMenu);
                }
            }
        }


        [HarmonyPatch(typeof(Player), "Update")]
        class Patch_PlayerAbilities
        {
            [HarmonyPostfix]
            static void SuperJumpPatch(ref Character __instance)
            {
                if (superJumpEnabled)
                {
                    __instance.m_jumpForce = 30f;
                }
                else
                {
                    __instance.m_jumpForce = 10f;
                }
            }


            [HarmonyPostfix]
            static void SuperSpeedPatch(ref Character __instance)
            {
                if (superSpeedEnabled)
                {
                    __instance.m_runSpeed = 50f;
                    __instance.m_swimSpeed = 50f;
                }
                else
                {
                    __instance.m_runSpeed = 20f;
                    __instance.m_swimSpeed = 2f;
                }
            }


            [HarmonyPostfix]
            static void InvincibilityPatch(ref Character __instance)
            {
                if (invincibilityEnabled)
                {
                    __instance.SetMaxHealth(101);
                    __instance.SetHealth(101);
                    __instance.AddStamina(1);
                }
            }


            [HarmonyPostfix]
            static void GodModePatch(ref Player __instance)
            {
                if (invincibilityEnabled)
                {
                    __instance.SetGodMode(true);
                }
                else
                {
                    __instance.SetGodMode(false);
                }
            }


            [HarmonyPostfix]
            static void MaxCarryWeightPatch(ref Player __instance)
            {
                if (invincibilityEnabled)
                {
                    __instance.m_maxCarryWeight = 9999f;

                }
                else
                {
                    __instance.m_maxCarryWeight = 300f;

                }
            }

        }


        static void CreateModMenu()
        {

            logSource.LogInfo("Creating Mod Menu Canvas");

            // Create Canvas
            canvasGameObject = new GameObject("ModMenuCanvas");
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasGameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasGameObject.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasGameObject);

            // Create Panel
            panelGameObject = new GameObject("ModMenuPanel");
            RectTransform panelRectTransform = panelGameObject.AddComponent<RectTransform>();
            panelRectTransform.sizeDelta = new Vector2(1000, 800); // Adjusted size to 400x400
            panelGameObject.AddComponent<CanvasRenderer>();
            Image panelImage = panelGameObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
            panelGameObject.transform.SetParent(canvasGameObject.transform, false);

            //DontDestroyOnLoad(panelGameObject);

            // Set Panel position
            panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelRectTransform.pivot = new Vector2(0.5f, 0.5f);
            panelRectTransform.anchoredPosition = Vector2.zero;

            // Create Buttons
            float spacing = 100f;
            float startY = 100f;

            superJumpButton = CreateButton(panelGameObject.transform, "Super Jump : OFF", new Vector2(0, startY), SuperJumpButton);
            superSpeedButton = CreateButton(panelGameObject.transform, "Super Speed : OFF", new Vector2(0, startY - spacing * 1), SuperSpeedButton);
            superPunchButton = CreateButton(panelGameObject.transform, "Super Punch : OFF", new Vector2(0, startY - spacing * 2), SuperPunchButton);
            invincibilityButton = CreateButton(panelGameObject.transform, "Invincibility : OFF", new Vector2(0, startY - spacing * 3), InvincibleButton);

            logSource.LogInfo("Mod Menu Canvas Created");
        }

        static void ToggleModMenu(bool show)
        {
            if (canvasGameObject != null && panelGameObject != null)
            {
                canvasGameObject.SetActive(show);
                panelGameObject.SetActive(show);
            }
        }

        static Button CreateButton(Transform parent, string buttonText, Vector2 position, UnityEngine.Events.UnityAction onClickAction)
        {
            // Create Button
            GameObject buttonGameObject = new GameObject(buttonText);
            RectTransform buttonRectTransform = buttonGameObject.AddComponent<RectTransform>();
            buttonRectTransform.sizeDelta = new Vector2(250, 100);
            buttonGameObject.AddComponent<CanvasRenderer>();
            Button buttonComponent = buttonGameObject.AddComponent<Button>();

            // Set button's target graphic
            Image buttonImage = buttonGameObject.AddComponent<Image>();
            buttonImage.color = Color.white;
            buttonComponent.targetGraphic = buttonImage;

            // Create Text for the Button
            GameObject textGameObject = new GameObject("Text");
            RectTransform textRectTransform = textGameObject.AddComponent<RectTransform>();
            textRectTransform.sizeDelta = new Vector2(0, 0);
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.SetParent(buttonGameObject.transform, false);

            Text textComponent = textGameObject.AddComponent<Text>();
            textComponent.text = buttonText;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 25;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleCenter;

            // Add onClick listener
            if (onClickAction != null)
            {
                buttonComponent.onClick.AddListener(onClickAction);
            }

            // Set button's position
            buttonRectTransform.SetParent(parent, false);
            buttonRectTransform.anchoredPosition = position;

            return buttonComponent;
        }

        static void SuperJumpButton()
        {
            superJumpEnabled = !superJumpEnabled;
            Text superJumpButtonText = superJumpButton.GetComponentInChildren<Text>();
            if (superJumpEnabled)
            {
                superJumpButtonText.text = "Super Jump : ON";
            }
            else
            {
                superJumpButtonText.text = "Super Jump : OFF";
            }
        }

        static void SuperSpeedButton()
        {
            superSpeedEnabled = !superSpeedEnabled;
            Text superSpeedButtonText = superSpeedButton.GetComponentInChildren<Text>();
            if (superSpeedEnabled)
            {
                superSpeedButtonText.text = "Super Speed : ON";
            }
            else
            {
                superSpeedButtonText.text = "Super Speed : OFF";
            }
        }

        static void SuperPunchButton()
        {
            superPunchEnabled = !superPunchEnabled;
            Text superPunchButtonText = superPunchButton.GetComponentInChildren<Text>();
            if (superPunchEnabled)
            {
                superPunchButtonText.text = "Super Punch : ON";
            }
            else
            {
                superPunchButtonText.text = "Super Punch : OFF";
            }
        }

        static void InvincibleButton()
        {
            invincibilityEnabled = !invincibilityEnabled;

            Text invincibilityButtonText = invincibilityButton.GetComponentInChildren<Text>();
            if (invincibilityEnabled)
            {
                invincibilityButtonText.text = "Invincibility : ON";
            }
            else
            {
                invincibilityButtonText.text = "Invincibility : OFF";
            }
        }


    }
}
