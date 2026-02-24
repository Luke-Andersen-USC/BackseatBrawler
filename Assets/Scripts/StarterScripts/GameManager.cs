using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using Object = System.Object;

public class GameManager : Singleton<GameManager>
{
    public static bool KeyboardFallbackEnabled { get; private set; }

    private List<PlayerController> players;

    public List<PlayerController> Players
    {
        get
        {
            if (players == null)
            {
                players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).ToList();
            }

            return players;
        }
    }


    private UIController uiController;

    public UIController UIController
    {
        get
        {
            if (uiController == null)
            {
                uiController = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
            }

            return uiController;
        }
    }

    private RoundController roundController;

    public RoundController RoundController
    {
        get
        {
            if (roundController == null)
            {
                roundController = GameObject.FindGameObjectWithTag("RoundController").GetComponent<RoundController>();
            }

            return roundController;
        }
    }


    public static void Setup()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Time.fixedDeltaTime = 1f / 60f;

        var playerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None)
            .OrderBy(p => p.playerIndex)
            .ToArray();

        if (playerInputs.Length == 0)
        {
            Debug.LogError("No PlayerInput components found in scene!");
            return;
        }

        KeyboardFallbackEnabled = false;

        int pairedCount = 0;

        // Prefer gamepads when connected.
        int gamepadPairCount = Mathf.Min(playerInputs.Length, Gamepad.all.Count, 2);
        for (int i = 0; i < gamepadPairCount; i++)
        {
            PairDevice(playerInputs[i], Gamepad.all[i]);
            pairedCount++;
        }

        // Failsafe: when no gamepads (or not enough), wire remaining players to keyboard.
        if (pairedCount < playerInputs.Length)
        {
            int keyboardPairs = PairKeyboardFallback(playerInputs, pairedCount);
            if (keyboardPairs > 0)
            {
                KeyboardFallbackEnabled = true;
                pairedCount += keyboardPairs;
            }
        }

        if (pairedCount == 0)
        {
            Debug.LogError("No compatible input devices found (gamepad/keyboard).");
            return;
        }

        Debug.Log($"Paired {pairedCount} PlayerInputs. Gamepads: {Gamepad.all.Count}, Keyboard fallback active: {Keyboard.current != null}");
    }

    private static int PairKeyboardFallback(PlayerInput[] inputs, int startIndex)
    {
        if (Keyboard.current == null)
        {
            Debug.LogWarning("Keyboard fallback unavailable: no keyboard device found.");
            return 0;
        }

        int paired = 0;

        for (int i = startIndex; i < inputs.Length; i++)
        {
            PlayerInput input = inputs[i];
            if (input == null)
                continue;

            var user = input.user;
            user.UnpairDevices();
            InputUser.PerformPairingWithDevice(Keyboard.current, user);

            if (Mouse.current != null)
            {
                InputUser.PerformPairingWithDevice(Mouse.current, user);
            }

            input.defaultControlScheme = "Keyboard&Mouse";
            if (Mouse.current != null)
            {
                input.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
            }
            else
            {
                input.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current);
            }

            paired++;

            Debug.Log($"Keyboard fallback paired to {input.gameObject.name}. Controls: A/D or Arrow keys move, UpArrow/Space punch, DownArrow/LeftShift block.");
        }

        return paired;
    }

    private static void PairDevice(PlayerInput input, InputDevice device)
    {
        if (input == null || device == null)
        {
            Debug.LogError("Cannot pair device: null reference");
            return;
        }

        var user = input.user;

        user.UnpairDevices();
        InputUser.PerformPairingWithDevice(device, user);

        input.SwitchCurrentControlScheme(device);

        Debug.Log($"Paired {input.gameObject.name} to {device.displayName}");
    }


    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
