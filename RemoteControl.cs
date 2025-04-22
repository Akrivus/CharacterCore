using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class RemoteControl : MonoBehaviour
{
    private Dictionary<string, Action> buttonActions = new Dictionary<string, Action>();

    private string pageUpPath;
    private string pageDownPath;

    public void Configure(RemoteControlConfigs c)
    {
        buttonActions = new Dictionary<string, Action>
        {
            { "leftButton", Select },
            { "leftArrow", LeftArrow },
            { "rightArrow", RightArrow },
            { "upArrow", UpArrow },
            { "downArrow", DownArrow },
            { "pageUp", PageUp },
            { "pageDown", PageDown },
            { "rightButton", BackButton },
            { "contextMenu", MenuButton }
        };

        pageUpPath = c.PageUpPath;
        pageDownPath = c.PageDownPath;
        InputSystem.onAnyButtonPress.Call(OnPress);
    }

    private void Awake()
    {
        ConfigManager.Instance.RegisterConfig(typeof(RemoteControlConfigs), "remote_control", (config) => Configure((RemoteControlConfigs)config));
    }

    private void OnPress(InputControl control)
    {
        if (buttonActions.TryGetValue(control.name, out var actionToInvoke))
            actionToInvoke.Invoke();
    }

    private void Launch(string path)
    {
        if (!File.Exists(path))
            return;
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path),
            UseShellExecute = true
        });
        Application.Quit();
    }

    private void Select()
    {
        ChatManager.IsPaused = !ChatManager.IsPaused;
    }

    private void LeftArrow()
    {

    }

    private void RightArrow()
    {

    }

    private void UpArrow()
    {

    }

    private void DownArrow()
    {

    }

    private void PageUp()
    {
        Launch(pageUpPath);
    }

    private void PageDown()
    {
        Launch(pageDownPath);
    }

    private void MenuButton()
    {
        Application.Quit();
    }

    private void BackButton()
    {

    }
}