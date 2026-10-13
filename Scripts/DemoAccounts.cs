using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DemoAccounts : MonoBehaviour 
{
    [Header("Preset Settings")]
    public string strengthPlanId = "str_intermediate_12w"; 
    public string goalTitle = "INCREASE STRENGTH";        

    [Header("Popup Panel UI")]
    public GameObject panelPopupMessage;
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textMessage;
    public Button btnConfirm;
    public Button btnCancel;

    [Header("Panel Navigation")]
    public GameObject panelDemoAcc;      
    public GameObject panelHomePage;     

    private int selectedDemoType = 0;

    void Start()
    {
        if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirmDemo);
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelDemo);
        if (panelPopupMessage != null) panelPopupMessage.SetActive(false);
    }

    public void SelectDemoType(int demoType)
    {
        selectedDemoType = demoType;
        if (panelPopupMessage != null) panelPopupMessage.SetActive(true);
        if (btnCancel != null) btnCancel.gameObject.SetActive(true);

        if (textTitle != null) textTitle.text = "DEMO ACCOUNT CONFIRMATION";

        if (demoType == 1)
        {
            if (textMessage != null) textMessage.text = "Load New Account (No Records)? This will clear all current progress.";
        }
        else if (demoType == 2)
        {
            if (textMessage != null) textMessage.text = "Load Mid-Progress demo profile? This will load active history.";
        }
        else if (demoType == 3)
        {
            if (textMessage != null) textMessage.text = "Load Accomplished profile? This will load completed history.";
        }
    }

    void OnConfirmDemo()
    {
        if (selectedDemoType == 1) LoadPreset_NewAccount();
        else if (selectedDemoType == 2) LoadPreset_MidProgress();
        else if (selectedDemoType == 3) LoadPreset_Accomplished();

        if (textTitle != null) textTitle.text = "SUCCESS";
        if (textMessage != null) textMessage.text = "Demo account successfully loaded!";
        if (btnCancel != null) btnCancel.gameObject.SetActive(false);

        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(FinishAndGoHome);
        }
    }

    void OnCancelDemo()
    {
        if (panelPopupMessage != null) panelPopupMessage.SetActive(false);
    }

    void FinishAndGoHome()
    {
        if (panelPopupMessage != null) panelPopupMessage.SetActive(false);
        if (panelDemoAcc != null) panelDemoAcc.SetActive(false);
        if (panelHomePage != null) panelHomePage.SetActive(true);
    }

    public void LoadPreset_NewAccount()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    public void LoadPreset_MidProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetString("ActiveFitnessPlanId", strengthPlanId);
        PlayerPrefs.SetString("ActiveGoalTitle", goalTitle);
        
        FitnessPlan activePlan = null;
        if (FitnessDatabaseManager.Instance != null)
            activePlan = FitnessDatabaseManager.Instance.GetPlanById(strengthPlanId);

        int totalWeeks = activePlan != null ? activePlan.durationWeeks : 12;
        int weeksDone = totalWeeks / 2;
        
        // [FIX]: Magdadagdag tayo ng 1 Missed day, kaya kailangang i-offset ang petsa ng +1 day!
        int targetValidDays = weeksDone * 7; 
        int missedDaysCount = 1; 
        int totalElapsedDays = targetValidDays + missedDaysCount;

        DateTime startDate = DateTime.Today.AddDays(-totalElapsedDays); 
        PlayerPrefs.SetString("ActivePlanStartDate", startDate.ToString("yyyy-MM-dd"));

        int validDays = 0;

        for (int i = 0; i < totalElapsedDays; i++)
        {
            DateTime d = startDate.AddDays(i);
            string key = "Workout_Status_" + d.ToString("yyyy-MM-dd");

            bool isRest = false;
            if (activePlan != null && activePlan.weeks.Count > 0 && activePlan.weeks[0].days.Count > 0)
                isRest = activePlan.weeks[0].days[validDays % activePlan.weeks[0].days.Count].isRestDay;

            if (isRest)
            {
                PlayerPrefs.SetInt(key, 0); // Rest Day
                validDays++; 
            }
            else
            {
                if (i == (totalElapsedDays / 2)) 
                {
                    PlayerPrefs.SetInt(key, 3); // Missed (Hindi nadadagdagan ang validDays)
                }
                else
                {
                    PlayerPrefs.SetInt(key, 2); // Completed
                    validDays++; 
                }
            }
        }

        PlayerPrefs.Save();
        GenerateSyncedHistory(startDate, totalElapsedDays, activePlan);
        RefreshCalendar();
    }

    public void LoadPreset_Accomplished()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetString("ActiveFitnessPlanId", strengthPlanId);
        PlayerPrefs.SetString("ActiveGoalTitle", goalTitle);
        
        FitnessPlan activePlan = null;
        if (FitnessDatabaseManager.Instance != null)
            activePlan = FitnessDatabaseManager.Instance.GetPlanById(strengthPlanId);

        int totalWeeks = activePlan != null ? activePlan.durationWeeks : 12;
        
        // [FIX]: Kung 12 weeks = 84 valid days. Kung may 3 missed days, 87 days dapat ang nakalipas para tapos lahat kahapon.
        int targetValidDays = totalWeeks * 7;
        int missedDaysCount = 3; 
        int totalElapsedDays = targetValidDays + missedDaysCount;

        DateTime startDate = DateTime.Today.AddDays(-totalElapsedDays); 
        PlayerPrefs.SetString("ActivePlanStartDate", startDate.ToString("yyyy-MM-dd"));

        int validDays = 0;

        for (int i = 0; i < totalElapsedDays; i++)
        {
            DateTime d = startDate.AddDays(i);
            string key = "Workout_Status_" + d.ToString("yyyy-MM-dd");

            bool isRest = false;
            if (activePlan != null && activePlan.weeks.Count > 0 && activePlan.weeks[0].days.Count > 0)
                isRest = activePlan.weeks[0].days[validDays % activePlan.weeks[0].days.Count].isRestDay;

            if (isRest)
            {
                PlayerPrefs.SetInt(key, 0); // Rest Day
                validDays++; 
            }
            else
            {
                if (i == 20 || i == 45 || i == 70)
                {
                    PlayerPrefs.SetInt(key, 3); // Missed (Shifts the schedule)
                }
                else
                {
                    PlayerPrefs.SetInt(key, 2); // Completed
                    validDays++; 
                }
            }
        }

        PlayerPrefs.Save();
        GenerateSyncedHistory(startDate, totalElapsedDays, activePlan);
        RefreshCalendar();
    }

    private void RefreshCalendar()
    {
        CalendarPreviewTest calendarScript = FindFirstObjectByType<CalendarPreviewTest>();
        if (calendarScript != null)
        {
            calendarScript.RefreshCalendarUI();
        }
    }

    private void GenerateSyncedHistory(DateTime startDate, int totalDays, FitnessPlan activePlan)
    {
        WorkoutHistoryData fakeHistory = new WorkoutHistoryData();
        int validDays = 0;

        for (int i = 0; i < totalDays; i++)
        {
            DateTime d = startDate.AddDays(i);
            int status = PlayerPrefs.GetInt("Workout_Status_" + d.ToString("yyyy-MM-dd"), -1);
            
            bool isRest = false;
            string dayType = "WHOLE BODY";

            if (activePlan != null && activePlan.weeks.Count > 0 && activePlan.weeks[0].days.Count > 0)
            {
                var dayData = activePlan.weeks[0].days[validDays % activePlan.weeks[0].days.Count];
                isRest = dayData.isRestDay;
                dayType = dayData.dayType;
            }

            if (status == 2 && !isRest)
            {
                int weekNum = (validDays / 7) + 1;
                int dayNum = (validDays % activePlan.weeks[0].days.Count) + 1;

                WorkoutCategory categoryInfo = null;
                if (FitnessDatabaseManager.Instance != null)
                {
                    categoryInfo = FitnessDatabaseManager.Instance.GetWorkoutCategory(dayType);
                }

                if (categoryInfo != null && categoryInfo.exercises != null && categoryInfo.exercises.Count > 0)
                {
                    for(int m = categoryInfo.exercises.Count - 1; m >= 0; m--)
                    {
                        var ex = categoryInfo.exercises[m]; 

                        WorkoutLog log = new WorkoutLog();
                        log.logId = d.Ticks.ToString() + m.ToString();
                        log.weekNumber = weekNum;
                        log.dayNumber = dayNum;
                        log.workoutType = dayType; 
                        log.machineName = ex.machineId.Replace("_", " ").ToUpper();
                        
                        if (!log.machineName.Contains("MACHINE") && !log.machineName.Contains("T BAR") && !log.machineName.Contains("T-BAR"))
                        {
                            log.machineName += " MACHINE";
                        }
                        
                        log.fitnessGoal = PlayerPrefs.GetString("ActiveGoalTitle", "FITNESS GOAL");
                        log.dateCompleted = d.ToString("MMMM dd, yyyy"); 
                        log.setsCompleted = $"{ex.sets} / {ex.sets} sets";
                        log.repsPerSet = $"{ex.reps} | {ex.reps} | {ex.reps} reps";
                        log.weightUsed = "10 | 20 | 25 kg"; 

                        fakeHistory.logs.Insert(0, log);
                    }
                }
            }

            if (status != 3) validDays++; 
        }

        PlayerPrefs.SetString("UserWorkoutHistory", JsonUtility.ToJson(fakeHistory));
        PlayerPrefs.Save();
    }
}