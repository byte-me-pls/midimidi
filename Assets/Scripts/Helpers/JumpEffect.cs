using UnityEngine;
using TMPro;
using System.Collections;

public class JumpEffect : MonoBehaviour
{
    public static JumpEffect Instance;

    [Header("ZIPLAMA EKİBİ (Herkesi Buraya Sürükle)")]
    public GameObject[] tumEkip; // Buraya 'fok' dahil zıplamasını istediğin herkesi ekle!

    [Header("Ayarlar")]
    public float jumpForce = 5f;
    public float jumpInterval = 2.5f;
    public float fontSize = 1f;
    [Range(0f, 5f)] public float textHeightOffset = 2.5f;
    
    [Header("Efektler")]
    public GameObject textBubblePrefab;
    public float shakeAmount = 0.1f;
    public float shakeSpeed = 10f;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // Senin kodun burayı çağırıyor
    public void TriggerJump(GameObject target, string text)
    {
        // 1. Önce listedeki (Inspector'a eklediğin) herkesi zıplat
        if (tumEkip != null)
        {
            foreach (GameObject arkadas in tumEkip)
            {
                if (arkadas != null) ApplyEffect(arkadas, text);
            }
        }

        // 2. Eğer 'Trigger'ı tetikleyen (fok) listede unutulmuşsa, onu da ayrıca zıplat
        // (Listede zaten varsa ikinciye zıplamaz, kontrol ediyoruz)
        if (target != null && !IsObjectInList(target))
        {
            ApplyEffect(target, text);
        }
    }

    // Hedef listede var mı kontrolü
    private bool IsObjectInList(GameObject obj)
    {
        foreach (GameObject item in tumEkip)
        {
            if (item == obj) return true;
        }
        return false;
    }

    // --- EFEKT UYGULAMA MERKEZİ ---
    private void ApplyEffect(GameObject target, string text)
    {
        // A. Sürekli Zıplama Scripti Ekle/Başlat
        ContinuousJumper jumper = target.GetComponent<ContinuousJumper>();
        if (jumper == null) jumper = target.AddComponent<ContinuousJumper>();
        jumper.StartJumping(jumpForce, jumpInterval);

        // B. Metin Baloncuğu Ekle/Güncelle
        BubbleController existingBubble = target.GetComponentInChildren<BubbleController>();
        if (existingBubble != null)
        {
            var tmp = existingBubble.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = text; tmp.fontSize = fontSize; }
        }
        else
        {
            CreateTextBubble(target, text);
        }
    }

    private void CreateTextBubble(GameObject target, string text)
    {
        Vector3 pos = target.transform.position + Vector3.up * textHeightOffset;
        GameObject bubble;

        if (textBubblePrefab)
        {
            bubble = Instantiate(textBubblePrefab, pos, Quaternion.identity);
            var tmp = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) { tmp.text = text; tmp.fontSize = fontSize; }
        }
        else
        {
            bubble = new GameObject("TextBubble");
            bubble.transform.position = pos;
            var canvas = bubble.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(5, 2);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(bubble.transform, false);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 2);
        }

        var ctrl = bubble.AddComponent<BubbleController>();
        ctrl.Initialize(target.transform, shakeAmount, shakeSpeed, textHeightOffset);
    }
}

// --- YARDIMCI SCRIPTLER (Hepsini tek dosyada tutabilirsin) ---

public class ContinuousJumper : MonoBehaviour
{
    private float force, interval;
    private Rigidbody rb;
    private bool running = false;
    void Awake() { rb = GetComponent<Rigidbody>(); }

    public void StartJumping(float f, float i)
    {
        force = f; interval = i;
        if (!running) StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        running = true;
        while (true)
        {
            if (rb) {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * force, ForceMode.Impulse);
            }
            yield return new WaitForSeconds(interval);
        }
    }
}