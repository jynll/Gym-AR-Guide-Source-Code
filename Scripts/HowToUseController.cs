using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HowToUseController : MonoBehaviour
{
    [Header("--- TOP BAR ---")]
    public Button btnBack;                  // the ← button directly on Canvas
    public string homeSceneName = "HomeScene";

    [Header("--- PANELS ---")]
    public GameObject panelList;            // your 7-button panel
    public GameObject panelDetail;          // the hidden detail panel

    [Header("--- PANEL LIST BUTTONS (assign manually, 1 to 7) ---")]
    public Button btnStep1;
    public Button btnStep2;
    public Button btnStep3;
    public Button btnStep4;
    public Button btnStep5;
    public Button btnStep6;
    public Button btnStep7;

    [Header("--- PANEL DETAIL CONTENTS ---")]
    public Image uiImageDetail;             // the UIImage inside panelDetail
    public Transform panelDotsContainer;    // the dots container
    public Button btnPrev;
    public Button btnNext;

    // ── Dot visuals ──────────────────────────────────────────────────
    // Populated automatically from panelDotsContainer children at Start()
    private Image[] dots;

    private int currentIndex = 0;
    private const int TOTAL_STEPS = 7;

    // Dot colors
    private readonly Color dotActive   = Color.black;
    private readonly Color dotInactive = new Color(0.75f, 0.75f, 0.75f);

    // ─────────────────────────────────────────────────────────────────

    void Start()
    {
        // --- Back button → HomeScene ---
        btnBack.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));

        // --- List buttons → open detail at that index ---
        btnStep1.onClick.AddListener(() => OpenDetail(0));
        btnStep2.onClick.AddListener(() => OpenDetail(1));
        btnStep3.onClick.AddListener(() => OpenDetail(2));
        btnStep4.onClick.AddListener(() => OpenDetail(3));
        btnStep5.onClick.AddListener(() => OpenDetail(4));
        btnStep6.onClick.AddListener(() => OpenDetail(5));
        btnStep7.onClick.AddListener(() => OpenDetail(6));

        // --- Prev / Next inside panelDetail ---
        btnPrev.onClick.AddListener(OnPrev);
        btnNext.onClick.AddListener(OnNext);

        // --- Collect dot Image components from panelDotsContainer ---
        dots = new Image[panelDotsContainer.childCount];
        for (int i = 0; i < panelDotsContainer.childCount; i++)
            dots[i] = panelDotsContainer.GetChild(i).GetComponent<Image>();

        // --- Always start showing the list ---
        ShowList();
    }

    // ── Called when a list button is clicked ─────────────────────────
    void OpenDetail(int index)
    {
        currentIndex = index;
        panelList.SetActive(false);
        panelDetail.SetActive(true);
        RefreshDetail();
    }

    // ── Refresh image + dots to match currentIndex ───────────────────
    void RefreshDetail()
    {
        // Images are named DetailedHowTo1 … DetailedHowTo7
        // They live in  Assets/Resources/HowToDetails/
        // Resources.Load uses the path RELATIVE to any Resources folder,
        // without the file extension.
        string imageName = "HowToDetails/DetailedHowTo" + (currentIndex + 1);
        Sprite loaded = Resources.Load<Sprite>(imageName);

        if (loaded != null)
        {
            uiImageDetail.sprite = loaded;
        }
        else
        {
            uiImageDetail.sprite = null;   // shows nothing until image is added
            Debug.LogWarning("HowToUseController: Could not load sprite at Resources/"
                             + imageName
                             + " — make sure the file exists and Texture Type = Sprite (2D and UI)");
        }

        // Update dots
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
                dots[i].color = (i == currentIndex) ? dotActive : dotInactive;
        }
    }

    // ── Prev button ───────────────────────────────────────────────────
    void OnPrev()
    {
        if (currentIndex == 0)
        {
            // Already on first — go back to list
            ShowList();
        }
        else
        {
            currentIndex--;
            RefreshDetail();
        }
    }

    // ── Next button ───────────────────────────────────────────────────
    void OnNext()
    {
        if (currentIndex >= TOTAL_STEPS - 1)
        {
            // Last step — go back to list
            ShowList();
        }
        else
        {
            currentIndex++;
            RefreshDetail();
        }
    }

    // ── Show list, hide detail ────────────────────────────────────────
    void ShowList()
    {
        panelList.SetActive(true);
        panelDetail.SetActive(false);
    }
}