using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CarAIController : MonoBehaviour
{
    private static CarAIController _instance;
    public static CarAIController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CarAIController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CarAIController");
                    _instance = go.AddComponent<CarAIController>();
                }
            }
            return _instance;
        }
    }

    [Header("Araba Yön Kontrolü")]
    [SerializeField] private Transform arabaOnuGuide; // Arabanın önüne koy, +Z ileri baksın
    
    [Header("Waypoint Sistemi")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private bool donguselRota = false;
    [SerializeField] private int curveNoktaSayisi = 20; // Curve'ü kaç noktaya bölelim
    
    [Header("Fizik Ayarları")]
    [SerializeField] private float motorGucu = 2500f;
    [SerializeField] private float direksiyon = 50f;
    [SerializeField] private float maksHiz = 25f;
    [SerializeField] private float minDonusHizi = 2f; // Dönmek için minimum hız

    [Header("Drift Ayarları")]
    [SerializeField] private float driftEsikAcisi = 30f; // Bu açıdan sonra drift başlar
    [SerializeField] private float driftGucu = 0.7f; // Yan kayma miktarı (0-1)
    [SerializeField] private float driftStabilizasyon = 2f; // Drift sırasında denge
    [SerializeField] private float driftHizCarpani = 1.2f; // Drift sırasında hız artışı

    [Header("Zemin Kontrolü")]
    [SerializeField] private LayerMask zeminMask;
    [SerializeField] private float zeminMesafesi = 2f; // Daha uzun suspensyon
    [SerializeField] private float supensiyon = 2500f; // Daha güçlü yay
    [SerializeField] private float suspensiyonDamper = 300f; // Daha fazla sönümleme
    [SerializeField] private float suspensiyonHedefYukseklik = 1f; // İdeal yükseklik

    [Header("Tekerlekler")]
    [SerializeField] private Transform[] tekerlekler;
    [SerializeField] private float tekerlekYaricapi = 0.3f;

    [Header("Kontrol")]
    [SerializeField] private float waypointToleransi = 5f;
    [SerializeField] private float rotasyonHizi = 3f;
    [SerializeField] private float ongoruMesafesi = 8f; // Curve üzerinde ne kadar ileriyi hedeflesin

    private Rigidbody rb;
    private Vector3 baslangicPozisyonu;
    private Quaternion baslangicRotasyonu;
    private List<Vector3> curvePath = new List<Vector3>(); // Bezier curve noktaları
    private int mevcutCurveIndex = 0;
    private bool hareketHalinde = false;
    private bool geriDonuyor = false;
    private float mevcutDireksiyon = 0f;
    private float mevcutMotor = 0f;
    private bool driftYapiyorMu = false;
    private Vector3 aktifHedef;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        rb = GetComponent<Rigidbody>();
        KurulumuYap();
    }

    private void KurulumuYap()
    {
        rb.mass = 1500f;
        rb.linearDamping = 0.2f; // Engebeli arazide daha fazla drag
        rb.angularDamping = 2f; // Yatma için daha fazla direnç
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0, -0.8f, 0); // Daha alçak ağırlık merkezi = daha stabil
        
        // Rigidbody constraints - Z ekseninde dönmeyi biraz sınırla (aşırı yatmayı önle)
        rb.maxAngularVelocity = 7f; // Dönüş hızını sınırla
    }

    private void Start()
    {
        baslangicPozisyonu = transform.position;
        baslangicRotasyonu = transform.rotation;
        CurveRotasiOlustur();
        
        // Guide transform yoksa uyarı ver
        if (arabaOnuGuide == null)
        {
            Debug.LogWarning("Araba Onu Guide tanımlanmamış! Inspector'dan arabanın önündeki guide objesini atayın.");
        }
    }
    
    /// <summary>
    /// Arabanın ön yönünü döndürür (Guide varsa onun +Z'si, yoksa -X)
    /// </summary>
    private Vector3 ArabaOnuYonu()
    {
        if (arabaOnuGuide != null)
        {
            return arabaOnuGuide.forward; // Guide'ın +Z ekseni
        }
        else
        {
            return -transform.right; // Fallback: -X
        }
    }

    private void Update()
    {
        // Test tuşu: R tuşuna bas
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotayaBasla();
            Debug.Log("Rota başlatıldı/durduruldu - Drift Eşiği: " + driftEsikAcisi + "°");
        }

        // Durdurma tuşu: T
        if (Input.GetKeyDown(KeyCode.T))
        {
            Durdur();
            Debug.Log("Araba durduruldu");
        }

        // Debug bilgisi: Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Hareket: {hareketHalinde} | Drift: {driftYapiyorMu} | Hız: {rb.linearVelocity.magnitude:F1} m/s | Curve Index: {mevcutCurveIndex}/{curvePath.Count}");
        }
    }

    private void FixedUpdate()
    {
        if (hareketHalinde)
        {
            ZeminUyumu();
            HareketKontrolu();
            FizikselHareket();
        }
        else
        {
            Frenle();
        }
        
        TekerlekleriGuncelle();
    }

    private void CurveRotasiOlustur()
    {
        curvePath.Clear();
        
        if (waypoints.Count < 2) return;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[i].position;
            Vector3 p3 = waypoints[i + 1].position;
            
            // Kontrol noktalarını otomatik oluştur (smooth curve için)
            Vector3 yön1 = (p3 - p0).normalized;
            Vector3 p1 = p0 + yön1 * Vector3.Distance(p0, p3) * 0.33f;
            Vector3 p2 = p3 - yön1 * Vector3.Distance(p0, p3) * 0.33f;
            
            // Bezier curve noktalarını oluştur
            for (int j = 0; j <= curveNoktaSayisi; j++)
            {
                float t = j / (float)curveNoktaSayisi;
                Vector3 nokta = BezierCurve(p0, p1, p2, p3, t);
                curvePath.Add(nokta);
            }
        }

        // Döngüsel rota için başa dön
        if (donguselRota && waypoints.Count > 2)
        {
            Vector3 p0 = waypoints[waypoints.Count - 1].position;
            Vector3 p3 = waypoints[0].position;
            Vector3 yön = (p3 - p0).normalized;
            Vector3 p1 = p0 + yön * Vector3.Distance(p0, p3) * 0.33f;
            Vector3 p2 = p3 - yön * Vector3.Distance(p0, p3) * 0.33f;
            
            for (int j = 0; j <= curveNoktaSayisi; j++)
            {
                float t = j / (float)curveNoktaSayisi;
                curvePath.Add(BezierCurve(p0, p1, p2, p3, t));
            }
        }
    }

    private Vector3 BezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Cubic Bezier Curve formülü
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    private void ZeminUyumu()
    {
        for (int i = 0; i < 4; i++)
        {
            if (tekerlekler != null && i < tekerlekler.Length && tekerlekler[i] != null)
            {
                RaycastHit hit;
                Vector3 tekerlekPos = tekerlekler[i].position;
                
                if (Physics.Raycast(tekerlekPos, -transform.up, out hit, zeminMesafesi, zeminMask))
                {
                    float mesafe = hit.distance;
                    float offset = zeminMesafesi - mesafe;
                    float hiz = Vector3.Dot(transform.up, rb.GetPointVelocity(tekerlekPos));
                    float kuvvet = (offset * supensiyon) - (hiz * suspensiyonDamper);
                    
                    rb.AddForceAtPosition(transform.up * kuvvet, tekerlekPos);
                    Debug.DrawLine(tekerlekPos, hit.point, driftYapiyorMu ? Color.red : Color.green);
                }
            }
        }

        RaycastHit onHit;
        if (Physics.Raycast(transform.position + Vector3.up, -Vector3.up, out onHit, 10f, zeminMask))
        {
            Quaternion hedefRot = Quaternion.FromToRotation(transform.up, onHit.normal) * transform.rotation;
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, hedefRot, Time.fixedDeltaTime * rotasyonHizi));
        }
    }

    private void HareketKontrolu()
    {
        if (curvePath.Count == 0)
        {
            hareketHalinde = false;
            return;
        }

        // En yakın curve noktasını bul
        float enYakinMesafe = float.MaxValue;
        int enYakinIndex = mevcutCurveIndex;
        
        for (int i = mevcutCurveIndex; i < Mathf.Min(mevcutCurveIndex + 10, curvePath.Count); i++)
        {
            float mesafe = Vector3.Distance(transform.position, curvePath[i]);
            if (mesafe < enYakinMesafe)
            {
                enYakinMesafe = mesafe;
                enYakinIndex = i;
            }
        }
        
        mevcutCurveIndex = enYakinIndex;

        // Öngörü mesafesindeki hedef noktayı bul
        int hedefIndex = mevcutCurveIndex;
        float toplamMesafe = 0f;
        
        for (int i = mevcutCurveIndex; i < curvePath.Count - 1; i++)
        {
            toplamMesafe += Vector3.Distance(curvePath[i], curvePath[i + 1]);
            if (toplamMesafe >= ongoruMesafesi)
            {
                hedefIndex = i;
                break;
            }
        }

        // Rota bitti mi?
        if (hedefIndex >= curvePath.Count - 1)
        {
            if (donguselRota)
            {
                mevcutCurveIndex = 0;
                hedefIndex = 0;
            }
            else
            {
                hareketHalinde = false;
                return;
            }
        }

        aktifHedef = curvePath[hedefIndex];

        // Hedefe olan yön (Guide transform ile)
        Vector3 hedefYon = (aktifHedef - transform.position);
        hedefYon.y = 0;
        hedefYon.Normalize();

        Vector3 arabaOnu = ArabaOnuYonu();
        arabaOnu.y = 0;
        arabaOnu.Normalize();

        float aci = Vector3.SignedAngle(arabaOnu, hedefYon, Vector3.up);
        float aciMutlak = Mathf.Abs(aci);

        // Drift kontrolü
        driftYapiyorMu = aciMutlak > driftEsikAcisi;

        // Direksiyon hesaplama
        float hedefDireksiyon = Mathf.Clamp(aci / direksiyon, -1f, 1f);
        
        if (driftYapiyorMu)
        {
            // Drift modunda daha agresif direksiyon
            hedefDireksiyon *= 1.3f;
            mevcutDireksiyon = Mathf.Lerp(mevcutDireksiyon, hedefDireksiyon, Time.fixedDeltaTime * 8f);
        }
        else
        {
            mevcutDireksiyon = Mathf.Lerp(mevcutDireksiyon, hedefDireksiyon, Time.fixedDeltaTime * 5f);
        }

        // Motor gücü
        float mevcutHiz = rb.linearVelocity.magnitude;
        float hedefHiz = driftYapiyorMu ? maksHiz * driftHizCarpani : maksHiz;
        
        if (mevcutHiz < hedefHiz)
        {
            float hizCarpani = driftYapiyorMu ? 1.1f : Mathf.Lerp(1f, 0.5f, aciMutlak / 90f);
            mevcutMotor = motorGucu * hizCarpani;
        }
        else
        {
            mevcutMotor = motorGucu * 0.3f; // Hafif gaz
        }
    }

    private void FizikselHareket()
    {
        // Araba önü yönünü hesapla (Guide transform'dan)
        Vector3 arabaOnu = ArabaOnuYonu();
        
        // Motor kuvveti (sürekli ileri git)
        Vector3 motorKuvveti = arabaOnu * mevcutMotor;
        rb.AddForce(motorKuvveti, ForceMode.Force);

        // Direksiyon ile dönüş (hız varsa)
        float mevcutHiz = rb.linearVelocity.magnitude;
        if (Mathf.Abs(mevcutDireksiyon) > 0.01f && mevcutHiz > 1f)
        {
            // Hıza bağlı dönüş - hızlı giderse daha etkili döner
            float donusEtkisi = Mathf.Clamp01(mevcutHiz / (maksHiz * 0.5f));
            float donusTorku = mevcutDireksiyon * direksiyon * donusEtkisi;
            rb.AddTorque(transform.up * donusTorku * 2f, ForceMode.Acceleration);
        }

        // Yanal sürtünme ve drift (Guide'ın sağ/sol yönü)
        Vector3 yanYon = arabaOnuGuide != null ? arabaOnuGuide.right : transform.forward;
        Vector3 yanHiz = yanYon * Vector3.Dot(rb.linearVelocity, yanYon);
        
        if (driftYapiyorMu && mevcutHiz > 5f)
        {
            // Drift: Yan kayma azaltılır ama tamamen yok edilmez
            rb.AddForce(-yanHiz * driftGucu * driftStabilizasyon, ForceMode.Acceleration);
            
            // Drift sırasında ek yan kuvvet
            Vector3 driftKuvveti = yanYon * mevcutDireksiyon * 300f;
            rb.AddForce(driftKuvveti, ForceMode.Force);
        }
        else
        {
            // Normal: Yanal kaymayı önle
            rb.AddForce(-yanHiz * 3.5f, ForceMode.Acceleration);
        }
    }

    private void Frenle()
    {
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        driftYapiyorMu = false;
    }

    private void TekerlekleriGuncelle()
    {
        if (tekerlekler == null) return;

        float donusHizi = (rb.linearVelocity.magnitude / (2 * Mathf.PI * tekerlekYaricapi)) * 360f * Time.fixedDeltaTime;
        
        for (int i = 0; i < tekerlekler.Length; i++)
        {
            if (tekerlekler[i] != null)
            {
                if (i < 2) // Ön tekerlekler
                {
                    float aci = mevcutDireksiyon * direksiyon;
                    tekerlekler[i].localRotation = Quaternion.Euler(0, aci, 0);
                }
                
                // Drift sırasında tekerlekler daha hızlı döner
                float driftCarpan = driftYapiyorMu ? 1.3f : 1f;
                tekerlekler[i].Rotate(donusHizi * driftCarpan, 0, 0);
            }
        }
    }

    public void RotayaBasla()
    {
        if (waypoints.Count < 2)
        {
            Debug.LogWarning("En az 2 waypoint gerekli!");
            return;
        }

        if (hareketHalinde)
        {
            hareketHalinde = false;
            mevcutCurveIndex = 0;
        }
        else
        {
            CurveRotasiOlustur();
            mevcutCurveIndex = 0;
            hareketHalinde = true;
            geriDonuyor = false;
        }
    }

    public void WaypointleriAyarla(List<Transform> yeniWaypoints)
    {
        waypoints = new List<Transform>(yeniWaypoints);
        CurveRotasiOlustur();
    }

    public void WaypointEkle(Transform waypoint)
    {
        if (!waypoints.Contains(waypoint))
        {
            waypoints.Add(waypoint);
            CurveRotasiOlustur();
        }
    }

    public void WaypointleriTemizle()
    {
        waypoints.Clear();
        curvePath.Clear();
    }

    public void Durdur()
    {
        hareketHalinde = false;
        mevcutMotor = 0f;
        mevcutDireksiyon = 0f;
    }

    public bool DriftYapiyorMu()
    {
        return driftYapiyorMu;
    }

    private void OnDrawGizmos()
    {
        // Guide transform'u göster
        if (arabaOnuGuide != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(arabaOnuGuide.position, arabaOnuGuide.position + arabaOnuGuide.forward * 3f);
            Gizmos.DrawWireSphere(arabaOnuGuide.position + arabaOnuGuide.forward * 3f, 0.5f);
        }
        
        // Curve yolunu çiz
        if (curvePath != null && curvePath.Count > 1)
        {
            for (int i = 0; i < curvePath.Count - 1; i++)
            {
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, i / (float)curvePath.Count);
                Gizmos.DrawLine(curvePath[i], curvePath[i + 1]);
                
                // Her 5. noktada küçük küre
                if (i % 5 == 0)
                {
                    Gizmos.DrawWireSphere(curvePath[i], 0.3f);
                }
            }
        }

        // Waypoint'ler
        if (waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 1f);
                    
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 2, "WP " + i);
#endif
                }
            }
        }

        // Aktif hedef ve mevcut pozisyon
        if (Application.isPlaying && hareketHalinde)
        {
            Gizmos.color = driftYapiyorMu ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, aktifHedef);
            Gizmos.DrawWireSphere(aktifHedef, 1f);
            
            // Arabanın baktığı yön
            Vector3 arabaOnu = ArabaOnuYonu();
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, arabaOnu * 5f);
        }

        // Tekerlekler
        if (tekerlekler != null)
        {
            Gizmos.color = driftYapiyorMu ? Color.red : Color.magenta;
            foreach (var tekerlek in tekerlekler)
            {
                if (tekerlek != null)
                {
                    Gizmos.DrawWireSphere(tekerlek.position, tekerlekYaricapi);
                }
            }
        }
    }
}