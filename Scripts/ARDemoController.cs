using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.SceneManagement;

public class ARDemoController : MonoBehaviour
{
    [Header("--- MACHINE DATA ---")]
    public MachineGuideData[] allMachineGuides;

    [Header("--- AR COMPONENTS ---")]
    public ARRaycastManager arRaycastManager;

    [Header("--- UI: TOP ---")]
    public TMP_Text txtARMachineName;

    [Header("--- UI: HINTS ---")]
    public GameObject txtPlacementHint;
    public GameObject txtRotateHint;

    [Header("--- UI: RIGHT BUTTONS ---")]
    public Button btnPausePlay;
    public TMP_Text txtPausePlayLabel;
    public Button btnReposition;

    [Header("--- UI: BOTTOM BUTTONS ---")]
    public Button btnShowGuideCards;
    public Button btnDoneWithSet;

    [Header("--- UI: WORKOUT INFO ---")]
    public Button btnRepMinus;
    public Button btnRepPlus;
    public TMP_Text txtRepCount;
    public Button btnWeightMinus;
    public Button btnWeightPlus;
    public TMP_Text txtWeightCount;
    public TMP_Text txtSetsCount;

    [Header("--- SCENE NAMES ---")]
    public string guideCardsSceneName = "GuideCardsScene";
    public string sessionCompleteSceneName = "SessionCompleteScene";

    [Header("--- WORKOUT SETTINGS ---")]
    public int totalSets = 3;
    public int defaultReps = 10;
    public int defaultWeightKg = 20;
    public int weightStepKg = 3; // 2.5 rounded to 3 for int; change to float if needed

    // Private state
    private MachineGuideData currentMachine;
    private GameObject placedModel;
    private Animator modelAnimator;
    private bool isPlaced = false;
    private bool isAnimationPlaying = true;
    private bool isRepositioning = false;

    private int currentReps;
    private int currentWeightKg;
    private int setsCompleted = 0;

    private Vector2 swipeDragStart;
    private bool isDraggingModel = false;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        LoadMachineData();
        SetupWorkoutDefaults();
        SetupButtonListeners();
        UpdateWorkoutUI();

