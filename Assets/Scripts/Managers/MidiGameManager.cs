using UnityEngine;
using System.Collections.Generic;

public class MidiGameManager : MonoBehaviour
{
    [Header("Oyun Ayarları")]
    public float noteSpeed = 800f;
    
    [Header("Optimization (Object Pooling)")]
    public int initialPoolSize = 50; // Başlangıçta kaç nota üretilsin

    [Header("Zamanlama & Offset")]
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
    private List<NoteUI>[] activeLanes;
    
    // --- POOLING SİSTEMİ ---
    private Queue<NoteUI> notePool = new Queue<NoteUI>();

    void Awake()
    {
        activeLanes = new List<NoteUI>[12];
        for (int i = 0; i < 12; i++) activeLanes[i] = new List<NoteUI>();

        foreach (HitResult result in System.Enum.GetValues(typeof(HitResult)))
            hitStats[result] = 0;
            
        Debug.Log("MidiGameManager: Pooling Sistemi Aktif");
    }

    void Start()
    {
        if (hitBar == null) Debug.LogError("Lütfen HitBar referansını ata!");
        if (notesContainer == null) Debug.LogError("Lütfen NotesContainer referansını ata!");

        // Havuzu başlangıçta doldur
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }
    }

    NoteUI CreateNewPoolObject()
    {
        GameObject noteObj = Instantiate(notePrefab, notesContainer);
        NoteUI note = noteObj.GetComponent<NoteUI>();
        if (note == null) note = noteObj.AddComponent<NoteUI>();
        
        noteObj.SetActive(false); // Başta kapalı olsun
        notePool.Enqueue(note);
        return note;
    }

    public void SpawnNote(int lane)
    {
        if (lane < 0 || lane >= laneSpawnPoints.Length) return;
        if (notePrefab == null || notesContainer == null) return;

        NoteUI note = null;

        // Havuzda eleman var mı kontrol et
        if (notePool.Count > 0)
        {
            note = notePool.Dequeue();
        }
        else
        {
            // Havuz boşsa yeni üret (havuzu genişlet)
            note = CreateNewPoolObject(); 
            // Create fonksiyonu havuza atıyor, biz hemen kullanacağımız için geri çıkarıyoruz
            note = notePool.Dequeue(); 
        }

        // --- KONUMLANDIRMA VE AKTİFLEŞTİRME ---
        note.gameObject.SetActive(true);
        RectTransform noteRect = note.RectTransform;
        RectTransform spawnPoint = laneSpawnPoints[lane];

        // Konum hesaplama (Hiyerarşi uyumlu)
        Vector3 localSpawnPos = notesContainer.InverseTransformPoint(spawnPoint.position);
        noteRect.anchoredPosition = new Vector2(localSpawnPos.x, localSpawnPos.y);
        noteRect.localRotation = Quaternion.identity;
        noteRect.localScale = Vector3.one;
    
        // Initialize
        note.Initialize(this, lane, noteSpeed);
        activeLanes[lane].Add(note);
    }

    // --- HAVUZA GERİ DÖNDÜRME (Eski RemoveNote yerine bu kullanılacak) ---
    public void ReturnNoteToPool(NoteUI note)
    {
        if (note.LaneIndex >= 0 && note.LaneIndex < activeLanes.Length)
        {
            activeLanes[note.LaneIndex].Remove(note);
        }

        note.gameObject.SetActive(false); // Kapat
        notePool.Enqueue(note);           // Havuza at
    }

    public float GetNoteSignedDistance(RectTransform noteRect)
    {
        Vector3 posInHitBar = hitBar.InverseTransformPoint(noteRect.position);
        return posInHitBar.x - hitOffset;
    }

    public void OnMidiKeyPressed(int lane)
    {
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

        if (closestNote == null) return;

        HitResult result = EvaluateHit(closestAbsDistance);

        if (result != HitResult.TooEarly)
        {
            RegisterHit(lane, result, closestAbsDistance);
            closestNote.OnHit();
        }
        else
        {
            Debug.Log($"Erken basıldı. Mesafe: {closestAbsDistance:F1}");
        }
    }

    private HitResult EvaluateHit(float distance)
    {
        if (distance <= perfectWindow) return HitResult.Perfect;
        else if (distance <= goodWindow) return HitResult.Good;
        else if (distance <= okWindow) return HitResult.Ok;
        else return HitResult.TooEarly;
    }

    public void RegisterHit(int lane, HitResult result, float distance)
    {
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
        Debug.Log($"<color=cyan>{result}</color> | Dist: {distance:F1}");
    }

    public void RegisterMiss(int lane)
    {
        hitStats[HitResult.Miss]++;
        combo = 0;
        if (scoreDisplay != null) scoreDisplay.ShowJudgement(HitResult.Miss);
    }

    public void PrintStats()
    {
        Debug.Log($"Score: {totalScore} | Combo: {maxCombo}");
    }

    public void ResetGame()
    {
        // Aktif olan tüm notaları havuza geri gönder
        foreach (var lane in activeLanes)
        {
            // ToArray kullanıyoruz çünkü döngü içindeyken listeyi değiştireceğiz
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

        foreach (HitResult result in System.Enum.GetValues(typeof(HitResult))) hitStats[result] = 0;
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

public enum HitResult { Perfect, Good, Ok, Miss, TooEarly }