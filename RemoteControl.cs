using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class RemoteControl : MonoBehaviour
{
    private Dictionary<string, Action> buttonActions;

    private void OnEnable()
    {
        buttonActions = new Dictionary<string, Action>
        {
            { "leftButton", Pause },
            { "leftArrow", Skip },
            { "rightArrow", Replay },
            { "upArrow", SpeedUp },
            { "downArrow", SlowDown },
            { "pageUp", SportsVolumeUp },
            { "pageDown", SportsVolumeDown },
            { "rightButton", Drop },
            { "contextMenu", ToggleGame }
        };
    }

    private void Awake()
    {
        InputSystem.onAnyButtonPress.Call((button) => buttonActions.GetValueOrDefault(button.name)?.Invoke());
    }

    private void Pause()
    {
        ChatManager.IsPaused = !ChatManager.IsPaused;
    }

    private void Skip()
    {
        ChatManager.SkipToEnd = true;
    }

    private void Replay()
    {

    }

    private void SpeedUp()
    {
        ActorController.GlobalSpeakingRate -= 0.1f;
    }

    private void SlowDown()
    {
        ActorController.GlobalSpeakingRate += 0.1f;
    }

    private void SportsVolumeUp()
    {

    }

    private void SportsVolumeDown()
    {

    }

    private void Drop()
    {

    }

    private void ToggleGame()
    {

    }
}