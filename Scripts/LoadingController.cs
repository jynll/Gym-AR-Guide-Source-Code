using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class LoadingController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoClip loadingClip;
    
    // Pinalitan natin ang default papunta sa Details scene
    public string nextSceneName = "Details"; 

    void Start()
    {
        if (videoPlayer != null && loadingClip != null)
        {
            videoPlayer.clip = loadingClip;
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("Video Player o Loading Clip ay hindi naka-assign!");
            // Kung walang video, lumipat na agad para hindi ma-stuck
            SceneManager.LoadScene(nextSceneName); 
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}