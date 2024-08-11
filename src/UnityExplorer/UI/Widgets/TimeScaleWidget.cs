using Harmony;
using UnityEngine;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Widgets
{
    internal class TimeScaleWidget
    {
        private static TimeScaleWidget Instance;
        private float desiredTime;

        private ButtonRef lockBtn;
        private bool locked;
        private bool settingTimeScale;
        private InputFieldRef timeInput;

        public TimeScaleWidget(GameObject parent)
    {
        Instance = this;

        ConstructUI(parent);

        InitPatch();
    }

        public void Update()
    {
        // Fallback in case Time.timeScale patch failed for whatever reason
        if (locked)
        {
            SetTimeScale(desiredTime);
        }

        if (!timeInput.Component.isFocused)
        {
            timeInput.Text = Time.timeScale.ToString("F2");
        }
    }

        private void SetTimeScale(float time)
    {
        settingTimeScale = true;
        Time.timeScale = time;
        settingTimeScale = false;
    }

        // UI event listeners

        private void OnTimeInputEndEdit(string val)
    {
        if (float.TryParse(val, out var f))
        {
            SetTimeScale(f);
            desiredTime = f;
        }
    }

        private void OnPauseButtonClicked()
    {
        OnTimeInputEndEdit(timeInput.Text);

        locked = !locked;

        var color = locked ? new Color(0.3f, 0.3f, 0.2f) : new Color(0.2f, 0.2f, 0.2f);
        RuntimeHelper.SetColorBlock(lockBtn.Component, color, color * 1.2f, color * 0.7f);
        lockBtn.ButtonText.text = locked ? "Unlock" : "Lock";
    }

        // UI Construction

        private void ConstructUI(GameObject parent)
    {
        var timeLabel = UIFactory.CreateLabel(parent, "TimeLabel", "Time:", TextAnchor.MiddleRight, Color.grey);
        UIFactory.SetLayoutElement(timeLabel.gameObject, minHeight: 25, minWidth: 35);

        timeInput = UIFactory.CreateInputField(parent, "TimeInput", "timeScale");
        UIFactory.SetLayoutElement(timeInput.Component.gameObject, minHeight: 25, minWidth: 40);
        timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

        timeInput.Text = string.Empty;
        timeInput.Text = Time.timeScale.ToString();

        lockBtn = UIFactory.CreateButton(parent, "PauseButton", "Lock", new Color(0.2f, 0.2f, 0.2f));
        UIFactory.SetLayoutElement(lockBtn.Component.gameObject, minHeight: 25, minWidth: 50);
        lockBtn.OnClick += OnPauseButtonClicked;
    }

        // Only allow Time.timeScale to be set if the user hasn't "locked" it or if we are setting the value internally.

        private static void InitPatch()
    {
        try
        {
            var target = typeof(Time).GetProperty("timeScale").GetSetMethod();
            ExplorerCore.Harmony.Patch(target, new HarmonyMethod(typeof(TimeScaleWidget), nameof(Prefix_Time_set_timeScale)), null);
        }
        catch
        {
        }
    }

        private static bool Prefix_Time_set_timeScale()
    {
        return !Instance.locked || Instance.settingTimeScale;
    }
    }
}
