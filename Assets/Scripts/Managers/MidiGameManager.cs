using UnityEngine;
using System.Collections.Generic;

public class MidiGameManager : MonoBehaviour
{
    [Header("Audio Bağlantısı")]
    public RhythmAudioController audioController;

    [Header("Oyun Ayarları")]
    public float noteSpeed = 800f;
    public int initialPoolSize = 50;
    public float hitOffset = 0f;
    public float perfectWindow = 50f;
    public float goodWindow = 100f;
    public float okWindow = 150f;
    public float missDistance = 200f;

    [Header("UI Referansları")]
    public RectTransform hitBar;
    public RectTransform[] laneSpawnPoints;
    public RectTransform notesContainer;
    public GameObject notePrefab;

    [Header("Görsel Efektler")]
    public LaneFeedback[] laneEffects;

    [Header("Skor Sistemi")]
    public int perfectScore = 100;
    public int goodScore = 50;
    public int okScore = 25;

    [Header("UI Display")]
    public ScoreDisplayUI scoreDisplay;

    private Dictionary<HitResult, int> hitStats = new Dictionary<HitResult, int>();
    private int totalScore = 0;
    private int combo = 0;
    private int maxCombo = 0;

    [Header("Detaylı İstatistikler")]
    public List<HitResult> hitHistory = new List<HitResult>();

    private List<NoteUI>[] activeLanes;
    private Queue<NoteUI> notePool = new Queue<NoteUI>();
    private bool[] laneHeld; // BASILI TUŞ STATE'İ

    void Awake()
    {
        int laneCount = 12;

        activeLanes = new List<NoteUI>[laneCount];
        for (int i = 0; i < laneCount; i++)
            activeLanes[i] = new List<NoteUI>();

        laneHeld = new bool[laneCount];

        foreach (HitResult result in System.Enum.GetValues(typeof(HitResult)))
            hitStats[result] = 0;

        Debug.Log("MidiGameManager: Pooling Sistemi + LaneHeld aktif.");
    }

    void Start()
    {
        if (hitBar == null) Debug.LogError("HitBar eksik!");
        if (notesContainer == null) Debug.LogError("NotesContainer eksik!");

        if (audioController == null)
            audioController = FindObjectOfType<RhythmAudioController>();

        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            CreateNewPoolObject();
    }

    NoteUI CreateNewPoolObject()
    {
        GameObject noteObj = Instantiate(notePrefab, notesContainer);
        NoteUI note = noteObj.GetComponent<NoteUI>();
        if (note == null) note = noteObj.AddComponent<NoteUI>();

        noteObj.SetActive(false);
        notePool.Enqueue(note);
        return note;
    }

    public void SpawnNote(int lane)
    {
        if (lane < 0 || lane >= laneSpawnPoints.Length) return;

        NoteUI note;
        if (notePool.Count > 0)
            note = notePool.Dequeue();
        else
            note = CreateNewPoolObject();

        note.gameObject.SetActive(true);

        RectTransform noteRect = note.RectTransform;
        RectTransform spawnPoint = laneSpawnPoints[lane];

        Vector3 localSpawnPos = notesContainer.InverseTransformPoint(spawnPoint.position);
        noteRect.anchoredPosition = new Vector2(localSpawnPos.x, localSpawnPos.y);
        noteRect.localRotation = Quaternion.identity;
        noteRect.localScale = Vector3.one;

        note.Initialize(this, lane, noteSpeed);
        activeLanes[lane].Add(note);
    }

    public void ReturnNoteToPool(NoteUI note)
    {
        if (note == null) return;

        if (note.LaneIndex >= 0 && note.LaneIndex < activeLanes.Length)
            activeLanes[note.LaneIndex].Remove(note);

        note.gameObject.SetActive(false);
        notePool.Enqueue(note);
    }

    public float GetNoteSignedDistance(RectTransform noteRect)
    {
        Vector3 posInHitBar = hitBar.InverseTransformPoint(noteRect.position);
        return posInHitBar.x - hitOffset;
    }

    /// <summary>
    /// MIDI veya klavyeden bir lane BASILDI.
    /// </summary>
    public void OnMidiKeyPressed(int lane)
    {
        if (lane < 0 || lane >= activeLanes.Length) return;

        // Zaten basılıysa ignore (çift event / jitter engeli)
        if (laneHeld[lane]) return;
        laneHeld[lane] = true;

        // 1. Görsel Efekt
        if (laneEffects != null && lane < laneEffects.Length && laneEffects[lane] != null)
            laneEffects[lane].OnLaneHit(1.0f);

        // 2. Nota Kontrolü (Hit/Miss/Early)
        HandleLaneHit(lane);
    }

    /// <summary>
    /// MIDI veya klavyeden bir lane BIRAKILDI.
    /// </summary>
    public void OnMidiKeyReleased(int lane)
    {
        if (lane < 0 || lane >= activeLanes.Length) return;

        // Zaten bırakık ise ignore
        if (!laneHeld[lane]) return;
        laneHeld[lane] = false;

        if (laneEffects != null && lane < laneEffects.Length && laneEffects[lane] != null)
            laneEffects[lane].OnLaneRelease();
    }

