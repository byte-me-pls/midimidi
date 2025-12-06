using UnityEngine;
using MidiJack;   

public class MidiTestController : MonoBehaviour
{

    [SerializeField] int noteNumber = 60;
    
    [SerializeField] int knobNumber = 1;

    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
        Debug.Log("MidiTest aktif");
    }

    void Update()
    {
    
        for (int n = 0; n < 128; n++)
        {
            float k = MidiMaster.GetKey(n);
            if (k > 0f)
            {
                Debug.Log($"NOTE ON  n={n}  velocity={k}");
            }
        }


        float keyValue = MidiMaster.GetKey(noteNumber);
        if (keyValue > 0f)
        {
          
            float s = 1f + keyValue * 1.5f;
            transform.localScale = baseScale * s;
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 10f);
        }

      
        float knobValue = MidiMaster.GetKnob(knobNumber);
        if (knobValue > 0f)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
          
                Color c = Color.HSVToRGB(knobValue, 1f, 1f);
                rend.material.color = c;
            }
        }
    }
}