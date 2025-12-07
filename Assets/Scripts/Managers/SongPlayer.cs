using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Coroutine için gerekli
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

    [Header("Ayarlar")]
    public bool autoPlayOnStart = true;
    public float delayBeforeStart = 2f;
    public bool loopSong = false;

    [Header("Bitiş Ayarları")]
    [Tooltip("Şarkı bittikten kaç saniye sonra oyun donsun?")]
    public float stopGameDelay = 5f; // İsteğine göre 5 saniye varsayılan

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

            // 2x Step Modu (Notaları 2'şer atladığımız için süreyi çarpıyoruz)
            beatInterval = (60f / song.bpm) * 2; 
            
            timer = 0f;
            currentBeatIndex = 0;
            songLoaded = true;

            if (showDebugInfo)
            {
                Debug.Log($"<color=green>═══ ŞARKI YÜKLENDİ (2x STEP MODU) ═══</color>");
                Debug.Log($"BPM: {song.bpm}");
                Debug.Log($"Beat Sayısı: {song.notes.Count}");
                Debug.Log($"Tahmini Süre: ~{((song.notes.Count / 2) * beatInterval):F1} saniye");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON parse hatası: {e.Message}\nStack: {e.StackTrace}");
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
                if (int.TryParse(numStr.Trim(), out int note))
                {
                    beatLine.Add(note);
                }
            }

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
        int spawnedCount = 0;

        for (int lane = 0; lane < currentBeat.Count && lane < 12; lane++)
        {
            if (currentBeat[lane] == 1)
            {
                manager.SpawnNote(lane);
                spawnedCount++;
            }
        }

        currentBeatIndex += 2;
    }

    void OnSongComplete()
    {
        if (!isPlaying) return; // Zaten bittiyse tekrar girme
        isPlaying = false;

        if (showDebugInfo)
        {
            Debug.Log("<color=green>═══ ŞARKI BİTTİ (Notalar Tükendi) ═══</color>");
            manager.PrintStats();
        }

        if (loopSong)
        {
            RestartSong();
        }
        else
        {
            // Şarkı tekrar etmeyecekse bekleme sürecini başlat
            StartCoroutine(WaitAndStopGame());
        }
    }

    // --- YENİ EKLENEN KISIM: 5 SANİYE BEKLE VE DURDUR ---
    IEnumerator WaitAndStopGame()
    {
        if (showDebugInfo) Debug.Log($"Oyun {stopGameDelay} saniye sonra durdurulacak...");

        // Belirtilen süre kadar bekle (Realtime değil oyun zamanı ile, çünkü oyun hala akıyor)
        yield return new WaitForSeconds(stopGameDelay);

        // Zamanı dondur
        Time.timeScale = 0f;
        
        if (showDebugInfo) Debug.Log("<color=red>■ OYUN DURDURULDU (Time.timeScale = 0) ■</color>");
        
        // Buraya istersen bir "Level Complete" UI paneli açma kodu da ekleyebilirsin.
        // Orneğin: UIManager.Instance.ShowEndScreen();
    }

    public void StartSong()
    {
        if (!songLoaded) return;
        
        // Eğer oyun daha önce durdurulduysa zamanı tekrar başlatmamız lazım
        Time.timeScale = 1f; 

        startDelay = delayBeforeStart;
        isPlaying = false;
        timer = 0f;
        currentBeatIndex = 0;
        if (showDebugInfo) Debug.Log($"Şarkı {delayBeforeStart} saniye içinde başlayacak...");
    }

    public void RestartSong()
    {
        // Restart atılırsa zamanı tekrar normale döndür
        Time.timeScale = 1f;
        StopAllCoroutines(); // Eğer durdurma sayacı çalışıyorsa iptal et

        timer = 0f;
        currentBeatIndex = 0;
        startDelay = delayBeforeStart;
        isPlaying = false;
        if (manager != null) manager.ResetGame();
        if (showDebugInfo) Debug.Log("Şarkı yeniden başlatılıyor...");
    }

    public bool IsPlaying => isPlaying;
    public float Progress => song != null && song.notes.Count > 0 ? (float)currentBeatIndex / song.notes.Count : 0f;
}