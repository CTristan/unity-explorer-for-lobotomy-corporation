using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorerForLobotomyCorporation.UnityExplorer.Config;
using UnityExplorerForLobotomyCorporation.UniverseLib;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Models;
using UnityExplorerForLobotomyCorporation.UniverseLib.UI.Widgets.ScrollView;
using UnityExplorerForLobotomyCorporation.UniverseLib.Utility;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.UI.Panels
{
    public class LogPanel : UEPanel, ICellPoolDataSource<ConsoleLogCell>
    {
        private static readonly List<LogInfo> Logs = new List<LogInfo>();
        private static string CurrentStreamPath;

        private static ScrollPool<ConsoleLogCell> logScrollPool;

        private static bool DoneScrollPoolInit;

        // Cell pool

        private static readonly Dictionary<LogType, Color> logColors = new Dictionary<LogType, Color>
        {
            {
                LogType.Log, Color.white
            },
            {
                LogType.Warning, Color.yellow
            },
            {
                LogType.Assert, Color.yellow
            },
            {
                LogType.Error, Color.red
            },
            {
                LogType.Exception, Color.red
            },
        };

        private readonly Color logEvenColor = new Color(0.34f, 0.34f, 0.34f);
        private readonly Color logOddColor = new Color(0.28f, 0.28f, 0.28f);

        public LogPanel(UIBase owner) : base(owner)
        {
            SetupIO();
        }

        public override string Name =>
            "Log";

        public override UIManager.Panels PanelType =>
            UIManager.Panels.ConsoleLog;

        public override int MinWidth =>
            350;

        public override int MinHeight =>
            75;

        public override Vector2 DefaultAnchorMin =>
            new Vector2(0.5f, 0.03f);

        public override Vector2 DefaultAnchorMax =>
            new Vector2(0.9f, 0.2f);

        public override bool ShouldSaveActiveState =>
            true;

        public override bool ShowByDefault =>
            true;

        public int ItemCount =>
            Logs.Count;

        public void OnCellBorrowed(ConsoleLogCell cell)
        {
        }

        public void SetCell(ConsoleLogCell cell,
            int index)
        {
            if (index >= Logs.Count)
            {
                cell.Disable();

                return;
            }

            // Logs are displayed in reverse order (newest at top)
            index = Logs.Count - index - 1;

            var log = Logs[index];
            cell.IndexLabel.text = $"{index}:";
            cell.Input.Text = log.message;
            cell.Input.Component.textComponent.color = logColors[log.type];

            var color = index % 2 == 0 ? logEvenColor : logOddColor;
            RuntimeHelper.SetColorBlock(cell.Input.Component, color);
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);

            if (active && !DoneScrollPoolInit)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
                logScrollPool.Initialize(this);
                DoneScrollPoolInit = true;
            }

            logScrollPool.Refresh(true);
        }

        private void SetupIO()
        {
            var fileName = $"UnityExplorer {DateTime.Now:u}.txt";
            fileName = IOUtility.EnsureValidFilename(fileName);
            var path = Path.Combine(ExplorerCore.ExplorerFolder, "Logs");
            CurrentStreamPath = IOUtility.EnsureValidFilePath(Path.Combine(path, fileName));

            // clean old log(s)
            var files = Directory.GetFiles(path);
            if (files.Length >= 10)
            {
                var sorted = files.ToList();
                // sort by 'datetime.ToString("u")' will put the oldest ones first
                sorted.Sort();
                for (var i = 0; i < files.Length - 9; i++) File.Delete(files[i]);
            }

            File.WriteAllLines(CurrentStreamPath, Logs.Select(it => it.message).ToArray());
        }

        // Logging

        public static void Log(string message,
            LogType type)
        {
            Logs.Add(new LogInfo(message, type));

            if (CurrentStreamPath != null)
            {
                File.AppendAllText(CurrentStreamPath, '\n' + message);
            }

            if (logScrollPool != null)
            {
                logScrollPool.Refresh(true);
            }
        }

        private static void ClearLogs()
        {
            Logs.Clear();
            logScrollPool.Refresh(true, true);
        }

        private static void OpenLogFile()
        {
            if (File.Exists(CurrentStreamPath))
            {
                Process.Start(CurrentStreamPath);
            }
        }

        // UI Construction

        protected override void ConstructPanelContent()
        {
            // Log scroll pool

            logScrollPool = UIFactory.CreateScrollPool<ConsoleLogCell>(ContentRoot, "Logs", out var scrollObj, out var scrollContent, new Color(0.03f, 0.03f, 0.03f));
            UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);

            // Buttons and toggles

            var optionsRow = UIFactory.CreateUIObject("OptionsRow", ContentRoot);
            UIFactory.SetLayoutElement(optionsRow, minHeight: 25, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(optionsRow, false, false, true, true, 5, 2, 2, 2, 2);

            var clearButton = UIFactory.CreateButton(optionsRow, "ClearButton", "Clear", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(clearButton.Component.gameObject, minHeight: 23, flexibleHeight: 0, minWidth: 60);
            clearButton.OnClick += ClearLogs;
            clearButton.Component.transform.SetSiblingIndex(1);

            var fileButton = UIFactory.CreateButton(optionsRow, "FileButton", "Open Log File", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(fileButton.Component.gameObject, minHeight: 23, flexibleHeight: 0, minWidth: 100);
            fileButton.OnClick += OpenLogFile;
            fileButton.Component.transform.SetSiblingIndex(2);

            var unityToggle = UIFactory.CreateToggle(optionsRow, "UnityLogToggle", out var toggle, out var toggleText);
            UIFactory.SetLayoutElement(unityToggle, minHeight: 25, minWidth: 150);
            toggleText.text = "Log Unity Debug?";
            toggle.isOn = ConfigManager.Log_Unity_Debug.Value;
            ConfigManager.Log_Unity_Debug.OnValueChanged += val => toggle.isOn = val;
            toggle.onValueChanged.AddListener(val => ConfigManager.Log_Unity_Debug.Value = val);
        }

        public struct LogInfo
        {
            public string message;
            public LogType type;

            public LogInfo(string message,
                LogType type)
            {
                this.message = message;
                this.type = type;
            }
        }
    }

    #region Log Cell View

    public class ConsoleLogCell : ICell
    {
        public Text IndexLabel;
        public InputFieldRef Input;

        public RectTransform Rect { get; set; }
        public GameObject UIRoot { get; set; }

        public float DefaultHeight =>
            25;

        public bool Enabled =>
            UIRoot.activeInHierarchy;

        public void Enable()
        {
            UIRoot.SetActive(true);
        }

        public void Disable()
        {
            UIRoot.SetActive(false);
        }


        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("LogCell", parent, new Vector2(25, 25));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 3);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, minWidth: 50, flexibleWidth: 9999);

            IndexLabel = UIFactory.CreateLabel(UIRoot, "IndexLabel", "i:", TextAnchor.MiddleCenter, Color.grey, false, 12);
            UIFactory.SetLayoutElement(IndexLabel.gameObject, minHeight: 25, minWidth: 30, flexibleWidth: 40);

            Input = UIFactory.CreateInputField(UIRoot, "Input", "");
            //Input.Component.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(Input.UIRoot, minHeight: 25, flexibleWidth: 9999);
            RuntimeHelper.SetColorBlock(Input.Component, new Color(0.1f, 0.1f, 0.1f), new Color(0.13f, 0.13f, 0.13f), new Color(0.07f, 0.07f, 0.07f));
            Input.Component.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            Input.Component.readOnly = true;
            Input.Component.textComponent.supportRichText = true;
            Input.Component.lineType = InputField.LineType.MultiLineNewline;
            Input.Component.textComponent.font = UniversalUI.ConsoleFont;
            Input.PlaceholderText.font = UniversalUI.ConsoleFont;

            return UIRoot;
        }
    }

    #endregion
}
