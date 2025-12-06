using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
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
    [SerializeField] private float waypointToleransi = 3f;

    [Header("NavMesh Ayarları")]
    [SerializeField] private float hiz = 15f;
    [SerializeField] private float donusHizi = 180f;
    [SerializeField] private float ivmelenme = 8f;

    [Header("Fizik Ayarları")]
    [SerializeField] private LayerMask zeminMask;
    [SerializeField] private float zeminMesafesi = 2f;
    [SerializeField] private float supensiyon = 2500f;
    [SerializeField] private float suspensiyonDamper = 300f;
    [SerializeField] private float suspensiyonHedefYukseklik = 1f;

    [Header("Drift Ayarları")]
    [SerializeField] private float driftEsikAcisi = 35f;
    [SerializeField] private float driftGucu = 0.6f;
    [SerializeField] private float driftStabilizasyon = 2f;

    [Header("Tekerlekler")]
    [SerializeField] private Transform[] tekerlekler;
    [SerializeField] private float tekerlekYaricapi = 0.3f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Vector3 baslangicPozisyonu;
    private Quaternion baslangicRotasyonu;
    private int mevcutWaypointIndex = 0;
    private bool hareketHalinde = false;
    private bool geriDonuyor = false;
    private bool driftYapiyorMu = false;
    private float mevcutDireksiyon = 0f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        KurulumuYap();
    }

    private void KurulumuYap()
    {
        // NavMeshAgent ayarları
        agent.speed = hiz;
        agent.angularSpeed = donusHizi;
        agent.acceleration = ivmelenme;
        agent.stoppingDistance = waypointToleransi;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        
        // Rigidbody ayarları
        rb.mass = 1500f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0, -0.8f, 0);
        rb.maxAngularVelocity = 7f;
        
        // NavMeshAgent için kinematic değil
        rb.isKinematic = false;
        
        // Guide kontrolü
        if (arabaOnuGuide == null)
        {
            Debug.LogWarning("Araba Onu Guide tanımlanmamış! Inspector'dan arabanın önündeki guide objesini atayın.");
        }
    }

    private void Start()
    {
        baslangicPozisyonu = transform.position;
        baslangicRotasyonu = transform.rotation;
    }

    private void Update()
    {
        // Test tuşları
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotayaBasla();
            Debug.Log("Rota başlatıldı/durduruldu");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Durdur();
            Debug.Log("Araba durduruldu");
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Hareket: {hareketHalinde} | Drift: {driftYapiyorMu} | Hız: {agent.velocity.magnitude:F1} m/s | Waypoint: {mevcutWaypointIndex}/{waypoints.Count}");
        }

        if (hareketHalinde)
        {
            WaypointKontrolu();
            DriftKontrolu();
        }
    }

    private void FixedUpdate()
    {
        if (hareketHalinde)
        {
            SuspensiyonSistemi();
            FizikDrift();
        }
        
        TekerlekleriGuncelle();
    }

    private void WaypointKontrolu()
    {
        if (waypoints.Count == 0 || !agent.enabled) return;

        // Hedefe ulaştı mı? (Sadece XZ düzleminde kontrol et)
        Vector3 hedefPozisyon = waypoints[mevcutWaypointIndex].position;
        Vector3 arabaPozisyon = transform.position;
        
        // Y eksenini yok say, sadece yatay mesafe
        float yatayMesafe = Vector2.Distance(
            new Vector2(arabaPozisyon.x, arabaPozisyon.z),
            new Vector2(hedefPozisyon.x, hedefPozisyon.z)
        );

        if (yatayMesafe <= waypointToleransi)
        {
            if (geriDonuyor)
            {
                // Geri dönerken
                mevcutWaypointIndex--;
                
                if (mevcutWaypointIndex < 0)
                {
                    // Başlangıca ulaştı
                    hareketHalinde = false;
                    geriDonuyor = false;
                    mevcutWaypointIndex = 0;
                    agent.isStopped = true;
                    return;
                }
            }
            else
            {
                // İleri giderken
                mevcutWaypointIndex++;
                
                if (mevcutWaypointIndex >= waypoints.Count)
                {
                    if (donguselRota)
                    {
                        mevcutWaypointIndex = 0;
                    }
                    else
                    {
                        hareketHalinde = false;
                        mevcutWaypointIndex = waypoints.Count - 1;
                        agent.isStopped = true;
                        return;
                    }
                }
            }
            
            // Yeni hedefe git (Y eksenini arabanın Y'si ile değiştir)
            if (mevcutWaypointIndex >= 0 && mevcutWaypointIndex < waypoints.Count)
            {
                Vector3 yeniHedef = waypoints[mevcutWaypointIndex].position;
                yeniHedef.y = transform.position.y; // Arabanın kendi yüksekliğini kullan
                agent.SetDestination(yeniHedef);
            }
        }
    }

    private void DriftKontrolu()
    {
        if (!agent.hasPath) return;

        // Arabanın önü ve hedef yön
        Vector3 arabaOnu = ArabaOnuYonu();
        arabaOnu.y = 0;
        arabaOnu.Normalize();

        Vector3 hedefYon = agent.steeringTarget - transform.position;
        hedefYon.y = 0;
        hedefYon.Normalize();

        // Dönüş açısı
        float aci = Vector3.SignedAngle(arabaOnu, hedefYon, Vector3.up);
        float aciMutlak = Mathf.Abs(aci);

        // Drift kontrolü
        driftYapiyorMu = aciMutlak > driftEsikAcisi && agent.velocity.magnitude > 5f;
        
        // Direksiyon hesaplama
        mevcutDireksiyon = Mathf.Clamp(aci / 45f, -1f, 1f);
    }

    private void FizikDrift()
    {
        if (!driftYapiyorMu) return;

        // Drift sırasında yanal kayma efekti
        Vector3 yanYon = arabaOnuGuide != null ? arabaOnuGuide.right : transform.forward;
        Vector3 yanHiz = yanYon * Vector3.Dot(rb.linearVelocity, yanYon);
        
        // Yanal sürtünmeyi azalt (kayma efekti)
        rb.AddForce(-yanHiz * driftGucu * driftStabilizasyon, ForceMode.Acceleration);
        
        // Drift sırasında ek yan kuvvet
        Vector3 driftKuvveti = yanYon * mevcutDireksiyon * 250f;
        rb.AddForce(driftKuvveti, ForceMode.Force);
    }

    private void SuspensiyonSistemi()
    {
        int zeminTemasSayisi = 0;
        Vector3 ortalamaNormal = Vector3.zero;
        
        // Her tekerlek için suspensyon
        for (int i = 0; i < 4; i++)
        {
            if (tekerlekler != null && i < tekerlekler.Length && tekerlekler[i] != null)
            {
                RaycastHit hit;
                Vector3 tekerlekPos = tekerlekler[i].position;
                
                if (Physics.Raycast(tekerlekPos, -transform.up, out hit, zeminMesafesi, zeminMask))
                {
                    zeminTemasSayisi++;
                    ortalamaNormal += hit.normal;
                    
                    // Suspensyon kuvveti
                    float mesafe = hit.distance;
                    float uzunlukFarki = suspensiyonHedefYukseklik - mesafe;
                    
                    Vector3 tekerlekHizi = rb.GetPointVelocity(tekerlekPos);
                    float dikeyhiz = Vector3.Dot(transform.up, tekerlekHizi);
                    
                    float yayKuvveti = uzunlukFarki * supensiyon;
                    float sonumlemeKuvveti = dikeyhiz * suspensiyonDamper;
                    float toplamKuvvet = yayKuvveti - sonumlemeKuvveti;
                    
                    rb.AddForceAtPosition(transform.up * toplamKuvvet, tekerlekPos, ForceMode.Force);
                    
                    Debug.DrawLine(tekerlekPos, hit.point, driftYapiyorMu ? Color.red : Color.green);
                }
            }
        }

        // Zemin normaline göre döndür
        if (zeminTemasSayisi > 0)
        {
            ortalamaNormal /= zeminTemasSayisi;
            ortalamaNormal.Normalize();
            
            Quaternion hedefRot = Quaternion.FromToRotation(transform.up, ortalamaNormal) * transform.rotation;
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, hedefRot, Time.fixedDeltaTime * 1.5f));
        }
        
        // Anti-roll stabilizasyon
        float yatmaAcisi = Vector3.Angle(transform.up, Vector3.up);
        if (yatmaAcisi > 35f)
        {
            Vector3 duzeltmeTorku = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(duzeltmeTorku * 500f, ForceMode.Force);
        }
    }

    private void TekerlekleriGuncelle()
    {
        if (tekerlekler == null) return;

        float hiz = agent.enabled ? agent.velocity.magnitude : rb.linearVelocity.magnitude;
        float donusHizi = (hiz / (2 * Mathf.PI * tekerlekYaricapi)) * 360f * Time.deltaTime;
        
        for (int i = 0; i < tekerlekler.Length; i++)
        {
            if (tekerlekler[i] != null)
            {
                // Ön tekerlekler direksiyon
                if (i < 2)
                {
                    float aci = mevcutDireksiyon * 35f;
                    tekerlekler[i].localRotation = Quaternion.Euler(0, aci, 0);
                }
                
                // Drift efekti
                float driftCarpan = driftYapiyorMu ? 1.3f : 1f;
                tekerlekler[i].Rotate(donusHizi * driftCarpan, 0, 0);
            }
        }
    }

    private Vector3 ArabaOnuYonu()
    {
        if (arabaOnuGuide != null)
        {
            return arabaOnuGuide.forward;
        }
        else
        {
            return transform.forward;
        }
    }

    public void RotayaBasla()
    {
        if (waypoints.Count == 0)
        {
            Debug.LogWarning("Hiç waypoint tanımlanmamış!");
            return;
        }

        if (hareketHalinde)
        {
            // Geri dön
            GeriDon();
        }
        else
        {
            // Rotaya başla
            mevcutWaypointIndex = 0;
            hareketHalinde = true;
            geriDonuyor = false;
            agent.isStopped = false;
            
            // İlk hedefi ayarla (Y eksenini arabanın Y'si ile)
            Vector3 hedef = waypoints[0].position;
            hedef.y = transform.position.y;
            agent.SetDestination(hedef);
        }
    }

    public void GeriDon()
    {
        if (waypoints.Count == 0) return;
        
        geriDonuyor = true;
        hareketHalinde = true;
        agent.isStopped = false;
        
        if (mevcutWaypointIndex >= waypoints.Count)
        {
            mevcutWaypointIndex = waypoints.Count - 1;
        }
        
        // Hedefi ayarla (Y eksenini arabanın Y'si ile)
        Vector3 hedef = waypoints[mevcutWaypointIndex].position;
        hedef.y = transform.position.y;
        agent.SetDestination(hedef);
    }

    public void Durdur()
    {
        hareketHalinde = false;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public void WaypointleriAyarla(List<Transform> yeniWaypoints)
    {
        waypoints = new List<Transform>(yeniWaypoints);
        mevcutWaypointIndex = 0;
    }

    public void WaypointEkle(Transform waypoint)
    {
        if (!waypoints.Contains(waypoint))
        {
            waypoints.Add(waypoint);
        }
    }

    public void WaypointleriTemizle()
    {
        waypoints.Clear();
        mevcutWaypointIndex = 0;
    }

    public bool HareketHalindeMi()
    {
        return hareketHalinde;
    }

    public bool DriftYapiyorMu()
    {
        return driftYapiyorMu;
    }

    private void OnDrawGizmos()
    {
        // Guide transform
        if (arabaOnuGuide != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(arabaOnuGuide.position, arabaOnuGuide.position + arabaOnuGuide.forward * 3f);
            Gizmos.DrawWireSphere(arabaOnuGuide.position + arabaOnuGuide.forward * 3f, 0.5f);
        }

        // NavMesh yolu
        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = driftYapiyorMu ? Color.red : Color.yellow;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
            
            // Steering target
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(agent.steeringTarget, 0.5f);
        }

        // Waypoint'ler
        if (waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, waypointToleransi);
                    
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 2, "WP " + i);
#endif
                    
                    // Waypoint bağlantıları
                    if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
            }
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