    private void HandleLaneHit(int lane)
    {
        List<NoteUI> notes = activeLanes[lane];

        // Boşa basış → ceza
        if (notes == null || notes.Count == 0)
        {
            if (audioController != null)
                audioController.RegisterError();

            hitHistory.Add(HitResult.Miss);
            hitStats[HitResult.Miss]++;
            combo = 0;

            if (scoreDisplay != null)
                scoreDisplay.ShowJudgement(HitResult.Miss);

            return;
    }

        NoteUI closestNote = null;
        float closestAbsDistance = float.MaxValue;

        foreach (NoteUI note in notes)
        {
            float signedDist = GetNoteSignedDistance(note.RectTransform);

            // Çok geçmiş nota, bunu artık aday yapma
            if (signedDist < -missDistance) continue;

            float absDist = Mathf.Abs(signedDist);
            if (absDist < closestAbsDistance)
            {
                closestAbsDistance = absDist;
                closestNote = note;
            }
        }

        if (closestNote == null) return;

        HitResult result = EvaluateHit(closestAbsDistance);

        // Erken basış
        if (result == HitResult.TooEarly)
        {
            if (audioController != null)
                audioController.RegisterError();

            hitHistory.Add(HitResult.TooEarly);
            hitStats[HitResult.TooEarly]++;
            combo = 0;

            // İster Miss olarak göster, ister ToEarly için ayrı UI yap
            if (scoreDisplay != null)
                scoreDisplay.ShowJudgement(HitResult.Miss);

            return;
        }

        // Başarılı vuruş (Perfect/Good/Ok)
        RegisterHit(lane, result, closestAbsDistance);
        closestNote.OnHit();
    }

    private HitResult EvaluateHit(float distance)
    {
        // distance: ABS mesafe (piksel)
        if (distance <= perfectWindow) return HitResult.Perfect;
        else if (distance <= goodWindow) return HitResult.Good;
        else if (distance <= okWindow) return HitResult.Ok;
        else return HitResult.TooEarly;
    }

    public void RegisterHit(int lane, HitResult result, float distance)
    {
        // Başarılı vuruşta ses toparlasın
        if (audioController != null)
            audioController.RegisterGoodHit();

        hitHistory.Add(result);
        hitStats[result]++;

        int baseScore = 0;
        switch (result)
        {
            case HitResult.Perfect:
                baseScore = perfectScore;
                combo++;
                break;
            case HitResult.Good:
                baseScore = goodScore;
                combo++;
                break;
            case HitResult.Ok:
                baseScore = okScore;
                combo++;
                break;
        }

        totalScore += baseScore * (1 + combo / 10);
        maxCombo = Mathf.Max(maxCombo, combo);

        if (scoreDisplay != null)
            scoreDisplay.ShowJudgement(result);
    }

    public void RegisterMiss(int lane)
    {
        // Auto-miss (nota barı geçti vs.) → ceza
        if (audioController != null)
            audioController.RegisterError();

        hitHistory.Add(HitResult.Miss);
        hitStats[HitResult.Miss]++;
        combo = 0;

        if (scoreDisplay != null)
            scoreDisplay.ShowJudgement(HitResult.Miss);
    }

    public void PrintStats()
    {
        Debug.Log($"Score: {totalScore} | Max Combo: {maxCombo}");
    }

    public void ResetGame()
    {
        // Aktif notaları temizle
        foreach (var lane in activeLanes)
        {
            foreach (var note in lane.ToArray())
            {
                if (note != null)
                {
                    note.gameObject.SetActive(false);
                    notePool.Enqueue(note);
                }
            }
            lane.Clear();
        }

        // İstatistikleri sıfırla
        foreach (HitResult result in System.Enum.GetValues(typeof(HitResult)))
            hitStats[result] = 0;

        hitHistory.Clear();
        totalScore = 0;
        combo = 0;
        maxCombo = 0;

        // Lane held state reset
        for (int i = 0; i < laneHeld.Length; i++)
            laneHeld[i] = false;

        // Ses reset
        if (audioController != null)
            audioController.ResetAudio();
    }

    public float MissDistance => missDistance;
    public float HitBarX => 0f;
    public int TotalScore => totalScore;
    public int CurrentCombo => combo;

    public Dictionary<HitResult, int> GetHitStats()
    {
        return new Dictionary<HitResult, int>(hitStats);
    }

    void OnDrawGizmos()
    {
        if (hitBar == null) return;

        float scaleX = hitBar.lossyScale.x;
        float height = 1500f * scaleX;
        Vector3 center = hitBar.position + (Vector3.right * hitOffset * scaleX);

        Gizmos.color = new Color(0f, 0.7f, 1f, 0.2f);
        Gizmos.DrawCube(center, new Vector3(okWindow * 2 * scaleX, height, 1));
        Gizmos.DrawWireCube(center, new Vector3(okWindow * 2 * scaleX, height, 1));

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawCube(center, new Vector3(goodWindow * 2 * scaleX, height, 1));
        Gizmos.DrawWireCube(center, new Vector3(goodWindow * 2 * scaleX, height, 1));

        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawCube(center, new Vector3(perfectWindow * 2 * scaleX, height, 1));
        Gizmos.DrawWireCube(center, new Vector3(perfectWindow * 2 * scaleX, height, 1));

        Gizmos.color = Color.red;
        Vector3 missPos = hitBar.position - (Vector3.right * missDistance * scaleX);
        Gizmos.DrawLine(missPos + Vector3.up * (height / 2), missPos - Vector3.up * (height / 2));
    }
}

// Hit sonuçları
public enum HitResult
{
    Perfect,
    Good,
    Ok,
    Miss,
    TooEarly
}
