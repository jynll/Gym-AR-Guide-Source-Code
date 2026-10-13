using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartWorkoutMenuController : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button btnBack;             
    public Button btn3DExercise;       
    public Button btnScanMachine;      
    public Button btnSetNewGoal;       

    [Header("Scene Names")]
    public string homeSceneName = "HomeScene";
    public string selectMachineSceneName = "Select3DONAR";
    public string scanMachineSceneName = "NewScanScene";
    public string fitnessGoalSceneName = "FitnessGoal"; 
    
    [Header("Workout Display References")]
    public TextMeshProUGUI txtWeekDay;       
    public TextMeshProUGUI txtWorkout;       
    public TextMeshProUGUI txtDetailWorkout; 
    
    [Header("Action Sections")]
    public GameObject arAndScanSection;       

    private FitnessPlan activePlan;
    private DateTime planStartDate;
    private string firstMachineIdToLaunch = ""; 
    
    private string[] activeCustomRoutine = new string[7];
    private int adjustedDurationWeeks = 4;

    void Start()
    {
        if (btnBack != null) btnBack.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
        if (btn3DExercise != null) btn3DExercise.onClick.AddListener(On3DExerciseClicked);
        if (btnScanMachine != null) btnScanMachine.onClick.AddListener(() => SceneManager.LoadScene(scanMachineSceneName));
        if (btnSetNewGoal != null) btnSetNewGoal.onClick.AddListener(() => SceneManager.LoadScene(fitnessGoalSceneName));

        LoadActivePlanData();
        DisplayTodayWorkout();
    }

    private void LoadActivePlanData()
    {
        string savedPlanId = PlayerPrefs.GetString("ActiveFitnessPlanId", "");
        if (!string.IsNullOrEmpty(savedPlanId) && FitnessDatabaseManager.Instance != null)
        {
            activePlan = FitnessDatabaseManager.Instance.GetPlanById(savedPlanId);
        }

        string startDateStr = PlayerPrefs.GetString("ActivePlanStartDate", DateTime.Today.ToString("yyyy-MM-dd"));
        DateTime.TryParse(startDateStr, out planStartDate);

        adjustedDurationWeeks = PlayerPrefs.GetInt("ActivePlanAdjustedWeeks", activePlan != null ? activePlan.durationWeeks : 4);

        string savedRoutine = PlayerPrefs.GetString("ActiveCustomRoutine", "");
        if (!string.IsNullOrEmpty(savedRoutine)) 
        {
            activeCustomRoutine = savedRoutine.Split(',');
        } 
        else if (activePlan != null && activePlan.weeks.Count > 0) 
        {
            for (int i = 0; i < 7; i++) activeCustomRoutine[i] = activePlan.weeks[0].days[i % activePlan.weeks[0].days.Count].dayType;
        }
    }

    // [FIX]: SHIFTING LOGIC IS BACK. Titigil ang bilang sa mga araw na Missed o Rescheduled
    // para umusad ang schedule mo sa susunod na araw!
    private int GetRoutineDayIndex(DateTime date)
    {
        if (date < planStartDate) return -1; 

        int validDays = 0;
        for (DateTime d = planStartDate.Date; d < date.Date; d = d.AddDays(1))
        {
            string key = "Workout_Status_" + d.ToString("yyyy-MM-dd");
            int statusVal = PlayerPrefs.GetInt(key, 1); 
            
            if (statusVal != 3 && statusVal != 5) 
            {
                validDays++;
            }
        }
        return validDays;
    }

    private void DisplayTodayWorkout()
    {
        PlayerPrefs.SetInt("IsFreeWorkout", 1);
        PlayerPrefs.SetString("TodayAllowedMachines", "");
        firstMachineIdToLaunch = ""; 

        if (btnSetNewGoal != null) btnSetNewGoal.gameObject.SetActive(false);

        if (activePlan == null)
        {
            if (txtWeekDay != null) txtWeekDay.text = "FREESTYLE MODE";
            if (txtWorkout != null) txtWorkout.text = "FREESTYLE WORKOUT";
            
            if (txtDetailWorkout != null) 
            {
                txtDetailWorkout.text = "No active fitness plan currently set.\n\n" +
                                        "<color=#2ECC71>Explore and exercise freely! Any completed machines will be recorded to your history and reflected on your calendar.</color>";
            }

            if (arAndScanSection != null) arAndScanSection.SetActive(true);
            if (btnSetNewGoal != null) btnSetNewGoal.gameObject.SetActive(true);
            return;
        }

        DateTime today = DateTime.Today;
        int routineDayIndex = GetRoutineDayIndex(today);

        if (routineDayIndex < 0 || routineDayIndex >= adjustedDurationWeeks * 7)
        {
            if (txtWeekDay != null) txtWeekDay.text = "PLAN ENDED";
            if (txtWorkout != null) txtWorkout.text = "CONGRATULATIONS!";
            if (txtDetailWorkout != null) txtDetailWorkout.text = "You have completed your fitness plan duration.";
            
            if (btnSetNewGoal != null) btnSetNewGoal.gameObject.SetActive(true);
            if (arAndScanSection != null) arAndScanSection.SetActive(true); 
            return;
        }

        string todayKey = "Workout_Status_" + today.ToString("yyyy-MM-dd");
        int todayStatusVal = PlayerPrefs.GetInt(todayKey, 1); 

        string todaysWorkoutType = activeCustomRoutine[routineDayIndex % 7];
        
        int weekNum = (routineDayIndex / 7) + 1;
        int dayNum = (routineDayIndex % 7) + 1;

        if (txtWeekDay != null) txtWeekDay.text = $"WEEK {weekNum} - DAY {dayNum}";

        if (todayStatusVal == 5)
        {
            if (txtWorkout != null) txtWorkout.text = "SCHEDULE ADJUSTED";
            
            if (txtDetailWorkout != null) 
            {
                txtDetailWorkout.text = "\nYour workout schedule for today has been adjusted and pushed to the next available day.\n\n" +
                                        "<color=#2ECC71>Still want to exercise? You can proceed with a Free Style Workout below. It will be recorded to your history.</color>";
            }
            if (arAndScanSection != null) arAndScanSection.SetActive(true); 
            return;
        }

        if (todaysWorkoutType.ToUpper().Contains("REST") || todayStatusVal == 0 || todayStatusVal == 4)
        {
            if (txtWorkout != null) txtWorkout.text = "REST DAY";
            
            if (txtDetailWorkout != null) 
            {
                txtDetailWorkout.text = "\nTake a rest, hydrate, and let your muscles recover today!\n\n" +
                                        "<color=#2ECC71>Still want to exercise? You can proceed with a Free Style Workout below. It will be recorded to your history.</color>";
            }
            if (arAndScanSection != null) arAndScanSection.SetActive(true); 
            return;
        }

        if (txtWorkout != null) 
        {
            string formattedTitle = todaysWorkoutType.ToUpper();
            if (!formattedTitle.Contains("WORKOUT")) formattedTitle += " WORKOUT";
            
            if (todayStatusVal == 2) formattedTitle += " <color=#2ECC71>(COMPLETED)</color>";
            else if (todayStatusVal == 6) formattedTitle += " <color=#3498DB>(IN PROGRESS)</color>";
            
            txtWorkout.text = formattedTitle;
        }

        WorkoutCategory categoryInfo = null;
        if (FitnessDatabaseManager.Instance != null)
        {
            categoryInfo = FitnessDatabaseManager.Instance.GetWorkoutCategory(todaysWorkoutType);
        }

        string detailsStr = "";
        if (categoryInfo != null && categoryInfo.exercises != null && categoryInfo.exercises.Count > 0)
        {
            PlayerPrefs.SetInt("IsFreeWorkout", 0); 
            
            firstMachineIdToLaunch = categoryInfo.exercises[0].machineId;
            List<string> allowedIds = new List<string>();

            foreach (var ex in categoryInfo.exercises)
            {
                allowedIds.Add(ex.machineId); 
                
                string formattedMachineName = ex.machineId.Replace("_", " ").ToUpper();
                if (!formattedMachineName.Contains("MACHINE") && !formattedMachineName.Contains("T BAR") && !formattedMachineName.Contains("SMITH"))
                {
                    formattedMachineName += " MACHINE";
                }
                detailsStr += $"<color=#F1C40F>{formattedMachineName}</color> | {ex.sets} sets | {ex.reps} reps |\n";
            }
            
            PlayerPrefs.SetString("TodayAllowedMachines", string.Join(",", allowedIds));
        }
        else
        {
            detailsStr = "Standard routine layout for today.";
        }

        if (txtDetailWorkout != null) txtDetailWorkout.text = detailsStr;
        if (arAndScanSection != null) arAndScanSection.SetActive(true);
    }

    private void On3DExerciseClicked()
    {
        if (!string.IsNullOrEmpty(firstMachineIdToLaunch))
        {
            WorkoutSession.SelectedMachineId = firstMachineIdToLaunch;
            WorkoutSession.SelectedMachineDisplayName = firstMachineIdToLaunch.Replace("_", " ").ToUpper();
            if (!WorkoutSession.SelectedMachineDisplayName.Contains("MACHINE") && !WorkoutSession.SelectedMachineDisplayName.Contains("T BAR"))
            {
                WorkoutSession.SelectedMachineDisplayName += " MACHINE";
            }
        }
        else
        {
            WorkoutSession.SelectedMachineId = "chest_press";
            WorkoutSession.SelectedMachineDisplayName = "CHEST PRESS MACHINE";
        }
        SceneManager.LoadScene(selectMachineSceneName);
    }
}