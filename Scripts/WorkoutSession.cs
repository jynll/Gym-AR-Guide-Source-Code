using System.Collections.Generic; // Kailangan para sa List<>

public static class WorkoutSession
{
    // Set by Scan screen, read by Guide Cards + AR Demo
    public static string SelectedMachineId = "";
    public static string SelectedMachineDisplayName = "";

    // Set by Scan Result screen
    public static float DetectionConfidence = 0f;

    // Set by AR Demo, read by Session Complete
    public static int CompletedSets = 0;
    public static int TotalReps = 0;
    public static int WeightUsedKg = 0;

    // --- IDINAGDAG PARA SA PER-SET TRACKING ---
    public static List<int> setRepsList = new List<int>();
    public static List<int> setWeightsList = new List<int>();

    public static void Reset()
    {
        SelectedMachineId = "";
        SelectedMachineDisplayName = "";
        DetectionConfidence = 0f;
        CompletedSets = 0;
        TotalReps = 0;
        WeightUsedKg = 0;
        
        // --- IDINAGDAG RIN PARA MA-RESET TUWING TAPOS ANG SESSION ---
        if (setRepsList != null) setRepsList.Clear();
        if (setWeightsList != null) setWeightsList.Clear();
    }
}