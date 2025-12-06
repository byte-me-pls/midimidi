using UnityEngine;
using System.Collections.Generic;
using MidiJack;

public class MidiInputHandler : MonoBehaviour
{
    [Header("Referanslar")]
    public MidiGameManager gameManager;

    [Header("MIDI Ayarları")]
    public int[] noteToLaneMap = new int[12] 
    { 
        48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59 
    };

    [Header("Debug - Klavye ile Test")]
    public bool useKeyboardForTesting = true;
    public KeyCode[] testKeys = new KeyCode[12]
    {
        KeyCode.Z, KeyCode.S, KeyCode.X, KeyCode.D, 
        KeyCode.C, KeyCode.V, KeyCode.G, KeyCode.B, 
        KeyCode.H, KeyCode.N, KeyCode.J, KeyCode.M
    };

    [Header("Debug Log")]
    public bool showDebugLog = true;

    void Start()
    {
        Debug.Log("═══ MIDI INPUT HANDLER (MidiJack) ═══");
    }

    void Update()
    {
        HandleMidiInput();

        if (useKeyboardForTesting)
        {
            HandleKeyboardInput();
        }
    }

    void HandleMidiInput()
    {
        for (int i = 0; i < noteToLaneMap.Length && i < 12; i++)
        {
            int midiNoteNumber = noteToLaneMap[i];

            // 1. BASMA (Ses Başlar)
            if (MidiMaster.GetKeyDown(midiNoteNumber))
            {
                float velocity = MidiMaster.GetKey(midiNoteNumber);
                OnNotePressed(i, velocity, "MIDI");
            }

            // 2. BIRAKMA (Ses Durur) - BU KISIM EKSİKTİ
            if (MidiMaster.GetKeyUp(midiNoteNumber))
            {
                OnNoteReleased(i, "MIDI");
            }
        }
    }

    void HandleKeyboardInput()
    {
        for (int i = 0; i < testKeys.Length && i < 12; i++)
        {
            // 1. BASMA
            if (Input.GetKeyDown(testKeys[i]))
            {
                OnNotePressed(i, 1.0f, "Keyboard");
            }

            // 2. BIRAKMA - BU KISIM EKSİKTİ
            if (Input.GetKeyUp(testKeys[i]))
            {
                OnNoteReleased(i, "Keyboard");
            }
        }
    }

    private void OnNotePressed(int lane, float velocity, string source)
    {
        if (gameManager != null)
        {
            gameManager.OnMidiKeyPressed(lane);
        }
        if (showDebugLog) Debug.Log($"[{source} BASILDI] Lane: {lane}");
    }

    // YENİ EKLENEN BIRAKMA FONKSİYONU
    private void OnNoteReleased(int lane, string source)
    {
        if (gameManager != null)
        {
            // Manager'a "Bıraktı" haberini yolluyoruz
            gameManager.OnMidiKeyReleased(lane);
        }
        if (showDebugLog) Debug.Log($"[{source} BIRAKILDI] Lane: {lane}");
    }

    void OnValidate()
    {
        if (noteToLaneMap.Length != 12) System.Array.Resize(ref noteToLaneMap, 12);
        if (testKeys.Length != 12) System.Array.Resize(ref testKeys, 12);
    }
}