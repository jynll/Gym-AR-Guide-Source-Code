using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AboutController : MonoBehaviour
{
    public Button btnBack;
    public string homeSceneName = "HomeScene";

    void Start()
    {
        btnBack.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
    }
}