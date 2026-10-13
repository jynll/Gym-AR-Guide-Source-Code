using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class GuideCardController : MonoBehaviour
{
    [Header("--- MACHINE DATA ---")]
    [Tooltip("Drag all 11 MachineGuideData assets here")]
    public MachineGuideData[] allMachineGuides;

    [Header("--- TOP BAR ---")]
    public TMP_Text txtMachineName;
    
    // [BAGONG ADD]: Button para sa Top-Left Arrow (Exit Scene)
    public Button btnExitScene; 

    [Header("--- CARD DISPLAY ---")]
    public VideoPlayer videoPlayer;
    public RawImage rawImgVideo;
    public Image imgCardStatic;          // shows PNG if no video assigned

    [Header("--- DOT INDICATORS ---")]
    public Transform panelDotsContainer;
    public GameObject dotPrefab;
    public Color dotActiveColor = Color.black;
    public Color dotInactiveColor = new Color(0.75f, 0.75f, 0.75f);

    [Header("--- BOTTOM BUTTONS ---")]
    public Button btnNext;
    public Button btnBack;              // Ito yung "BACK" card button sa ibaba
    public TMP_Text txtNextLabel;       // the TMP Text child inside btnNext

    public Button btnSkip; 

    [Header("--- SCENE NAMES ---")]
    public string arDemoSceneName = "ARDemoScene";
    public string ar2DemoSceneName = "AR2DemoScene";
    public string detailsSceneName = "StartWorkout"; // Babalikang scene

    // Private
    private MachineGuideData currentMachine;
    private int currentCardIndex = 0;
    private List<Image> spawnedDots = new List<Image>();
    private RenderTexture videoRT;

    void Start()
    {
        LoadMachineData();
        BuildDots();
        ShowCard(0);

        // [BAGONG ADD]: Listener para sa Top-Left Arrow
        if (btnExitScene != null) btnExitScene.onClick.AddListener(OnExitScenePressed);

        if (btnNext != null) btnNext.onClick.AddListener(OnNextPressed);
        if (btnBack != null) btnBack.onClick.AddListener(OnBackPressed);
        if (btnSkip != null) btnSkip.onClick.AddListener(OnSkipPressed);
    }

    void LoadMachineData()
    {
        string id = WorkoutSession.SelectedMachineId;

        if (!string.IsNullOrEmpty(id))
        {
            // Tatanggalin ang "machine", space, at underscore para siguradong saktong-sakto ang match
            string cleanSessionId = id.ToLower().Replace("machine", "").Replace(" ", "").Replace("_", "").Trim();

            foreach (MachineGuideData m in allMachineGuides)
            {
                if (m != null && !string.IsNullOrEmpty(m.machineId))
                {
                    string cleanGuideId = m.machineId.ToLower().Replace("machine", "").Replace(" ", "").Replace("_", "").Trim();
                    
                    if (cleanGuideId == cleanSessionId)
                    {
                        currentMachine = m;
                        break;
                    }
                }
            }
        }

        // Kung walang nahanap, gagamitin ang unang nasa listahan (Fallback)
        if (currentMachine == null)
        {
            if (allMachineGuides != null && allMachineGuides.Length > 0 && allMachineGuides[0] != null)
            {
                Debug.LogWarning($"Machine ID '{id}' not found. Falling back to '{allMachineGuides[0].machineDisplayName}'.");
                currentMachine = allMachineGuides[0];
                WorkoutSession.SelectedMachineId = currentMachine.machineId; 
            }
            else
            {
                Debug.LogError("GuideCardController: Critical Error - The 'All Machine Guides' array is empty! Please assign them in the Inspector.");
                return;
            }
        }

        if (txtMachineName != null)
            txtMachineName.text = currentMachine.machineDisplayName;
    }

    void BuildDots()
    {
        foreach (Image d in spawnedDots) Destroy(d.gameObject);
        spawnedDots.Clear();

        if (currentMachine == null) return;

        for (int i = 0; i < currentMachine.guideCards.Length; i++)
        {
            GameObject dot = Instantiate(dotPrefab, panelDotsContainer);
            Image dotImg = dot.GetComponent<Image>();
            spawnedDots.Add(dotImg);
        }

        RefreshDots();
    }

    void RefreshDots()
    {
        for (int i = 0; i < spawnedDots.Count; i++)
            spawnedDots[i].color = (i == currentCardIndex) ? dotActiveColor : dotInactiveColor;
    }

    void ShowCard(int index)
    {
        if (currentMachine == null) return;

        currentCardIndex = Mathf.Clamp(index, 0, currentMachine.guideCards.Length - 1);
        GuideCard card = currentMachine.guideCards[currentCardIndex];

        if (card.cardVideoClip != null)
        {
            if (rawImgVideo != null) rawImgVideo.gameObject.SetActive(true);
            if (imgCardStatic != null) imgCardStatic.gameObject.SetActive(false);
            PlayVideo(card.cardVideoClip);
        }
        else if (card.cardImage != null)
        {
            if (rawImgVideo != null) rawImgVideo.gameObject.SetActive(false);
            if (imgCardStatic != null)
            {
                imgCardStatic.gameObject.SetActive(true);
                imgCardStatic.sprite = card.cardImage;
            }
            if (videoPlayer != null) videoPlayer.Stop();
        }
        else
        {
            if (rawImgVideo != null) rawImgVideo.gameObject.SetActive(false);
            if (imgCardStatic != null) imgCardStatic.gameObject.SetActive(false);
            if (videoPlayer != null) videoPlayer.Stop();
        }

        RefreshDots();
        UpdateNextButtonLabel();
    }

    void PlayVideo(VideoClip clip)
    {
        if (videoPlayer == null) return;

        if (videoRT == null)
        {
            videoRT = new RenderTexture(1024, 1024, 0);
            videoPlayer.targetTexture = videoRT;
            if (rawImgVideo != null) rawImgVideo.texture = videoRT;
        }

        videoPlayer.clip = clip;
        videoPlayer.isLooping = true;
        videoPlayer.Stop();
        videoPlayer.Play();
    }

    void UpdateNextButtonLabel()
    {
        if (txtNextLabel == null || currentMachine == null) return;
        bool isLast = currentCardIndex == currentMachine.guideCards.Length - 1;
        txtNextLabel.text = isLast ? "AR DEMO" : "NEXT";
    }

    // --- BUTTON FUNCTIONS ---

    public void OnNextPressed()
    {
        if (currentMachine == null) return;
        bool isLast = currentCardIndex == currentMachine.guideCards.Length - 1;
        if (isLast)
        {
            SceneManager.LoadScene(arDemoSceneName);
        }
        else
        {
            ShowCard(currentCardIndex + 1);
        }
    }

    public void OnBackPressed()
    {
        if (currentCardIndex == 0)
        {
            // Kung unang card na at nag-back pa, babalik sa scene
            SceneManager.LoadScene(detailsSceneName);
        }
        else
        {
            // Kung hindi, babalik lang sa nakaraang card
            ShowCard(currentCardIndex - 1);
        }
    }

    // [BAGONG METHOD]: Para sa Top-Left Arrow
    public void OnExitScenePressed() 
    {
        SceneManager.LoadScene(detailsSceneName);
    }

    public void OnSwipeLeft() => OnNextPressed();
    public void OnSwipeRight() => OnBackPressed();

    public void OnSkipPressed()
    {
        SceneManager.LoadScene("AR2Demo");
    }
}