using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoToRawImage : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage rawImage;

    void Start()
    {
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.isLooping = true;
        videoPlayer.Play();

        rawImage.texture = videoPlayer.texture;
    }

    void Update()
    {
        if (rawImage.texture != videoPlayer.texture)
        {
            rawImage.texture = videoPlayer.texture;
        }
    }
}