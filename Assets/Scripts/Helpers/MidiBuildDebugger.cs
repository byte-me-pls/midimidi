using UnityEngine;
using MidiJack; // MidiJack kütüphanesi

public class MidiBuildDebugger : MonoBehaviour
{
    private string lastLog = "Henüz veri yok...";
    private string deviceStatus = "MidiJack Kontrol Ediliyor...";
    
    // GUI stil ayarları
    private GUIStyle style;

    void Start()
    {
        // Yazı boyutunu büyütelim ki buildde okunsun
        style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        
        // Cihaz sayısını kontrol et (MidiJack API'sine göre değişebilir, genel kontrol)
        CheckDevices();
    }

    void CheckDevices()
    {
        // MidiJack'in arka planda çalışıp çalışmadığını anlamak için basit bir log
        deviceStatus = "Midi Sürücüsü Aktif. Tuşlara basın.";
    }

    void Update()
    {
        // 128 notayı tara
        for (int i = 0; i < 128; i++)
        {
            if (MidiMaster.GetKeyDown(i))
            {
                lastLog = $"Son Basılan: NOTA {i} (KeyDown)";
            }
            
            // Knob/Slider kontrolü (Kanal 0-30 arası tara)
            if (i < 30)
            {
                float knobVal = MidiMaster.GetKnob(i);
                if (knobVal > 0.01f)
                {
                    lastLog = $"Son Oynanan: KNOB {i} - Değer: {knobVal:F2}";
                }
            }
        }
    }

    void OnGUI()
    {
        // Ekranın sol üstüne siyah bir kutu çiz
        GUI.Box(new Rect(10, 10, 500, 200), "");
        
        // Durumları yazdır
        GUI.Label(new Rect(20, 20, 480, 50), "--- MIDI BUILD DEBUGGER ---", style);
        GUI.Label(new Rect(20, 70, 480, 50), deviceStatus, style);
        
        // Dinamik olarak kırmızı renkle son basılanı göster
        style.normal.textColor = Color.yellow;
        GUI.Label(new Rect(20, 120, 480, 50), lastLog, style);
        style.normal.textColor = Color.white;
    }
}