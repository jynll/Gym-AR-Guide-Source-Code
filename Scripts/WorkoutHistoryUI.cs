using UnityEngine;
using TMPro;

public class WorkoutHistoryUI : MonoBehaviour
{
    public Transform contentContainer;      // Dito mo i-drag ang "Content" object kung saan nakasalansan ang mga buttons
    public GameObject historyItemPrefab;    // Dito mo i-drag ang Prefab ng iisang button ng history

    private void OnEnable()
    {
        RefreshHistoryDisplay();
    }

    public void RefreshHistoryDisplay()
    {
        if (WorkoutHistoryManager.Instance == null) return;

        // 1. Burahin lahat ng nakaiwang dummy prefabs (yung mga hardcoded mo sa Unity)
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Kunin ang mga totoong history logs (na ginawa ng DemoAccounts natin)
        var logs = WorkoutHistoryManager.Instance.GetAllLogs();

        // 3. I-spawn at i-display ang totoong data sa screen!
        foreach (var log in logs)
        {
            GameObject item = Instantiate(historyItemPrefab, contentContainer);
            
            // Hanapin ang mga Text (TMP) sa loob ng button
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();
            
            // Assuming na ang unang Text (Yellow) ay ang Machine, at ang pangalawa (White) ay ang Week/Day
            if (texts.Length >= 2)
            {
                texts[0].text = log.machineName; 
                texts[1].text = $"WEEK {log.weekNumber} - DAY {log.dayNumber} {log.workoutType}"; 
            }
        }
    }
}