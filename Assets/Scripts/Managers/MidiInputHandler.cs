using UnityEngine;
using System.Collections.Generic;
using MidiJack;

public class MidiInputHandler : MonoBehaviour
{
    [Header("Referanslar")]
    public MidiGameManager gameManager;

    [Header("MIDI Ayarları")]
    // İSTEĞİN ÜZERİNE GÜNCELLENDİ: 48'den 59'a kadar olan notalar
    // 48=Lane0, 49=Lane1 ... 59=Lane11
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
        Debug.Log($"Dinlenen Notalar: {noteToLaneMap[0]} - {noteToLaneMap[noteToLaneMap.Length-1]} arası");
    }

    void Update()
    {
        // 1. MIDI Girişini Kontrol Et
        HandleMidiInput();

        // 2. Klavye ile Test Et
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

            // MidiJack ile tuşa basılma anını yakala
            if (MidiMaster.GetKeyDown(midiNoteNumber))
            {
                float velocity = MidiMaster.GetKey(midiNoteNumber);
                OnNotePressed(i, velocity, "MIDI");
            }
        }
    }

    void HandleKeyboardInput()
    {
        for (int i = 0; i < testKeys.Length && i < 12; i++)
        {
            if (Input.GetKeyDown(testKeys[i]))
            {
                OnNotePressed(i, 1.0f, "Keyboard");
            }
        }
    }

    private void OnNotePressed(int lane, float velocity, string source)
    {
        if (gameManager != null)
        {
            gameManager.OnMidiKeyPressed(lane);
        }

        if (showDebugLog)
        {
            int noteNum = noteToLaneMap[lane];
            Debug.Log($"[{source}] Note: {noteNum} -> Lane: {lane} (Vel: {velocity:F2})");
        }
    }

    void OnValidate()
    {
        if (noteToLaneMap.Length != 12) System.Array.Resize(ref noteToLaneMap, 12);
        if (testKeys.Length != 12) System.Array.Resize(ref testKeys, 12);
    }
}