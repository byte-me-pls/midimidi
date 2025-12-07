using UnityEngine;
using MidiJack;

public class MidiInputHandler : MonoBehaviour
{
    [Header("Referanslar")]
    public MidiGameManager gameManager;

    [Header("MIDI Ayarları")]
    // 12 lane için MIDI notaları
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
        if (gameManager == null) return;

        for (int i = 0; i < noteToLaneMap.Length && i < 12; i++)
        {
            int midiNoteNumber = noteToLaneMap[i];

            // BASMA
            if (MidiMaster.GetKeyDown(midiNoteNumber))
            {
                float velocity = MidiMaster.GetKey(midiNoteNumber);
                OnNotePressed(i, velocity, "MIDI");
            }

            // BIRAKMA
            if (MidiMaster.GetKeyUp(midiNoteNumber))
            {
                OnNoteReleased(i, "MIDI");
            }
        }
    }

    void HandleKeyboardInput()
    {
        if (gameManager == null) return;

        for (int i = 0; i < testKeys.Length && i < 12; i++)
        {
            // BASMA
            if (Input.GetKeyDown(testKeys[i]))
            {
                OnNotePressed(i, 1.0f, "Keyboard");
            }

            // BIRAKMA
            if (Input.GetKeyUp(testKeys[i]))
            {
                OnNoteReleased(i, "Keyboard");
            }
        }
    }

    private void OnNotePressed(int lane, float velocity, string source)
    {
        gameManager.OnMidiKeyPressed(lane);

        if (showDebugLog)
            Debug.Log($"[{source} BASILDI] Lane: {lane}, Velocity: {velocity:F2}");
    }

    private void OnNoteReleased(int lane, string source)
    {
        gameManager.OnMidiKeyReleased(lane);

        if (showDebugLog)
            Debug.Log($"[{source} BIRAKILDI] Lane: {lane}");
    }

    void OnValidate()
    {
        if (noteToLaneMap.Length != 12) System.Array.Resize(ref noteToLaneMap, 12);
        if (testKeys.Length != 12) System.Array.Resize(ref testKeys, 12);
    }
}
