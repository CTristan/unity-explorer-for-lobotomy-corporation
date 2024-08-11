using System;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Inspectors;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.Input;
using UnityExplorerForLobotomyCorporation.UniverseLib.Reflection;
using UnityExplorerForLobotomyCorporation.UniverseLib.Runtime.Mono;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;
using Object = UnityEngine.Object;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    internal class FreeCamPanel : UEPanel
    {
        internal static bool inFreeCamMode;
        internal static bool usingGameCamera;
        internal static Camera ourCamera;
        internal static Camera lastMainCamera;
        internal static FreeCamBehaviour freeCamScript;

        internal static float desiredMoveSpeed = 10f;

        internal static Vector3 originalCameraPosition;
        internal static Quaternion originalCameraRotation;

        internal static Vector3? currentUserCameraPosition;
        internal static Quaternion? currentUserCameraRotation;

        internal static Vector3 previousMousePosition;

        internal static Vector3 lastSetCameraPosition;

        private static ButtonRef startStopButton;
        private static Toggle useGameCameraToggle;
        private static InputFieldRef positionInput;
        private static InputFieldRef moveSpeedInput;
        private static ButtonRef inspectButton;

        public FreeCamPanel(UIBase owner) : base(owner)
        {
        }

        public override string Name =>
            "Freecam";

        public override UIManager.Panels PanelType =>
            UIManager.Panels.Freecam;

        public override int MinWidth =>
            400;

        public override int MinHeight =>
            320;

        public override Vector2 DefaultAnchorMin =>
            new Vector2(0.4f, 0.4f);

        public override Vector2 DefaultAnchorMax =>
            new Vector2(0.6f, 0.6f);

        public override bool NavButtonWanted =>
            true;

        public override bool ShouldSaveActiveState =>
            true;

        internal static void BeginFreecam()
        {
            inFreeCamMode = true;

            previousMousePosition = InputManager.MousePosition;

            CacheMainCamera();
            SetupFreeCamera();

            inspectButton.GameObject.SetActive(true);
        }

        private static void CacheMainCamera()
        {
            var currentMain = Camera.main;
            if (currentMain)
            {
                lastMainCamera = currentMain;
                originalCameraPosition = currentMain.transform.position;
                originalCameraRotation = currentMain.transform.rotation;

                if (currentUserCameraPosition == null)
                {
                    currentUserCameraPosition = currentMain.transform.position;
                    currentUserCameraRotation = currentMain.transform.rotation;
                }
            }
            else
            {
                originalCameraRotation = Quaternion.identity;
            }
        }

        private static void SetupFreeCamera()
        {
            if (useGameCameraToggle.isOn)
            {
                if (!lastMainCamera)
                {
                    ExplorerCore.LogWarning("There is no previous Camera found, reverting to default Free Cam.");
                    useGameCameraToggle.isOn = false;
                }
                else
                {
                    usingGameCamera = true;
                    ourCamera = lastMainCamera;
                }
            }

            if (!useGameCameraToggle.isOn)
            {
                usingGameCamera = false;

                if (lastMainCamera)
                {
                    lastMainCamera.enabled = false;
                }
            }

            if (!ourCamera)
            {
                ourCamera = new GameObject("UE_Freecam").AddComponent<Camera>();
                ourCamera.gameObject.tag = "MainCamera";
                Object.DontDestroyOnLoad(ourCamera.gameObject);
                ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            if (!freeCamScript)
            {
                freeCamScript = ourCamera.gameObject.AddComponent<FreeCamBehaviour>();
            }

            ourCamera.transform.position = (Vector3)currentUserCameraPosition;
            ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;

            ourCamera.gameObject.SetActive(true);
            ourCamera.enabled = true;
        }

        internal static void EndFreecam()
        {
            inFreeCamMode = false;

            if (usingGameCamera)
            {
                ourCamera = null;

                if (lastMainCamera)
                {
                    lastMainCamera.transform.position = originalCameraPosition;
                    lastMainCamera.transform.rotation = originalCameraRotation;
                }
            }

            if (ourCamera)
            {
                ourCamera.gameObject.SetActive(false);
            }
            else
            {
                inspectButton.GameObject.SetActive(false);
            }

            if (freeCamScript)
            {
                Object.Destroy(freeCamScript);
                freeCamScript = null;
            }

            if (lastMainCamera)
            {
                lastMainCamera.enabled = true;
            }
        }

        private static void SetCameraPosition(Vector3 pos)
        {
            if (!ourCamera || lastSetCameraPosition == pos)
            {
                return;
            }

            ourCamera.transform.position = pos;
            lastSetCameraPosition = pos;
        }

        internal static void UpdatePositionInput()
        {
            if (!ourCamera)
            {
                return;
            }

            if (positionInput.Component.isFocused)
            {
                return;
            }

            lastSetCameraPosition = ourCamera.transform.position;
            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(lastSetCameraPosition);
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {
            startStopButton = UIFactory.CreateButton(ContentRoot, "ToggleButton", "Freecam");
            UIFactory.SetLayoutElement(startStopButton.GameObject, 150, 25, 9999);
            startStopButton.OnClick += StartStopButton_OnClick;
            SetToggleButtonState();

            AddSpacer(5);

            var toggleObj = UIFactory.CreateToggle(ContentRoot, "UseGameCameraToggle", out useGameCameraToggle, out var toggleText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, flexibleWidth: 9999);
            useGameCameraToggle.onValueChanged.AddListener(OnUseGameCameraToggled);
            useGameCameraToggle.isOn = false;
            toggleText.text = "Use Game Camera?";

            AddSpacer(5);

            var posRow = AddInputField("Position", "Freecam Pos:", "eg. 0 0 0", out positionInput, PositionInput_OnEndEdit);

            var resetPosButton = UIFactory.CreateButton(posRow, "ResetButton", "Reset");
            UIFactory.SetLayoutElement(resetPosButton.GameObject, 70, 25);
            resetPosButton.OnClick += OnResetPosButtonClicked;

            AddSpacer(5);

            AddInputField("MoveSpeed", "Move Speed:", "Default: 1", out moveSpeedInput, MoveSpeedInput_OnEndEdit);
            moveSpeedInput.Text = desiredMoveSpeed.ToString();

            AddSpacer(5);

            var instructions = @"Controls:
- WASD / Arrows: Movement
- Space / PgUp: Move up
- LeftCtrl / PgDown: Move down
- Right Mouse Button: Free look
- Shift: Super speed";

            var instructionsText = UIFactory.CreateLabel(ContentRoot, "Instructions", instructions, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(instructionsText.gameObject, flexibleWidth: 9999, flexibleHeight: 9999);

            AddSpacer(5);

            inspectButton = UIFactory.CreateButton(ContentRoot, "InspectButton", "Inspect Free Camera");
            UIFactory.SetLayoutElement(inspectButton.GameObject, flexibleWidth: 9999, minHeight: 25);
            inspectButton.OnClick += () =>
            {
                InspectorManager.Inspect(ourCamera);
            };

            inspectButton.GameObject.SetActive(false);

            AddSpacer(5);
        }

        private void AddSpacer(int height)
        {
            var obj = UIFactory.CreateUIObject("Spacer", ContentRoot);
            UIFactory.SetLayoutElement(obj, minHeight: height, flexibleHeight: 0);
        }

        private GameObject AddInputField(string name,
            string labelText,
            string placeHolder,
            out InputFieldRef inputField,
            Action<string> onInputEndEdit)
        {
            var row = UIFactory.CreateHorizontalGroup(ContentRoot, $"{name}_Group", false, false, true, true, 3, default, new Color(1, 1, 1, 0));

            var posLabel = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, 100, 25);

            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, 125, 25, 9999);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return row;
        }

        private void StartStopButton_OnClick()
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (inFreeCamMode)
            {
                EndFreecam();
            }
            else
            {
                BeginFreecam();
            }

            SetToggleButtonState();
        }

        private void SetToggleButtonState()
        {
            if (inFreeCamMode)
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new Color(0.4f, 0.2f, 0.2f));
                startStopButton.ButtonText.text = "End Freecam";
            }
            else
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new Color(0.2f, 0.4f, 0.2f));
                startStopButton.ButtonText.text = "Begin Freecam";
            }
        }

        private void OnUseGameCameraToggled(bool value)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!inFreeCamMode)
            {
                return;
            }

            EndFreecam();
            BeginFreecam();
        }

        private void OnResetPosButtonClicked()
        {
            currentUserCameraPosition = originalCameraPosition;
            currentUserCameraRotation = originalCameraRotation;

            if (inFreeCamMode && ourCamera)
            {
                ourCamera.transform.position = (Vector3)currentUserCameraPosition;
                ourCamera.transform.rotation = (Quaternion)currentUserCameraRotation;
            }

            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(originalCameraPosition);
        }

        private void PositionInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out Vector3 parsed, out var parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse position to Vector3: {parseEx.ReflectionExToString()}");
                UpdatePositionInput();

                return;
            }

            SetCameraPosition(parsed);
        }

        private void MoveSpeedInput_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out var parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                moveSpeedInput.Text = desiredMoveSpeed.ToString();

                return;
            }

            desiredMoveSpeed = parsed;
        }
    }

    internal class FreeCamBehaviour : MonoBehaviour
    {
        internal void Update()
        {
            if (FreeCamPanel.inFreeCamMode)
            {
                if (!FreeCamPanel.ourCamera)
                {
                    FreeCamPanel.EndFreecam();

                    return;
                }

                var transform = FreeCamPanel.ourCamera.transform;

                FreeCamPanel.currentUserCameraPosition = transform.position;
                FreeCamPanel.currentUserCameraRotation = transform.rotation;

                var moveSpeed = FreeCamPanel.desiredMoveSpeed * Time.deltaTime;

                if (InputManager.GetKey(KeyCode.LeftShift) || InputManager.GetKey(KeyCode.RightShift))
                {
                    moveSpeed *= 10f;
                }

                if (InputManager.GetKey(KeyCode.LeftArrow) || InputManager.GetKey(KeyCode.A))
                {
                    transform.position += transform.right * -1 * moveSpeed;
                }

                if (InputManager.GetKey(KeyCode.RightArrow) || InputManager.GetKey(KeyCode.D))
                {
                    transform.position += transform.right * moveSpeed;
                }

                if (InputManager.GetKey(KeyCode.UpArrow) || InputManager.GetKey(KeyCode.W))
                {
                    transform.position += transform.forward * moveSpeed;
                }

                if (InputManager.GetKey(KeyCode.DownArrow) || InputManager.GetKey(KeyCode.S))
                {
                    transform.position += transform.forward * -1 * moveSpeed;
                }

                if (InputManager.GetKey(KeyCode.Space) || InputManager.GetKey(KeyCode.PageUp))
                {
                    transform.position += transform.up * moveSpeed;
                }

                if (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.PageDown))
                {
                    transform.position += transform.up * -1 * moveSpeed;
                }

                if (InputManager.GetMouseButton(1))
                {
                    var mouseDelta = InputManager.MousePosition - FreeCamPanel.previousMousePosition;

                    var newRotationX = transform.localEulerAngles.y + mouseDelta.x * 0.3f;
                    var newRotationY = transform.localEulerAngles.x - mouseDelta.y * 0.3f;
                    transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
                }

                FreeCamPanel.UpdatePositionInput();

                FreeCamPanel.previousMousePosition = InputManager.MousePosition;
            }
        }
    }
}