        // Start with placement hints visible
        txtPlacementHint.SetActive(true);
        txtRotateHint.SetActive(false);
        btnPausePlay.gameObject.SetActive(false);
        btnReposition.gameObject.SetActive(false);
    }

    void LoadMachineData()
    {
        string id = WorkoutSession.SelectedMachineId;
        foreach (MachineGuideData m in allMachineGuides)
        {
            if (m.machineId == id)
            {
                currentMachine = m;
                break;
            }
        }

        if (currentMachine == null)
        {
            Debug.LogError("ARDemoController: No machine found for ID: " + id);
            return;
        }

        txtARMachineName.text = currentMachine.machineDisplayName;
    }

    void SetupWorkoutDefaults()
    {
        currentReps = defaultReps;
        currentWeightKg = defaultWeightKg;
        setsCompleted = WorkoutSession.CompletedSets; // carry over if returning from guide cards
    }

    void SetupButtonListeners()
    {
        btnPausePlay.onClick.AddListener(OnPausePlayPressed);
        btnReposition.onClick.AddListener(OnRepositionPressed);
        btnShowGuideCards.onClick.AddListener(OnShowGuideCardsPressed);
        btnDoneWithSet.onClick.AddListener(OnDoneWithSetPressed);

        btnRepMinus.onClick.AddListener(() => ChangeReps(-1));
        btnRepPlus.onClick.AddListener(() => ChangeReps(1));
        btnWeightMinus.onClick.AddListener(() => ChangeWeight(-weightStepKg));
        btnWeightPlus.onClick.AddListener(() => ChangeWeight(weightStepKg));
    }

    void Update()
    {
        HandleTouchInput();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        // Ignore touches on UI
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (!isPlaced || isRepositioning)
        {
            // TAP TO PLACE
            if (touch.phase == TouchPhase.Began)
            {
                TryPlaceModel(touch.position);
            }
        }
        else
        {
            // SWIPE TO ROTATE
            if (touch.phase == TouchPhase.Began)
            {
                swipeDragStart = touch.position;
                isDraggingModel = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDraggingModel && placedModel != null)
            {
                float deltaX = touch.position.x - swipeDragStart.x;
                placedModel.transform.Rotate(Vector3.up, -deltaX * 0.3f, Space.World);
                swipeDragStart = touch.position;

                // Start animation automatically once user starts rotating
                if (!isAnimationPlaying && modelAnimator != null)
                {
                    modelAnimator.enabled = true;
                    modelAnimator.speed = 1f;
                    isAnimationPlaying = true;
                    txtPausePlayLabel.text = "⏸";
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDraggingModel = false;
            }
        }
    }

    void TryPlaceModel(Vector2 screenPosition)
{
    if (arRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
    {
        Pose hitPose = hits[0].pose;

        if (placedModel == null)
        {
            if (currentMachine == null || currentMachine.arMachinePrefab == null)
            {
                Debug.LogWarning("ARDemoController: No AR prefab assigned for "
                                 + WorkoutSession.SelectedMachineId);
                return;
            }

            placedModel = Instantiate(currentMachine.arMachinePrefab,
                                      hitPose.position, hitPose.rotation);

            modelAnimator = placedModel.GetComponent<Animator>();
            if (modelAnimator == null)
                modelAnimator = placedModel.GetComponentInChildren<Animator>();

            // STOP animation on spawn — user rotates first
            if (modelAnimator != null)
            {
                modelAnimator.enabled = false;
                modelAnimator.speed = 0f;
            }
        }
        else
        {
            // Repositioning — move model, keep animation paused
            placedModel.transform.position = hitPose.position;
            placedModel.transform.rotation = hitPose.rotation;
        }

        isPlaced = true;
        isRepositioning = false;
        isAnimationPlaying = false; // animation off until user rotates

        txtPlacementHint.SetActive(false);
        txtRotateHint.SetActive(true);     // show "Swipe to Rotate" hint
        btnPausePlay.gameObject.SetActive(true);
        btnReposition.gameObject.SetActive(true);

        // Pause/Play button shows PLAY since animation is not playing yet
        txtPausePlayLabel.text = "▶";
    }
}

    void OnPausePlayPressed()
    {
        if (modelAnimator == null) return;

        isAnimationPlaying = !isAnimationPlaying;
        modelAnimator.enabled = isAnimationPlaying;
        txtPausePlayLabel.text = isAnimationPlaying ? "⏸" : "▶";
    }

    void OnRepositionPressed()
    {
        isRepositioning = true;
        txtPlacementHint.SetActive(true);
        txtRotateHint.SetActive(false);
    }

    void OnShowGuideCardsPressed()
    {
        // Save current workout state before leaving
        WorkoutSession.CompletedSets = setsCompleted;
        WorkoutSession.TotalReps = setsCompleted * currentReps;
        WorkoutSession.WeightUsedKg = currentWeightKg;
        SceneManager.LoadScene(guideCardsSceneName);
    }

    void OnDoneWithSetPressed()
    {
        setsCompleted++;
        WorkoutSession.CompletedSets = setsCompleted;
        WorkoutSession.TotalReps = setsCompleted * currentReps;
        WorkoutSession.WeightUsedKg = currentWeightKg;

        UpdateWorkoutUI();

        if (setsCompleted >= totalSets)
        {
            SceneManager.LoadScene(sessionCompleteSceneName);
        }
    }

    void ChangeReps(int delta)
    {
        currentReps = Mathf.Max(1, currentReps + delta);
        UpdateWorkoutUI();
    }

    void ChangeWeight(int delta)
    {
        currentWeightKg = Mathf.Max(0, currentWeightKg + delta);
        UpdateWorkoutUI();
    }

    void UpdateWorkoutUI()
    {
        txtRepCount.text = currentReps.ToString();
        txtWeightCount.text = currentWeightKg.ToString();
        txtSetsCount.text = setsCompleted + " / " + totalSets + " Sets";
    }
}