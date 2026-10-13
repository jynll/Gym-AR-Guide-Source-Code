using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class GuideCard
{
    [Header("Option A — Animated Card (MP4 from Canva)")]
    public VideoClip cardVideoClip;

    [Header("Option B — Static Card (PNG from Canva)")]
    public Sprite cardImage;

    // How to use:
    // If you assign an MP4 to cardVideoClip → it plays the video
    // If you only assign a PNG to cardImage → it shows the image
    // You can mix per card (some cards MP4, some PNG)
}