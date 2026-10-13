using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class WorkoutLog
{
    public string logId;            
    public int weekNumber;          
    public int dayNumber;           
    public string workoutType;      
    public string machineName;      
    public string fitnessGoal;      
    public string dateCompleted;    
    public string setsCompleted;    
    public string repsPerSet;       
    public string weightUsed;       
}

[System.Serializable]
public class WorkoutHistoryData
{
    public List<WorkoutLog> logs = new List<WorkoutLog>();
}

public class WorkoutHistoryManager : MonoBehaviour
{
    public static WorkoutHistoryManager Instance { get; private set; }
    private WorkoutHistoryData historyData;

    private const string HISTORY_KEY = "UserWorkoutHistory";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            LoadHistory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadHistory()
    {
        string json = PlayerPrefs.GetString(HISTORY_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            historyData = JsonUtility.FromJson<WorkoutHistoryData>(json);
        }
        else
        {
            // Blangko lang kapag New User, walang dummy data na mag-i-interfere!
            historyData = new WorkoutHistoryData(); 
        }
    }

    public void AddWorkoutLog(WorkoutLog newLog)
    {
        if (string.IsNullOrEmpty(newLog.logId))
        {
            newLog.logId = DateTime.Now.Ticks.ToString();
        }
        
        LoadHistory();

        // Idagdag ang bagong workout sa simula (Index 0) para mapunta sa tuktok
        historyData.logs.Insert(0, newLog);
        
        SaveHistoryToPrefs();
        Debug.Log("SUCCESS: Workout Log saved! Total logs: " + historyData.logs.Count);
    }

    void SaveHistoryToPrefs()
    {
        string updatedJson = JsonUtility.ToJson(historyData);
        PlayerPrefs.SetString(HISTORY_KEY, updatedJson);
        PlayerPrefs.Save();
    }

    public List<WorkoutLog> GetAllLogs()
    {
        LoadHistory(); 

        var sortedLogs = historyData.logs
            .OrderByDescending(x => x.weekNumber)
            .ThenByDescending(x => x.dayNumber)
            .ToList();

        return sortedLogs;
    }
}