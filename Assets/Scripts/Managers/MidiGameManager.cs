using UnityEngine;
using System.Collections.Generic;

public class MidiGameManager : MonoBehaviour
{
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

    void Awake()
    {
        activeLanes = new List<NoteUI>[12];
        for (int i = 0; i < 12; i++) activeLanes[i] = new List<NoteUI>();

        foreach (HitResult result in System.Enum.GetValues(typeof(HitResult)))
            hitStats[result] = 0;
    }

    void Start()
    {
        if (hitBar == null) Debug.LogError("HitBar eksik!");
        if (notesContainer == null) Debug.LogError("NotesContainer eksik!");
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++) CreateNewPoolObject();
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
        NoteUI note = (notePool.Count > 0) ? notePool.Dequeue() : CreateNewPoolObject();
        if (notePool.Count == 0) note = notePool.Dequeue(); // Eğer yeni oluşturduysak geri al

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

    // --- TUŞA BASILINCA (SESİ BAŞLAT) ---
    public void OnMidiKeyPressed(int lane)
    {
        // 1. Efekti ve Sesi Başlat
        if (laneEffects != null && lane < laneEffects.Length && laneEffects[lane] != null)
        {
            laneEffects[lane].OnLaneHit(1.0f); 
        }

        // 2. Vuruş Kontrolü (Hit Detection)
        if (lane < 0 || lane >= activeLanes.Length) return;
        List<NoteUI> notes = activeLanes[lane];
        if (notes.Count == 0) return;

        NoteUI closestNote = null;
        float closestAbsDistance = float.MaxValue;
        
        foreach (NoteUI note in notes)
        {
            float signedDist = GetNoteSignedDistance(note.RectTransform);
            if (signedDist < -missDistance) continue; 
            float absDist = Mathf.Abs(signedDist);
            if (absDist < closestAbsDistance)
            {
                closestAbsDistance = absDist;
                closestNote = note;
            }
        }

        if (closestNote != null)
        {
            HitResult result = EvaluateHit(closestAbsDistance);
            if (result != HitResult.TooEarly)
            {
                RegisterHit(lane, result, closestAbsDistance);
                closestNote.OnHit();
            }
        }
    }

    // --- TUŞU BIRAKINCA (SESİ DURDUR) - BU EKSİKTİ EKLENDİ ---
    public void OnMidiKeyReleased(int lane)
    {
        if (laneEffects != null && lane < laneEffects.Length && laneEffects[lane] != null)
        {
            // LaneFeedback scriptindeki OnLaneRelease fonksiyonunu çağırır (Stop Audio orada)
            laneEffects[lane].OnLaneRelease();
        }
    }
    // --------------------------------------------------------

    private HitResult EvaluateHit(float distance)
    {
        if (distance <= perfectWindow) return HitResult.Perfect;
        else if (distance <= goodWindow) return HitResult.Good;
        else if (distance <= okWindow) return HitResult.Ok;
        else return HitResult.TooEarly;
    }

    public void RegisterHit(int lane, HitResult result, float distance)
    {
        hitHistory.Add(result);
        hitStats[result]++;
        int score = 0;
        switch (result)
        {
            case HitResult.Perfect: score = perfectScore; combo++; break;
            case HitResult.Good: score = goodScore; combo++; break;
            case HitResult.Ok: score = okScore; combo++; break;
        }
        totalScore += score * (1 + combo / 10);
        maxCombo = Mathf.Max(maxCombo, combo);
        if (scoreDisplay != null) scoreDisplay.ShowJudgement(result);
    }

    public void RegisterMiss(int lane)
    {
        hitHistory.Add(HitResult.Miss);
        hitStats[HitResult.Miss]++;
        combo = 0;
        if (scoreDisplay != null) scoreDisplay.ShowJudgement(HitResult.Miss);
    }

    public void PrintStats() { Debug.Log($"Score: {totalScore} | Combo: {maxCombo}"); }

    public void ResetGame()
    {
        foreach (var lane in activeLanes)
        {
            foreach (var note in lane.ToArray()) 
            {
                if (note != null) { note.gameObject.SetActive(false); notePool.Enqueue(note); }
            }
            lane.Clear();
        }
        foreach (HitResult result in System.Enum.GetValues(typeof(HitResult))) hitStats[result] = 0;
        hitHistory.Clear();
        totalScore = 0; combo = 0;
    }

    public float MissDistance => missDistance;
    public float HitBarX => 0f;
    public int TotalScore => totalScore;
    public int CurrentCombo => combo;
    public Dictionary<HitResult, int> GetHitStats() => new Dictionary<HitResult, int>(hitStats);

    void OnDrawGizmos()
    {
        if (hitBar == null) return;
        float scaleX = hitBar.lossyScale.x;
        Vector3 center = hitBar.position + (Vector3.right * hitOffset * scaleX);
        // ... (Gizmos kodları aynı kalabilir) ...
    }
}

public enum HitResult { Perfect, Good, Ok, Miss, TooEarly }