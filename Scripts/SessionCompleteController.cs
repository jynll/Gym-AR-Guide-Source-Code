using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class SessionCompleteController : MonoBehaviour
{
    [Header("--- MACHINE INFO ---")]
    public TMP_Text machineDisplayName;      
    public Image imgMachineThumbnail;        

    [Header("--- WORKOUT SUMMARY ---")]
    public TMP_Text txtSetsCompleted;
    public TMP_Text txtTotalReps;            
    public TMP_Text txtWeightUsed;           
    public TMP_Text txtDateCompleted;

    [Header("--- COMMENT ---")]
    public TMP_Text txtComment;

    [Header("--- BUTTONS ---")]
    public Button btnHome;
    public Button btnSave;

    [Header("--- SCENE NAMES ---")]
    public string homeSceneName = "HomeScene";

    private string lockedRepsString = "10 | 10 | 10";
    private string lockedWeightsString = "20 | 20 | 20";

    void Start()
    {
        LoadSummary();
        
        if (btnHome != null) 
            btnHome.onClick.AddListener(OnHomePressed);
            
        if (btnSave != null) 
            btnSave.onClick.AddListener(OnSavePressed);
    }

    void LoadSummary()
    {
        string currentId = WorkoutSession.SelectedMachineId;

        MachineGuideData matchedGuide = LoadMachineGuideData(currentId);
        string baseName = matchedGuide != null ? matchedGuide.machineDisplayName : WorkoutSession.SelectedMachineDisplayName;
        
        if (string.IsNullOrEmpty(baseName)) baseName = currentId;

        if (machineDisplayName != null)
            machineDisplayName.text = baseName + " Workout";

        if (imgMachineThumbnail != null)
        {
            Sprite loadedSprite = LoadExerciseImageWithModel(currentId);
            if (loadedSprite == null && matchedGuide != null && matchedGuide.machineImageWithModel != null)
            {
                loadedSprite = matchedGuide.machineImageWithModel;
            }

            if (loadedSprite != null)
            {
                imgMachineThumbnail.sprite = loadedSprite;
                imgMachineThumbnail.gameObject.SetActive(true);
            }
        }

        int sets = Mathf.Max(1, WorkoutSession.CompletedSets);
        
        List<int> repsPerSet = WorkoutSession.setRepsList;
        List<int> weightsPerSet = WorkoutSession.setWeightsList;

        if (repsPerSet == null || repsPerSet.Count == 0)
        {
            repsPerSet = new List<int> { 10, 10, 10 }; 
        }
        if (weightsPerSet == null || weightsPerSet.Count == 0)
        {
            int activeWeight = WorkoutSession.WeightUsedKg > 0 ? WorkoutSession.WeightUsedKg : 20;
            weightsPerSet = new List<int> { activeWeight, activeWeight, activeWeight };
        }

        lockedRepsString = string.Join(" | ", repsPerSet);
        lockedWeightsString = string.Join(" | ", weightsPerSet);

        if (txtTotalReps != null)
        {
            List<string> coloredReps = new List<string>();
            foreach (int rep in repsPerSet)
            {
                if (rep >= 12) 
                    coloredReps.Add($"<color=#2ECC71>{rep}</color>"); 
                else if (rep >= 10 && rep <= 11) 
                    coloredReps.Add($"<color=#F1C40F>{rep}</color>"); 
                else 
                    coloredReps.Add($"<color=#E74C3C>{rep}</color>"); 
            }
            
            txtTotalReps.text = string.Join(" | ", coloredReps) + " Reps";
        }

        if (txtWeightUsed != null)
        {
            txtWeightUsed.text = lockedWeightsString + " kg";
        }

        if (txtSetsCompleted != null)
            txtSetsCompleted.text = sets + " / 3 Sets";

        if (txtDateCompleted != null)
            txtDateCompleted.text = DateTime.Now.ToString("MMMM d, yyyy");

        if (txtComment != null)
        {
            txtComment.text = GeneratePerformanceComment(repsPerSet);
        }
    }

    Sprite LoadExerciseImageWithModel(string machineId)
    {
        Sprite loadedSprite = null;
        string folder = "ExerciseImages/";

        if (string.IsNullOrEmpty(machineId)) return null;

        if (machineId.Contains("leg_extension") || machineId.Contains("legs_extension"))
        {
            loadedSprite = Resources.Load<Sprite>(folder + "legs_extensionwithPeap");
        }
        else if (machineId.Contains("lat_pulldown"))
        {
            loadedSprite = Resources.Load<Sprite>(folder + "lat_pulldown_machine_withPeap");
        }
        else
        {
            string cleanId = machineId.Replace("_machine", "");
            
            loadedSprite = Resources.Load<Sprite>(folder + machineId + "withPeap");
            if (loadedSprite == null)
                loadedSprite = Resources.Load<Sprite>(folder + machineId + "_withPeap");
            if (loadedSprite == null)
                loadedSprite = Resources.Load<Sprite>(folder + cleanId + "_machinewithPeap");
            if (loadedSprite == null)
                loadedSprite = Resources.Load<Sprite>(folder + cleanId + "withPeap");
        }

        return loadedSprite;
    }

    string GeneratePerformanceComment(List<int> reps)
    {
        int completedSets = WorkoutSession.CompletedSets;

        if (completedSets == 0 || reps == null || reps.Count == 0)
            return "No data recorded for this session. Make sure to log your sets correctly next time!";

        if (completedSets < 3)
            return $"You only completed {completedSets} out of 3 sets. Make sure to pace yourself, hydrate, and take proper rest in between sets. You'll get it next time!";

        int lowestRep = 999;
        foreach (int rep in reps)
        {
            if (rep < lowestRep) lowestRep = rep;
        }

        if (lowestRep >= 12)
            return "Great! You've consistently hit your target reps for all sets. Next week, try adding 2.5 kg to 5 kg of weights on this particular workout to keep challenging your muscles!";
        else if (lowestRep >= 10 && lowestRep <= 11)
            return "Good job pushing through! Keep the current weight next week and aim to complete 12 reps on all sets before increasing the load.";
        else
            return "Solid effort! However, your reps dropped. Lessen the weights next time and focus on good form to reach your target goal.";
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

    void OnHomePressed()
    {
        WorkoutSession.Reset();
        if (!string.IsNullOrEmpty(homeSceneName))
        {
            SceneManager.LoadScene(homeSceneName);
        }
    }

    private int GetRoutineDayIndex(DateTime date, DateTime planStartDate)
    {
        if (date < planStartDate) return -1; 

        int validDays = 0;
        for (DateTime d = planStartDate.Date; d < date.Date; d = d.AddDays(1))
        {
            int statusVal = PlayerPrefs.GetInt("Workout_Status_" + d.ToString("yyyy-MM-dd"), 1); 
            if (statusVal != 3 && statusVal != 5) 
            {
                validDays++;
            }
        }
        return validDays;
    }

    void OnSavePressed()
    {
        WorkoutLog newLog = new WorkoutLog();

        int completedSetsCount = Mathf.Max(3, WorkoutSession.CompletedSets); 
        newLog.setsCompleted = completedSetsCount + " / 3 Sets";
        newLog.repsPerSet = lockedRepsString + " reps";
        newLog.weightUsed = lockedWeightsString + " kg";

        int currentWeek = 1;  
        int currentDay = 1;   
        string workoutGroup = "POSTERIOR";
        string goalStr = "WEIGHT LOSS";

        string activePlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (!string.IsNullOrEmpty(activePlanId) && FitnessDatabaseManager.Instance != null)
        {
            var plan = FitnessDatabaseManager.Instance.GetPlanById(activePlanId);
            if (plan != null)
            {
                goalStr = plan.planId.Replace("_", " ").ToUpper();
                
                string startDateStr = PlayerPrefs.GetString("ActivePlanStartDate", DateTime.Today.ToString("yyyy-MM-dd"));
                if (DateTime.TryParse(startDateStr, out DateTime planStart))
                {
                    int rIndex = GetRoutineDayIndex(DateTime.Today, planStart);
                    if (rIndex >= 0)
                    {
                        currentWeek = (rIndex / 7) + 1;
                        currentDay = (rIndex % 7) + 1; 

                        string savedRoutine = PlayerPrefs.GetString("ActiveCustomRoutine", "");
                        if (!string.IsNullOrEmpty(savedRoutine))
                        {
                            string[] routineArr = savedRoutine.Split(',');
                            workoutGroup = routineArr[rIndex % 7].ToUpper().Replace(" WORKOUT", "").Trim();
                        }
                    }
                }
            }
        }

        // =========================================================
        // [BULLETPROOF FIX]: Imbes na i-force as "Completed" (2), 
        // gagawin nating "In Progress" (6) muna. Hahayaan natin ang
        // Home Screen ang mag-mark ng (2) kung tapos na talaga lahat!
        // =========================================================
        int currentDayStatus = PlayerPrefs.GetInt("Workout_Status_" + DateTime.Today.ToString("yyyy-MM-dd"), 1);
        if (currentDayStatus != 2) // Kung hindi pa "Completed" (2)
        {
            PlayerPrefs.SetInt("Workout_Status_" + DateTime.Today.ToString("yyyy-MM-dd"), 6); // 6 = In Progress
        }
        PlayerPrefs.Save();

        newLog.weekNumber = currentWeek;
        newLog.dayNumber = currentDay;
        newLog.workoutType = workoutGroup; 
        newLog.fitnessGoal = goalStr;

        string machineNameStr = machineDisplayName != null ? machineDisplayName.text.Replace(" Workout", "") : "UNKNOWN MACHINE";
        newLog.machineName = machineNameStr;
        newLog.dateCompleted = DateTime.Now.ToString("MMMM d, yyyy");
        newLog.logId = DateTime.Now.Ticks.ToString();

        string HISTORY_KEY = "UserWorkoutHistory";
        string json = PlayerPrefs.GetString(HISTORY_KEY, "");
        
        WorkoutHistoryData historyData = new WorkoutHistoryData();
        if (!string.IsNullOrEmpty(json))
        {
            try { historyData = JsonUtility.FromJson<WorkoutHistoryData>(json); } catch { }
        }
        
        if (historyData == null) historyData = new WorkoutHistoryData();
        if (historyData.logs == null) historyData.logs = new List<WorkoutLog>();
        
        historyData.logs.Insert(0, newLog);
        
        PlayerPrefs.SetString(HISTORY_KEY, JsonUtility.ToJson(historyData));
        PlayerPrefs.Save(); 

        if (WorkoutHistoryManager.Instance != null)
        {
            try { WorkoutHistoryManager.Instance.AddWorkoutLog(newLog); } catch {}
        }

        if (btnSave != null)
        {
            var btnText = btnSave.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "SAVED ✓";
            btnSave.interactable = false;
        }
        
        Debug.Log("Workout Saved! Status marked as In Progress.");
    }
}