using UnityEngine;
using System.Collections.Generic;

public class DikeySiralama : MonoBehaviour
{
    public List<Transform> objeler; // Sıralanacak objeleri buraya sürükle
    public float bosluk = 1.5f;     // Aralarındaki mesafe

    void Start()
    {
        Sirala();
    }

    [ContextMenu("Şimdi Sırala")] // Oyun çalışmadan test etmek için
    void Sirala()
    {
        for (int i = 0; i < objeler.Count; i++)
        {
            if (objeler[i] != null)
            {
                // İlk objeyi referans alarak diğerlerini altına dizer
                // transform.position.x -> Yatay konumu korur
                // transform.position.y -> Aşağı doğru (negatif yön) sıralar
                
                float yeniY = transform.position.y - (i * bosluk);
                objeler[i].position = new Vector3(transform.position.x, yeniY, 0);
            }
        }
    }
}