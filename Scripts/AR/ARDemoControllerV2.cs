using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ARDemoControllerV2 : MonoBehaviour
{
    [Header("--- AR MANAGER REFERENCE ---")]
    public ARPlacementManager arManager; 

    [Header("--- UI: HINTS & LABELS ---")]
    public TMP_Text txtPlaceHint;          
    public TMP_Text txtARMachine;          
    public TMP_Text txtPTargetMuscle; 

    [Header("--- UI: AR BUTTONS ---")]
    public Button btnPausePlay;
    public TMP_Text txtPausePlayLabel; 
    public Button btnReposition;

    [Header("--- UI: BOTTOM BUTTONS ---")]
    public Button btnShowGuideCards;
    public Button btnDoneWithSet;

    [Header("--- UI: WORKOUT INFO PANEL ---")]
    public GameObject panelWorkoutInfo;
    public Button btnRepMinus;
    public Button btnRepPlus;
    public TMP_Text txtRepCount;
    public Button btnWeightMinus;
    public Button btnWeightPlus;
    public TMP_Text txtWeightCount;
    public TMP_Text txtSetsCount;

    [Header("--- UI: REST AFTER SET PANEL ---")]
    public GameObject panelRestAfterSet;
    public TMP_Text txtRestTimer;          
    public Button btnSkipTimer;            
    public Button btnUndoSet;              

    [Header("--- SCENE NAMES ---")]
    public string guideCardsSceneName = "GuideCardsScene";
    public string sessionCompleteSceneName = "SessionCompleteScene";

    [Header("--- WORKOUT SETTINGS ---")]
    public int totalSets = 3;
    public int defaultReps = 10;
    public int defaultWeightKg = 20;
    public int weightStepKg = 5; // [UPDATED] Changed to 5 KG jumps

    private int currentReps;
    private int currentWeightKg;
    private int setsCompleted = 0;
    private bool isAnimPlaying = true; 

    // Rest Timer Variables
    private float restTimeRemaining = 120f;
    private bool isResting = false;

    void Start()
    {
        if (WorkoutSession.setRepsList != null) WorkoutSession.setRepsList.Clear();
        if (WorkoutSession.setWeightsList != null) WorkoutSession.setWeightsList.Clear();

        SetupWorkoutDefaults();
        LoadSelectedMachineData(); 
        WireButtons();
        UpdateWorkoutUI();

        if (btnPausePlay != null) btnPausePlay.gameObject.SetActive(true);
        if (btnReposition != null) btnReposition.gameObject.SetActive(true);

        if (panelRestAfterSet != null) panelRestAfterSet.SetActive(false);
        if (panelWorkoutInfo != null) panelWorkoutInfo.SetActive(true);
        if (btnDoneWithSet != null) btnDoneWithSet.gameObject.SetActive(true);
    }

    void Update()
    {
        if (arManager != null && txtPlaceHint != null)
        {
            bool shouldShowHint = !arManager.isPlaced; 

            if (txtPlaceHint.gameObject.activeSelf != shouldShowHint)
            {
                txtPlaceHint.gameObject.SetActive(shouldShowHint);
            }
        }

        if (isResting)
        {
            if (restTimeRemaining > 0)
            {
                restTimeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(restTimeRemaining);
            }
            else
            {
                restTimeRemaining = 0;
                isResting = false;
                OnSkipRestPressed(); 
            }
        }
    }

    void SetupWorkoutDefaults()
    {
        currentReps = defaultReps;
        currentWeightKg = defaultWeightKg;
        setsCompleted = WorkoutSession.CompletedSets;
    }

    void LoadSelectedMachineData()
    {
        string currentId = WorkoutSession.SelectedMachineId;
        MachineGuideData matchedGuide = LoadMachineGuideData(currentId);

        if (matchedGuide != null)
        {
            if (txtARMachine != null)
            {
                txtARMachine.text = matchedGuide.machineDisplayName;
            }
        }
        else
        {
            if (txtARMachine != null)
            {
                txtARMachine.text = WorkoutSession.SelectedMachineDisplayName;
            }
        }

        UpdateTargetMuscles(currentId);
    }

    MachineGuideData LoadMachineGuideData(string machineId)
    {
        MachineGuideData[] allGuides = Resources.LoadAll<MachineGuideData>("MachineData");
        if (allGuides.Length == 0) allGuides = Resources.LoadAll<MachineGuideData>("");

        foreach (var guide in allGuides)
        {
            if (guide != null && guide.machineId == machineId) return guide;
        }
        return null;
    }

    void UpdateTargetMuscles(string machineId)
    {
        if (txtPTargetMuscle == null) return;

        string cleanId = machineId != null ? machineId.ToLower().Trim() : "";
        cleanId = cleanId.Replace("machine", "").Trim('_').Trim();

        string primary = "";
        string sec1 = "";
        string sec2 = "";

        switch (cleanId)
        {
            case "chest_press":
            case "chest press":
            case "chess_press":
                primary = "Chest";
                sec1 = "Triceps";
                sec2 = "Outer Shoulders";
                break;
            case "lat_pulldown":
            case "lat pulldown":
                primary = "Mid Back/Lats";
                sec1 = "Biceps";
                sec2 = "Rear Shoulders";
                break;
            case "incline_chest_press":
            case "incline chest press":
                primary = "Upper Chest";
                sec1 = "Triceps";
                sec2 = "Outer Shoulders";
                break;
            case "flat_bench_chest_press":
                primary = "Chest";
                sec1 = "Triceps";
                sec2 = "Outer Shoulders";
                break;
            case "pec_fly":
            case "pec fly":
                primary = "Chest";
                sec1 = "Outer Shoulders";
                sec2 = "";
                break;
            case "shoulder_press":
            case "seated_shoulder_press":
            case "shoulder press":
                primary = "Outer Shoulders";
                sec1 = "Triceps";
                sec2 = "Traps";
                break;
            case "seated_row":
            case "seated row":
                primary = "Mid Back/Lats";
                sec1 = "Biceps";
                sec2 = "Rear Shoulders";
                break;
            case "legs_extension":
            case "legs extension":
            case "leg extension":
            case "leg_extension":
                primary = "Quads/Thighs";
                sec1 = "Hip Flexors";
                sec2 = "";
                break;
            case "leg_press":
            case "leg press":
                primary = "Quads/Thighs";
                sec1 = "Glutes";
                sec2 = "Hamstrings";
                break;
            case "squat":
                primary = "Quads/Thighs";
                sec1 = "Glutes";
                sec2 = "Hamstrings";
                break;
            case "seated_leg_curl":
            case "leg curl":
            case "leg_curl":
                primary = "Hamstrings";
                sec1 = "Calves";
                sec2 = "Glutes";
                break;
            case "preacher_curl":
            case "preacher curl":
                primary = "Biceps";
                sec1 = "Forearm Flexors";
                sec2 = "";
                break;
            case "abductor":
                primary = "Inner Thigh/Adductor";
                sec1 = "Glutes";
                sec2 = "";
                break;
            case "smith":
                primary = "Quads/Thighs";
                sec1 = "Glutes";
                sec2 = "Hamstrings";
                break;
            case "t_bar":
            case "t-bar":
            case "t bar":
                primary = "Mid Back/Lats";
                sec1 = "Biceps";
                sec2 = "Traps";
                break;
            default:
                primary = "General Muscle Group";
                sec1 = "";
                sec2 = "";
                break;
        }

        List<string> muscleList = new List<string>();
        
        if (!string.IsNullOrEmpty(primary)) 
            muscleList.Add(GetColoredMuscleString(primary));
            
        if (!string.IsNullOrEmpty(sec1)) 
            muscleList.Add(GetColoredMuscleString(sec1));
            
        if (!string.IsNullOrEmpty(sec2)) 
            muscleList.Add(GetColoredMuscleString(sec2));

        txtPTargetMuscle.text = "Target Muscles: " + string.Join(", ", muscleList);
    }

    string GetColoredMuscleString(string muscleName)
    {
        Color32 col = GetMuscleColor(muscleName);
        string hexColor = ColorUtility.ToHtmlStringRGB(col);
        return $"<color=#{hexColor}>{muscleName}</color>";
    }

    Color GetMuscleColor(string muscleName)
    {
        string m = muscleName.ToLower();

        if (m.Contains("chest") || m.Contains("pectoralis") || m.Contains("upper chest"))
            return new Color32(219, 53, 39, 255);       
        else if (m.Contains("lat") || m.Contains("mid back"))
            return new Color32(84, 153, 50, 255);       
        else if (m.Contains("quad") || m.Contains("thigh"))
            return new Color32(23, 43, 77, 255);        
        else if (m.Contains("shoulder") || m.Contains("deltoid"))
            return new Color32(31, 111, 235, 255);      
        else if (m.Contains("biceps"))
            return new Color32(186, 85, 211, 255);      
        else if (m.Contains("triceps"))
            return new Color32(92, 40, 18, 255);        
        else if (m.Contains("hamstring"))
            return new Color32(24, 134, 115, 255);      
        else if (m.Contains("glute"))
            return new Color32(115, 59, 150, 255);      
        else if (m.Contains("calf") || m.Contains("calves"))
            return new Color32(139, 0, 0, 255);         
        else if (m.Contains("trap"))
            return new Color32(10, 100, 30, 255);       
        else if (m.Contains("forearm"))
            return new Color32(120, 160, 120, 255);     
        else if (m.Contains("adductor") || m.Contains("inner thigh") || m.Contains("hip flexor"))
            return new Color32(110, 90, 160, 255);      

        return Color.white;
    }

    void WireButtons()
    {
        if (btnPausePlay != null) btnPausePlay.onClick.AddListener(OnPausePlayClicked);
        if (btnReposition != null) btnReposition.onClick.AddListener(OnRepositionClicked);

        if (btnShowGuideCards != null) btnShowGuideCards.onClick.AddListener(OnShowGuideCards);
        if (btnDoneWithSet != null) btnDoneWithSet.onClick.AddListener(OnDoneWithSetPressed);

        if (btnSkipTimer != null) btnSkipTimer.onClick.AddListener(OnSkipRestPressed);
        if (btnUndoSet != null) btnUndoSet.onClick.AddListener(OnUndoSetPressed);

        if (btnRepMinus != null) btnRepMinus.onClick.AddListener(() => ChangeReps(-1));
        if (btnRepPlus != null) btnRepPlus.onClick.AddListener(() => ChangeReps(1));

        if (btnWeightMinus != null) btnWeightMinus.onClick.AddListener(() => ChangeWeight(-weightStepKg));
        if (btnWeightPlus != null) btnWeightPlus.onClick.AddListener(() => ChangeWeight(weightStepKg));
    }

    void OnPausePlayClicked()
    {
        if (arManager != null)
        {
            arManager.ToggleAnimation();
            isAnimPlaying = !isAnimPlaying;
            if (txtPausePlayLabel != null) txtPausePlayLabel.text = isAnimPlaying ? "⏸" : "▶";
        }
    }

    void OnRepositionClicked()
    {
        if (arManager != null)
        {
            arManager.RemoveMachineAndUnlock();
            isAnimPlaying = true;
            if (txtPausePlayLabel != null) txtPausePlayLabel.text = "⏸";
        }
    }

    void OnShowGuideCards()
    {
        WorkoutSession.CompletedSets = setsCompleted;
        WorkoutSession.TotalReps = setsCompleted * currentReps;
        WorkoutSession.WeightUsedKg = currentWeightKg;
        SceneManager.LoadScene(guideCardsSceneName);
    }

    void OnDoneWithSetPressed()
    {
        setsCompleted++;

        if (WorkoutSession.setRepsList == null) WorkoutSession.setRepsList = new List<int>();
        if (WorkoutSession.setWeightsList == null) WorkoutSession.setWeightsList = new List<int>();

        WorkoutSession.setRepsList.Add(currentReps);
        WorkoutSession.setWeightsList.Add(currentWeightKg);

        WorkoutSession.CompletedSets = setsCompleted;
        WorkoutSession.WeightUsedKg = currentWeightKg;
        
        int calculatedTotalReps = 0;
        foreach (int r in WorkoutSession.setRepsList)
        {
            calculatedTotalReps += r;
        }
        WorkoutSession.TotalReps = calculatedTotalReps;

        UpdateWorkoutUI();

        // [UPDATED] We no longer return early here if setsCompleted >= totalSets.
        // We want the rest timer to show up for the final set as well!

        if (panelWorkoutInfo != null) panelWorkoutInfo.SetActive(false);
        if (panelRestAfterSet != null) panelRestAfterSet.SetActive(true);
        if (btnDoneWithSet != null) btnDoneWithSet.gameObject.SetActive(false);

        restTimeRemaining = 120f; 
        isResting = true;
    }

    void OnSkipRestPressed()
    {
        isResting = false;
        
        // [UPDATED] Check if we finished all sets. If yes, move to the complete scene.
        if (setsCompleted >= totalSets) 
        {
            SceneManager.LoadScene(sessionCompleteSceneName);
            return;
        }

        if (panelRestAfterSet != null) panelRestAfterSet.SetActive(false);
        if (panelWorkoutInfo != null) panelWorkoutInfo.SetActive(true);
        
        if (btnDoneWithSet != null) btnDoneWithSet.gameObject.SetActive(true);
    }

    void OnUndoSetPressed()
    {
        if (setsCompleted > 0)
        {
            setsCompleted--;

            if (WorkoutSession.setRepsList != null && WorkoutSession.setRepsList.Count > 0)
            {
                WorkoutSession.setRepsList.RemoveAt(WorkoutSession.setRepsList.Count - 1);
            }
            if (WorkoutSession.setWeightsList != null && WorkoutSession.setWeightsList.Count > 0)
            {
                WorkoutSession.setWeightsList.RemoveAt(WorkoutSession.setWeightsList.Count - 1);
            }

            WorkoutSession.CompletedSets = setsCompleted;

            int calculatedTotalReps = 0;
            if (WorkoutSession.setRepsList != null)
            {
                foreach (int r in WorkoutSession.setRepsList) calculatedTotalReps += r;
            }
            WorkoutSession.TotalReps = calculatedTotalReps;

            UpdateWorkoutUI();
        }

        isResting = false;
        if (panelRestAfterSet != null) panelRestAfterSet.SetActive(false);
        if (panelWorkoutInfo != null) panelWorkoutInfo.SetActive(true);
        
        if (btnDoneWithSet != null) btnDoneWithSet.gameObject.SetActive(true);
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
        if (txtRestTimer == null) return;

        timeToDisplay += 1f;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        txtRestTimer.text = string.Format("{0:00} : {1:00}", minutes, seconds);
    }

    void ChangeReps(int d)
    {
        currentReps = Mathf.Max(1, currentReps + d);
        UpdateWorkoutUI();
    }

    void ChangeWeight(int d)
    {
        currentWeightKg = Mathf.Max(0, currentWeightKg + d);
        UpdateWorkoutUI();
    }

    void UpdateWorkoutUI()
    {
        if (txtRepCount != null) txtRepCount.text = currentReps.ToString();
        if (txtWeightCount != null) txtWeightCount.text = currentWeightKg.ToString();
        if (txtSetsCount != null) txtSetsCount.text = setsCompleted + " / " + totalSets + " Sets";
    }
}