using System;
using UnityEngine;

public class SandevistanController : MonoBehaviour
{
    [SerializeField] KeyCode k_sandevistan=KeyCode.E;
    public float PlayerTimeMultiplier { get; private set; } = 1f;
    public bool SandyOn = false;
    void Start()
    {
        
    }

    
    void Update()
    {
        if (Input.GetKeyDown(k_sandevistan))
        {
            SandyOn = !SandyOn;
            Time.timeScale = SandyOn ? 0.2f : 1f;
            PlayerTimeMultiplier = Time.timeScale > 0f ? 1f / Time.timeScale : 0f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
        }
    }

    

}
