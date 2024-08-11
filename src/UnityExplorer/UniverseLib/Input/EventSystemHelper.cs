using System.Collections.Generic;
using Harmony;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorerForLobotomyCorporation.UniverseLib.Config;
using UnityExplorerForLobotomyCorporation.UniverseLib.Enums;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Runtime;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.Input
{
    public static class EventSystemHelper
    {
        internal static EventSystem lastEventSystem;
        private static BaseInputModule lastInputModule;
        private static bool settingEventSystem;
        private static float timeOfLastEventSystemSearch;

        private static readonly AmbiguousMemberHandler<EventSystem, EventSystem> EventSystemCurrent_Handler = new AmbiguousMemberHandler<EventSystem, EventSystem>(true, true, "current", "main");

        private static bool usingEventSystemDictionaryMembers;

        private static readonly AmbiguousMemberHandler<EventSystem, GameObject> m_CurrentSelected_Handler_Normal =
            new AmbiguousMemberHandler<EventSystem, GameObject>(true, true, "m_CurrentSelected", "m_currentSelected");

        private static readonly AmbiguousMemberHandler<EventSystem, bool> m_SelectionGuard_Handler_Normal =
            new AmbiguousMemberHandler<EventSystem, bool>(true, true, "m_SelectionGuard", "m_selectionGuard");

        private static readonly AmbiguousMemberHandler<EventSystem, Dictionary<int, GameObject>> m_CurrentSelected_Handler_Dictionary =
            new AmbiguousMemberHandler<EventSystem, Dictionary<int, GameObject>>(true, true, "m_CurrentSelected", "m_currentSelected");

        private static readonly AmbiguousMemberHandler<EventSystem, Dictionary<int, bool>> m_SelectionGuard_Handler_Dictionary =
            new AmbiguousMemberHandler<EventSystem, Dictionary<int, bool>>(true, true, "m_SelectionGuard", "m_selectionGuard");
#if MONO
        private static readonly AmbiguousMemberHandler<EventSystem, List<EventSystem>> m_EventSystems_handler =
            new AmbiguousMemberHandler<EventSystem, List<EventSystem>>(true, true, "m_EventSystems", "m_eventSystems");
#endif
        /// <summary>The value of "EventSystem.current", or "EventSystem.main" in some older games.</summary>
        public static EventSystem CurrentEventSystem
        {
            get =>
                EventSystemCurrent_Handler.GetValue();
            set =>
                EventSystemCurrent_Handler.SetValue(value);
        }

        /// <summary>The current BaseInputModule being used for the UniverseLib UI.</summary>
        public static BaseInputModule UIInput =>
            InputManager.inputHandler.UIInputModule;

        internal static void Init()
        {
            InitPatches();

            usingEventSystemDictionaryMembers = m_CurrentSelected_Handler_Dictionary.member != null;
        }

        /// <summary>Helper to call EventSystem.SetSelectedGameObject and bypass UniverseLib's override patch.</summary>
        public static void SetSelectedGameObject(GameObject obj)
        {
            try
            {
                var system = CurrentEventSystem;
                BaseEventData pointer = new AxisEventData(system);

                GameObject currentSelected;
                if (usingEventSystemDictionaryMembers)
                {
                    currentSelected = m_CurrentSelected_Handler_Dictionary.GetValue(system)[0];
                }
                else
                {
                    currentSelected = m_CurrentSelected_Handler_Normal.GetValue(system);
                }

                ExecuteEvents.Execute(currentSelected, pointer, ExecuteEvents.deselectHandler);

                if (usingEventSystemDictionaryMembers)
                {
                    m_CurrentSelected_Handler_Dictionary.GetValue(system)[0] = obj;
                }
                else
                {
                    m_CurrentSelected_Handler_Normal.SetValue(system, obj);
                }

                ExecuteEvents.Execute(obj, pointer, ExecuteEvents.selectHandler);
            }
            catch //(Exception e)
            {
                //Universe.LogWarning($"Exception setting current selected GameObject: {e}");
            }
        }

        /// <summary>Helper to set the SelectionGuard property on the current EventSystem with safe API.</summary>
        public static void SetSelectionGuard(bool value)
        {
            var system = CurrentEventSystem;

            if (usingEventSystemDictionaryMembers)
            {
                m_SelectionGuard_Handler_Dictionary.GetValue(system)[0] = value;
            }
            else
            {
                m_SelectionGuard_Handler_Normal.SetValue(system, value);
            }
        }

        /// <summary>If the UniverseLib EventSystem is not enabled, this enables it and sets EventSystem.current to it, and stores the previous EventSystem.</summary>
        internal static void EnableEventSystem()
        {
            if (!UniversalUI.EventSys)
            {
                return;
            }

            // Deactivate and store the current EventSystem

            var current = CurrentEventSystem;

            // If it's enabled and it's not the UniverseLib system, store it.
            if (current && !current.ReferenceEqual(UniversalUI.EventSys) && current.isActiveAndEnabled)
            {
                lastEventSystem = current;
                lastInputModule = current.currentInputModule;
                lastEventSystem.enabled = false;
            }
            else if (!lastEventSystem && !ConfigManager.Disable_Fallback_EventSystem_Search && Time.realtimeSinceStartup - timeOfLastEventSystemSearch > 10f)
            {
                FallbackEventSystemSearch();
                if (lastEventSystem)
                {
                    lastEventSystem.enabled = false;
                }
            }

            if (!UniversalUI.EventSys.enabled)
            {
                // Set to our current system
                settingEventSystem = true;
                UniversalUI.EventSys.enabled = true;
                ActivateUIModule();
                CurrentEventSystem = UniversalUI.EventSys;
                settingEventSystem = false;
            }

            CheckVRChatEventSystemFix();
        }

        // In some cases we may need to set our own EventSystem active before the original EventSystem is created or enabled.
        // For that we will need to use Resources to find the other active EventSystem once it has been created.
        private static void FallbackEventSystemSearch()
        {
            timeOfLastEventSystemSearch = Time.realtimeSinceStartup;
            Object[] allSystems = RuntimeHelper.FindObjectsOfTypeAll<EventSystem>();
            foreach (var obj in allSystems)
            {
                var system = obj.TryCast<EventSystem>();
                if (system.ReferenceEqual(UniversalUI.EventSys))
                {
                    continue;
                }

                if (system.isActiveAndEnabled)
                {
                    lastEventSystem = system;
                    lastInputModule = system.currentInputModule;

                    //lastEventSystem.enabled = false;
                    break;
                }
            }
        }

        /// <summary>If the UniverseLib EventSystem is enabled, this disables it and sets EventSystem.current to the previous EventSystem which was enabled.</summary>
        internal static void ReleaseEventSystem()
        {
            if (!UniversalUI.EventSys)
            {
                return;
            }

            CheckVRChatEventSystemFix();

            if (!lastEventSystem && !ConfigManager.Disable_Fallback_EventSystem_Search && Time.realtimeSinceStartup - timeOfLastEventSystemSearch > 10f)
            {
                FallbackEventSystemSearch();
            }

            if (!lastEventSystem)
            {
                //Universe.LogWarning($"No previous EventSystem found to set back to!");
                return;
            }

            settingEventSystem = true;

            UniversalUI.EventSys.enabled = false;
            UniversalUI.EventSys.currentInputModule?.DeactivateModule();

            if (lastEventSystem && lastEventSystem.gameObject.activeSelf)
            {
                if (lastInputModule)
                {
                    lastInputModule.ActivateModule();
                    lastEventSystem.m_CurrentInputModule = lastInputModule;
                }

#if MONO
                if (m_EventSystems_handler.member != null)
                {
                    var list = m_EventSystems_handler.GetValue();
                    if (list != null && !list.Contains(lastEventSystem))
                    {
                        list.Add(lastEventSystem);
                    }
                }
#else
                    if (EventSystem.m_EventSystems != null && !EventSystem.m_EventSystems.Contains(lastEventSystem))
                        EventSystem.m_EventSystems.Add(lastEventSystem);

#endif
                CurrentEventSystem = lastEventSystem;
                lastEventSystem.enabled = true;
            }

            settingEventSystem = false;
        }

        // UI Input Module

        internal static void AddUIModule()
        {
            InputManager.inputHandler.AddUIInputModule();
            ActivateUIModule();
        }

        internal static void ActivateUIModule()
        {
            UniversalUI.EventSys.m_CurrentInputModule = UIInput;
            InputManager.inputHandler.ActivateModule();
        }

        // Dirty fix for some VRChat weirdness

        private static void CheckVRChatEventSystemFix()
        {
            try
            {
                if (Application.productName != "VRChat")
                {
                    return;
                }

                if (!(GameObject.Find("EventSystem") is GameObject strayEventSystem))
                {
                    return;
                }

                // Try to make sure it's the right object I guess

                var count = strayEventSystem.GetComponents<Component>().Length;
                if (count != 3 && count != 4)
                {
                    return;
                }

                if (strayEventSystem.transform.childCount > 0)
                {
                    return;
                }

                Universe.LogWarning("Disabling extra VRChat EventSystem");
                strayEventSystem.SetActive(false);
            }
            catch
            {
            }
        }

        // ~~~~~~~~~~~~ Patches ~~~~~~~~~~~~

        private static void InitPatches()
        {
            Universe.Patch(typeof(EventSystem), new[]
            {
                "current", "main",
            }, MethodType.Setter, prefix: AccessTools.Method(typeof(EventSystemHelper), nameof(Prefix_EventSystem_set_current)));

            Universe.Patch(typeof(EventSystem), "SetSelectedGameObject", MethodType.Normal, new[]
            {
                new[]
                {
                    typeof(GameObject), typeof(BaseEventData), typeof(int),
                },
                new[]
                {
                    typeof(GameObject), typeof(BaseEventData),
                },
            }, AccessTools.Method(typeof(EventSystemHelper), nameof(Prefix_EventSystem_SetSelectedGameObject)));
        }

        // Prevent setting non-UniverseLib objects as selected when a menu is open

        internal static bool Prefix_EventSystem_SetSelectedGameObject(GameObject __0)
        {
            if (ConfigManager.Allow_UI_Selection_Outside_UIBase || !UniversalUI.AnyUIShowing || !UniversalUI.CanvasRoot)
            {
                return true;
            }

            return __0 && __0.transform.root.gameObject.GetInstanceID() == UniversalUI.CanvasRoot.GetInstanceID();
        }

        // Force EventSystem.current to be UniverseLib's when menu is open

        internal static void Prefix_EventSystem_set_current(ref EventSystem value)
        {
            if (!settingEventSystem && value && !value.ReferenceEqual(UniversalUI.EventSys))
            {
                lastEventSystem = value;
                lastInputModule = value.currentInputModule;
            }

            if (!UniversalUI.EventSys)
            {
                return;
            }

            if (!settingEventSystem && CursorUnlocker.ShouldUnlock && !ConfigManager.Disable_EventSystem_Override)
            {
                ActivateUIModule();
                value = UniversalUI.EventSys;
                value.enabled = true;
            }
        }
    }
}
