using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Text.RegularExpressions;

[System.Serializable]
public class SongData
{
    public int bpm;
    public List<List<int>> notes = new List<List<int>>();
}

public class SongPlayer : MonoBehaviour
{
    [Header("Referanslar")]
    public TextAsset jsonFile;
    public MidiGameManager manager;

    [Header("UI Referansları")]
    [Tooltip("Oyun bittiğinde açılacak olan Panel (Canvas içindeki)")]
    public GameObject gameEndUI; // YENİ: Bitiş ekranı paneli

    [Header("Ayarlar")]
    public bool autoPlayOnStart = true;
    public float delayBeforeStart = 2f;
    public bool loopSong = false;

    [Header("Bitiş Ayarları")]
    public float stopGameDelay = 5f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private SongData song;
    private float beatInterval;
    private float timer;
    private int currentBeatIndex;
    private bool isPlaying = false;
    private float startDelay;
    private bool songLoaded = false;

    void Start()
    {
        // Başlangıçta bitiş ekranı açıksa kapatalım
        if (gameEndUI != null) gameEndUI.SetActive(false);

        if (jsonFile == null)
        {
            Debug.LogError("SongPlayer: JSON dosyası atanmadı!");
            return;
        }

        if (manager == null)
        {
            Debug.LogError("SongPlayer: MidiGameManager atanmadı!");
            return;
        }

        LoadSong();

        if (autoPlayOnStart && songLoaded)
        {
            StartSong();
        }
    }

    void LoadSong()
    {
        try
        {
            song = ParseCustomJson(jsonFile.text);

            if (song == null || song.notes == null || song.notes.Count == 0)
            {
                Debug.LogError("Song verisi yüklenemedi veya boş!");
                return;
            }

            if (song.bpm <= 0) song.bpm = 120;

            beatInterval = (60f / song.bpm) * 2; 
            
            timer = 0f;
            currentBeatIndex = 0;
            songLoaded = true;

            if (showDebugInfo)
            {
                Debug.Log($"<color=green>ŞARKI YÜKLENDİ. Süre: ~{((song.notes.Count / 2) * beatInterval):F1} sn</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON hatası: {e.Message}");
            songLoaded = false;
        }
    }

    SongData ParseCustomJson(string jsonText)
    {
        SongData data = new SongData();
        data.notes = new List<List<int>>();

        var bpmMatch = Regex.Match(jsonText, "\"bpm\"\\s*:\\s*(\\d+)");
        data.bpm = bpmMatch.Success ? int.Parse(bpmMatch.Groups[1].Value) : 120;

        int notesStartIndex = jsonText.IndexOf("\"notes\"");
        if (notesStartIndex == -1) return data;

        int arrayOpenIndex = jsonText.IndexOf('[', notesStartIndex);
        int arrayCloseIndex = jsonText.LastIndexOf(']');
        if (arrayOpenIndex == -1 || arrayCloseIndex == -1) return data;

        string notesContent = jsonText.Substring(arrayOpenIndex + 1, arrayCloseIndex - arrayOpenIndex - 1);
        MatchCollection beatMatches = Regex.Matches(notesContent, @"\[([\d,\s]+)\]");

        foreach (Match match in beatMatches)
        {
            string content = match.Groups[1].Value;
            List<int> beatLine = new List<int>();
            string[] numbers = content.Split(',');
            foreach (string numStr in numbers)
            {
                if (int.TryParse(numStr.Trim(), out int note)) beatLine.Add(note);
            }
            if (beatLine.Count > 0) data.notes.Add(beatLine);
        }
        return data;
    }

    void Update()
    {
        if (!songLoaded || song == null) return;

        if (startDelay > 0)
        {
            startDelay -= Time.deltaTime;
            if (startDelay <= 0)
            {
                isPlaying = true;
                if (showDebugInfo) Debug.Log("<color=yellow>♪ ŞARKI BAŞLADI ♪</color>");
            }
            return;
        }

        if (!isPlaying) return;

        timer += Time.deltaTime;

        if (timer >= beatInterval)
        {
            timer -= beatInterval;
            PlayBeat();
        }
    }

    void PlayBeat()
    {
        if (currentBeatIndex >= song.notes.Count)
        {
            OnSongComplete();
            return;
        }

        List<int> currentBeat = song.notes[currentBeatIndex];
        for (int lane = 0; lane < currentBeat.Count && lane < 12; lane++)
        {
            if (currentBeat[lane] == 1) manager.SpawnNote(lane);
        }
        currentBeatIndex += 2;
    }

    void OnSongComplete()
    {
        if (!isPlaying) return;
        isPlaying = false;

        if (showDebugInfo) Debug.Log("<color=green>ŞARKI BİTTİ</color>");

        if (loopSong) RestartSong();
        else StartCoroutine(WaitAndStopGame());
    }

    IEnumerator WaitAndStopGame()
    {
        if (showDebugInfo) Debug.Log($"Oyun {stopGameDelay} saniye sonra durdurulacak...");

        // 1. Bekle
        yield return new WaitForSeconds(stopGameDelay);

        // 2. Bitiş Ekranını Aç (Panel)
        if (gameEndUI != null)
        {
            gameEndUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameEndUI (Bitiş Paneli) atanmamış!");
        }

        // 3. Zamanı Dondur
        Time.timeScale = 0f;
        
        if (showDebugInfo) Debug.Log("<color=red>■ OYUN DURDURULDU ■</color>");
    }

    public void StartSong()
    {
        if (!songLoaded) return;
        
        // UI'ı gizle ve zamanı başlat
        if (gameEndUI != null) gameEndUI.SetActive(false);
        Time.timeScale = 1f; 

        startDelay = delayBeforeStart;
        isPlaying = false;
        timer = 0f;
        currentBeatIndex = 0;
    }

    public void RestartSong()
    {
        Time.timeScale = 1f;
        StopAllCoroutines();
        
        // UI'ı gizle
        if (gameEndUI != null) gameEndUI.SetActive(false);

        timer = 0f;
        currentBeatIndex = 0;
        startDelay = delayBeforeStart;
        isPlaying = false;
        if (manager != null) manager.ResetGame();
    }

    public bool IsPlaying => isPlaying;
    public float Progress => song != null && song.notes.Count > 0 ? (float)currentBeatIndex / song.notes.Count : 0f;
}