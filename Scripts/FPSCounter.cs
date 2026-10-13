using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text fpsText;

    [Header("Settings")]
    public float updateInterval = 0.5f; // I-update ang text tuwing 0.5s para readable

    private float accum = 0f;
    private int frames = 0;
    private float timeleft;

    void Start()
    {
        if (fpsText == null)
            fpsText = GetComponent<TMP_Text>();

        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string format = System.String.Format("{0:F1} FPS", fps);
            
            if (fpsText != null)
            {
                fpsText.text = format;

                // Color coding para sa performance check
                if (fps >= 25)
                    fpsText.color = Color.green;
                else if (fps >= 15)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
            }

            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}