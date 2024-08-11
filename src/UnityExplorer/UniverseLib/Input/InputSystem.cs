using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UniverseLib.Input
{
    public class InputSystem : IHandleInput
    {
        public InputSystem()
        {
            SetupSupportedDevices();

            p_kbCurrent = TKeyboard.GetProperty("current");
            p_kbIndexer = TKeyboard.GetProperty("Item", new[]
            {
                TKey,
            });

            var t_btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            p_btnIsPressed = t_btnControl.GetProperty("isPressed");
            p_btnWasPressed = t_btnControl.GetProperty("wasPressedThisFrame");
            p_btnWasReleased = t_btnControl.GetProperty("wasReleasedThisFrame");

            p_mouseCurrent = TMouse.GetProperty("current");
            p_leftButton = TMouse.GetProperty("leftButton");
            p_rightButton = TMouse.GetProperty("rightButton");
            p_middleButton = TMouse.GetProperty("middleButton");
            p_backButton = TMouse.GetProperty("backButton");
            p_forwardButton = TMouse.GetProperty("forwardButton");
            p_scrollDelta = TMouse.GetProperty("scroll");

            p_position = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Pointer").GetProperty("position");

            m_ReadV2Control = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl`1").MakeGenericType(typeof(Vector2)).GetMethod("ReadValue");
        }

        // Input API

        public Vector2 MousePosition
        {
            get
            {
                try
                {
                    return (Vector2)m_ReadV2Control.Invoke(MousePositionInfo, ArgumentUtility.EmptyArgs);
                }
                catch
                {
                    return default;
                }
            }
        }

        public Vector2 MouseScrollDelta
        {
            get
            {
                try
                {
                    return (Vector2)m_ReadV2Control.Invoke(MouseScrollInfo, ArgumentUtility.EmptyArgs);
                }
                catch
                {
                    return default;
                }
            }
        }

        public bool GetMouseButtonDown(int btn)
        {
            try
            {
                return (bool)p_btnWasPressed.GetValue(GetMouseButtonObject(btn), null);
            }
            catch
            {
                return false;
            }
        }

        public bool GetMouseButton(int btn)
        {
            try
            {
                return (bool)p_btnIsPressed.GetValue(GetMouseButtonObject(btn), null);
            }
            catch
            {
                return false;
            }
        }

        public bool GetMouseButtonUp(int btn)
        {
            try
            {
                return (bool)p_btnWasReleased.GetValue(GetMouseButtonObject(btn), null);
            }
            catch
            {
                return false;
            }
        }

        public bool GetKeyDown(KeyCode key)
        {
            try
            {
                var actual = KeyCodeToActualKey(key);

                return (bool)p_btnWasPressed.GetValue(actual, null);
            }
            catch
            {
                return false;
            }
        }

        public bool GetKey(KeyCode key)
        {
            try
            {
                return (bool)p_btnIsPressed.GetValue(KeyCodeToActualKey(key), null);
            }
            catch
            {
                return false;
            }
        }

        public bool GetKeyUp(KeyCode key)
        {
            try
            {
                return (bool)p_btnWasReleased.GetValue(KeyCodeToActualKey(key), null);
            }
            catch
            {
                return false;
            }
        }

        // InputSystem has no equivalent API for "ResetInputAxes".

        public void ResetInputAxes()
        {
        }

        // UI Input

        public void AddUIInputModule()
        {
            if (TInputSystemUIInputModule == null)
            {
                Universe.LogWarning("Unable to find UI Input Module Type, Input will not work!");

                return;
            }

            var assetType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionAsset");
            newInputModule = RuntimeHelper.AddComponent<BaseInputModule>(UniversalUI.CanvasRoot, TInputSystemUIInputModule);
            var asset = RuntimeHelper.CreateScriptable(assetType).TryCast(assetType);

            t_InputExtensions = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionSetupExtensions");

            var addMap = t_InputExtensions.GetMethod("AddActionMap", new[]
            {
                assetType, typeof(string),
            });

            var map = addMap.Invoke(null, new[]
            {
                asset, "UI",
            }).TryCast(ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionMap"));

            CreateAction(map, "point", new[]
            {
                "<Mouse>/position",
            }, "point");

            CreateAction(map, "click", new[]
            {
                "<Mouse>/leftButton",
            }, "leftClick");

            CreateAction(map, "rightClick", new[]
            {
                "<Mouse>/rightButton",
            }, "rightClick");

            CreateAction(map, "scrollWheel", new[]
            {
                "<Mouse>/scroll",
            }, "scrollWheel");

            m_UI_Enable = map.GetType().GetMethod("Enable");
            m_UI_Enable.Invoke(map, ArgumentUtility.EmptyArgs);
            UIActionMap = map;

            p_actionsAsset = TInputSystemUIInputModule.GetProperty("actionsAsset");
        }

        public void ActivateModule()
        {
            try
            {
                var newInput = (BaseInputModule)newInputModule.TryCast(TInputSystemUIInputModule);
                newInput.m_EventSystem = UniversalUI.EventSys;
                newInput.ActivateModule();
                m_UI_Enable.Invoke(UIActionMap, ArgumentUtility.EmptyArgs);

                // if the actionsAsset is null, call the AssignDefaultActions method.
                if (p_actionsAsset.GetValue(newInput.TryCast(p_actionsAsset.DeclaringType), null) == null)
                {
                    var assignDefaultMethod = newInput.GetType().GetMethod("AssignDefaultActions");
                    if (assignDefaultMethod != null)
                    {
                        assignDefaultMethod.Invoke(newInput.TryCast(assignDefaultMethod.DeclaringType), new object[0]);
                    }
                    else
                    {
                        Universe.Log("AssignDefaultActions method is null!");
                    }
                }
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception enabling InputSystem UI Input Module: " + ex);
            }
        }

        internal static void SetupSupportedDevices()
        {
            try
            {
                // typeof(InputSystem)
                var t_InputSystem = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputSystem");
                // InputSystem.settings
                var settings = t_InputSystem.GetProperty("settings", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                // typeof(InputSettings)
                var t_Settings = settings.GetActualType();
                // InputSettings.supportedDevices
                var supportedProp = t_Settings.GetProperty("supportedDevices", BindingFlags.Public | BindingFlags.Instance);
                var supportedDevices = supportedProp.GetValue(settings, null);
                // An empty supportedDevices list means all devices are supported.
                supportedProp.SetValue(settings, Activator.CreateInstance(supportedDevices.GetActualType(), new object[]
                {
                    new string[0],
                }), null);
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception setting up InputSystem.settings.supportedDevices list!");
                Universe.Log(ex);
            }
        }

        private static object GetMouseButtonObject(int btn)
        {
            if (btn == 0)
            {
                return LeftMouseButton;
            }

            if (btn == 1)
            {
                return RightMouseButton;
            }

            if (btn == 2)
            {
                return MiddleMouseButton;
            }

            if (btn == 3)
            {
                return BackMouseButton;
            }

            if (btn == 4)
            {
                return ForwardMouseButton;
            }

            throw new NotImplementedException();
        }

        private void CreateAction(object map,
            string actionName,
            string[] bindings,
            string propertyName)
        {
            var disable = map.GetType().GetMethod("Disable");
            disable.Invoke(map, ArgumentUtility.EmptyArgs);

            var inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction");
            var addAction = t_InputExtensions.GetMethod("AddAction");
            var action = addAction.Invoke(null, new[]
            {
                map, actionName, default, null, null, null, null, null,
            }).TryCast(inputActionType);

            var addBinding = t_InputExtensions.GetMethod("AddBinding", new[]
            {
                inputActionType, typeof(string), typeof(string), typeof(string), typeof(string),
            });

            foreach (var binding in bindings)
            {
                addBinding.Invoke(null, new[]
                {
                    action.TryCast(inputActionType), binding, null, null, null,
                });
            }

            var refType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionReference");
            var inputRef = refType.GetMethod("Create").Invoke(null, new[]
            {
                action,
            }).TryCast(refType);

            TInputSystemUIInputModule.GetProperty(propertyName).SetValue(newInputModule.TryCast(TInputSystemUIInputModule), inputRef, null);
        }

        #region Reflection cache

        // typeof(InputSystem.Keyboard)
        public static Type TKeyboard =>
            t_Keyboard = t_Keyboard ?? ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Keyboard");

        private static Type t_Keyboard;

        // typeof(InputSystem.Mouse)
        public static Type TMouse =>
            t_Mouse = t_Mouse ?? ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse");

        private static Type t_Mouse;

        // typeof (InputSystem.Key)
        public static Type TKey =>
            t_Key = t_Key ?? ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Key");

        private static Type t_Key;

        // InputSystem.Controls.ButtonControl.isPressed
        private static PropertyInfo p_btnIsPressed;

        // InputSystem.Controls.ButtonControl.wasPressedThisFrame
        private static PropertyInfo p_btnWasPressed;

        // InputSystem.Controls.ButtonControl.wasReleasedThisFrame
        private static PropertyInfo p_btnWasReleased;

        // Keyboard.current
        private static object CurrentKeyboard =>
            p_kbCurrent.GetValue(null, null);

        private static PropertyInfo p_kbCurrent;

        // Keyboard.this[Key]
        private static PropertyInfo p_kbIndexer;

        // Mouse.current
        private static object CurrentMouse =>
            p_mouseCurrent.GetValue(null, null);

        private static PropertyInfo p_mouseCurrent;

        // Mouse.current.leftButton
        private static object LeftMouseButton =>
            p_leftButton.GetValue(CurrentMouse, null);

        private static PropertyInfo p_leftButton;

        // Mouse.current.rightButton
        private static object RightMouseButton =>
            p_rightButton.GetValue(CurrentMouse, null);

        private static PropertyInfo p_rightButton;

        // Mouse.current.middleButton
        private static object MiddleMouseButton =>
            p_middleButton.GetValue(CurrentMouse, null);

        private static PropertyInfo p_middleButton;

        // Mouse.current.forwardButton
        private static object ForwardMouseButton =>
            p_forwardButton.GetValue(CurrentMouse, null);

        private static PropertyInfo p_forwardButton;

        // Mouse.current.backButton
        private static object BackMouseButton =>
            p_backButton.GetValue(CurrentMouse, null);

        private static PropertyInfo p_backButton;

        // InputSystem.InputControl<Vector2>.ReadValue()
        private static MethodInfo m_ReadV2Control;

        // Mouse.current.position
        private static object MousePositionInfo =>
            p_position.GetValue(CurrentMouse, null);

        private static PropertyInfo p_position;

        // Mouse.current.scroll
        private static object MouseScrollInfo =>
            p_scrollDelta.GetValue(CurrentMouse, null);

        private static PropertyInfo p_scrollDelta;

        // typeof(InputSystem.UI.InputSystemUIInputModule)
        public Type TInputSystemUIInputModule =>
            t_UIInputModule = t_UIInputModule ?? ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");

        internal Type t_UIInputModule;

        // Our UI input module
        public BaseInputModule UIInputModule =>
            newInputModule;

        internal BaseInputModule newInputModule;

        // UI input action maps
        private Type t_InputExtensions;
        private object UIActionMap;
        private MethodInfo m_UI_Enable;
        private PropertyInfo p_actionsAsset;

        #endregion

        #region KeyCode <-> Key Helpers

        public static Dictionary<KeyCode, object> KeyCodeToKeyDict = new Dictionary<KeyCode, object>();
        public static Dictionary<KeyCode, object> KeyCodeToKeyEnumDict = new Dictionary<KeyCode, object>();

        internal static Dictionary<string, string> keycodeToKeyFixes = new Dictionary<string, string>
        {
            {
                "Control", "Ctrl"
            },
            {
                "Return", "Enter"
            },
            {
                "Alpha", "Digit"
            },
            {
                "Keypad", "Numpad"
            },
            {
                "Numlock", "NumLock"
            },
            {
                "Print", "PrintScreen"
            },
            {
                "BackQuote", "Backquote"
            },
        };

        public static object KeyCodeToActualKey(KeyCode key)
        {
            if (!KeyCodeToKeyDict.ContainsKey(key))
            {
                try
                {
                    var parsed = KeyCodeToKeyEnum(key);
                    var actualKey = p_kbIndexer.GetValue(CurrentKeyboard, new[]
                    {
                        parsed,
                    });

                    KeyCodeToKeyDict.Add(key, actualKey);
                }
                catch
                {
                    KeyCodeToKeyDict.Add(key, default);
                }
            }

            return KeyCodeToKeyDict[key];
        }

        public static object KeyCodeToKeyEnum(KeyCode key)
        {
            if (!KeyCodeToKeyEnumDict.ContainsKey(key))
            {
                var s = key.ToString();
                try
                {
                    if (keycodeToKeyFixes.First(it => s.Contains(it.Key)) is KeyValuePair<string, string> entry)
                    {
                        s = s.Replace(entry.Key, entry.Value);
                    }
                }
                catch
                {
                    /* suppressed */
                }

                try
                {
                    var parsed = Enum.Parse(TKey, s);
                    KeyCodeToKeyEnumDict.Add(key, parsed);
                }
                catch (Exception ex)
                {
                    Universe.Log(ex);
                    KeyCodeToKeyEnumDict.Add(key, default);
                }
            }

            return KeyCodeToKeyEnumDict[key];
        }

        #endregion
    }
}
