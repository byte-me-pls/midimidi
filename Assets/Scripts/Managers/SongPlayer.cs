using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions; // Regex kütüphanesini ekledik

[System.Serializable]
public class SongData
{
    public int bpm;
    // JsonUtility bunu görmezden gelecek ama biz manuel dolduracağız
    public List<List<int>> notes = new List<List<int>>();
}

public class SongPlayer : MonoBehaviour
{
    [Header("Referanslar")]
    public TextAsset jsonFile;
    public MidiGameManager manager;

    [Header("Ayarlar")]
    public bool autoPlayOnStart = true;
    public float delayBeforeStart = 2f;
    public bool loopSong = false;

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
            // STANDART YÖNTEM YERİNE ÖZEL PARSER KULLANIYORUZ
            song = ParseCustomJson(jsonFile.text);

            if (song == null || song.notes == null || song.notes.Count == 0)
            {
                Debug.LogError("Song verisi yüklenemedi veya boş!");
                return;
            }

            if (song.bpm <= 0)
            {
                Debug.LogWarning("Geçersiz BPM, 120'ye ayarlandı");
                song.bpm = 120;
            }

            beatInterval = 60f / song.bpm;
            timer = 0f;
            currentBeatIndex = 0;
            songLoaded = true;

            if (showDebugInfo)
            {
                Debug.Log($"<color=green>═══ ŞARKI YÜKLENDİ (MANUEL PARSE) ═══</color>");
                Debug.Log($"Dosya: {jsonFile.name}");
                Debug.Log($"BPM: {song.bpm}");
                Debug.Log($"Beat Sayısı: {song.notes.Count}");
                Debug.Log($"Beat Aralığı: {beatInterval:F3} saniye");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON parse hatası: {e.Message}\nStack: {e.StackTrace}");
            songLoaded = false;
        }
    }

    /// <summary>
    /// Unity JsonUtility'nin okuyamadığı iç içe listeleri ([[1,0...]]) okumak için özel parser.
    /// </summary>
    SongData ParseCustomJson(string jsonText)
    {
        SongData data = new SongData();
        data.notes = new List<List<int>>();

        // 1. BPM'i Regex ile bul
        var bpmMatch = Regex.Match(jsonText, "\"bpm\"\\s*:\\s*(\\d+)");
        if (bpmMatch.Success)
        {
            data.bpm = int.Parse(bpmMatch.Groups[1].Value);
        }
        else
        {
            data.bpm = 120; // Varsayılan
        }

        // 2. "notes": [ ... ] bloğunun içini bul
        int notesStartIndex = jsonText.IndexOf("\"notes\"");
        if (notesStartIndex == -1) return data;

        // Notes'un başladığı ilk '[' karakterini bul
        int arrayOpenIndex = jsonText.IndexOf('[', notesStartIndex);
        // Dosyanın sonundaki ']' karakterini bul (kabaca son '}' den öncesi)
        int arrayCloseIndex = jsonText.LastIndexOf(']');

        if (arrayOpenIndex == -1 || arrayCloseIndex == -1) return data;

        // Sadece array içeriğini al: [[1,0...], [0,1...]]
        string notesContent = jsonText.Substring(arrayOpenIndex + 1, arrayCloseIndex - arrayOpenIndex - 1);

        // 3. İçteki [ ... ] bloklarını Regex ile yakala
        // Bu desen, köşeli parantez içindeki sayıları ve virgülleri yakalar
        MatchCollection beatMatches = Regex.Matches(notesContent, @"\[([\d,\s]+)\]");

        foreach (Match match in beatMatches)
        {
            string content = match.Groups[1].Value; // Parantez içindeki "1, 0, 0, ..." stringi
            
            List<int> beatLine = new List<int>();
            string[] numbers = content.Split(',');

            foreach (string numStr in numbers)
            {
                if (int.TryParse(numStr.Trim(), out int note))
                {
                    beatLine.Add(note);
                }
            }

            // Eğer satırda veri varsa listeye ekle
            if (beatLine.Count > 0)
            {
                data.notes.Add(beatLine);
            }
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
                if (showDebugInfo)
                {
                    Debug.Log("<color=yellow>♪ ŞARKI BAŞLADI ♪</color>");
                }
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
        int spawnedCount = 0;

        for (int lane = 0; lane < currentBeat.Count && lane < 12; lane++)
        {
            if (currentBeat[lane] == 1)
            {
                manager.SpawnNote(lane);
                spawnedCount++;
            }
        }

        currentBeatIndex++;
    }

    void OnSongComplete()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (showDebugInfo)
        {
            Debug.Log("<color=green>═══ ŞARKI BİTTİ ═══</color>");
            manager.PrintStats();
        }

        if (loopSong)
        {
            RestartSong();
        }
    }

    public void StartSong()
    {
        if (!songLoaded)
        {
            Debug.LogError("Şarkı yüklenemedi, başlatılamıyor!");
            return;
        }

        startDelay = delayBeforeStart;
        isPlaying = false;
        timer = 0f;
        currentBeatIndex = 0;

        if (showDebugInfo)
        {
            Debug.Log($"Şarkı {delayBeforeStart} saniye içinde başlayacak...");
        }
    }

    public void RestartSong()
    {
        timer = 0f;
        currentBeatIndex = 0;
        startDelay = delayBeforeStart;
        isPlaying = false;

        if (manager != null)
        {
            manager.ResetGame();
        }

        if (showDebugInfo)
        {
            Debug.Log("Şarkı yeniden başlatılıyor...");
        }
    }

    public bool IsPlaying => isPlaying;
    public float Progress => song != null && song.notes.Count > 0 ? (float)currentBeatIndex / song.notes.Count : 0f;
}