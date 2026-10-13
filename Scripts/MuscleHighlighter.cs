using UnityEngine;

public class MuscleHighlighter : MonoBehaviour
{
    [Header("--- 3D DUMMY SETUP ---")]
    public Renderer dummyRenderer;

    [Header("--- SETTINGS ---")]
    public bool autoDetectMachine = true;
    public string manualMachineId = "squat";
    public Color neutralColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    void Start()
    {
        if (dummyRenderer == null) return;
        string currentId = autoDetectMachine ? WorkoutSession.SelectedMachineId : manualMachineId;
        HighlightBasedOnMachine(currentId);
    }

    public void HighlightBasedOnMachine(string id)
    {
        string primaryMat = "";
        string sec1Mat = "";
        string sec2Mat = "";

        string cleanId = string.IsNullOrEmpty(id) ? "" : id.Replace("_machine", "").ToLower();

        switch (cleanId)
        {
            case "chest_press":
            case "flat_bench_chest_press":
            case "incline_chest_press":
            case "chess_press":
                primaryMat = "chest";
                sec1Mat = "trisceps"; 
                sec2Mat = "shoulder";
                break;
            case "lat_pulldown":
            case "seated_row":
            case "t_bar":
            case "t-bar":
                primaryMat = "lat"; 
                sec1Mat = "bisceps"; 
                sec2Mat = "back";    
                break;
            case "shoulder_press":
            case "seated_shoulder_press":
                primaryMat = "shoulder";
                sec1Mat = "trisceps";
                sec2Mat = "back";
                break;
            case "leg_press":
            case "squat":
            case "smith":
                primaryMat = "thigh"; 
                sec1Mat = "glute";
                sec2Mat = "hamstring";
                break;
            case "leg_extension":
            case "legs_extension":
                primaryMat = "thigh";
                break;
            case "seated_leg_curl":
                primaryMat = "hamstring";
                sec1Mat = "calves"; 
                sec2Mat = "glute";
                break;
            case "preacher_curl":
                primaryMat = "bisceps";
                sec1Mat = "forearm"; 
                break;
            case "pec_fly":
                primaryMat = "chest";
                sec1Mat = "shoulder";
                break;
            case "abductor":
                primaryMat = "glute"; 
                sec1Mat = "thigh";
                break;
        }

        Material[] mats = dummyRenderer.materials;

        for (int i = 0; i < mats.Length; i++)
        {
            // Converts to lowercase automatically so it matches easily
            string matName = mats[i].name.ToLower();

            // REMOVED "abs" here so it fades to gray when not active
            if (matName.Contains("skin") || matName.Contains("eyes")) continue;

            // .Contains() checks if the target muscle word exists in the material name, ignoring .001, .002, etc.
            bool isTargetMuscle = 
                (!string.IsNullOrEmpty(primaryMat) && matName.Contains(primaryMat)) ||
                (!string.IsNullOrEmpty(sec1Mat) && matName.Contains(sec1Mat)) ||
                (!string.IsNullOrEmpty(sec2Mat) && matName.Contains(sec2Mat));

            if (isTargetMuscle)
            {
                Debug.Log($"<color=green>HIGHLIGHTED:</color> {matName}"); // For checking in Unity Console
                Color originalColor = mats[i].GetColor("_BaseColor");
                mats[i].EnableKeyword("_EMISSION");
                
                float intensity = (!string.IsNullOrEmpty(primaryMat) && matName.Contains(primaryMat)) ? 2.0f : 1.0f;
                mats[i].SetColor("_EmissionColor", originalColor * intensity);
            }
            else
            {
                Debug.Log($"<color=grey>FADED:</color> {matName}"); // For checking in Unity Console
                mats[i].SetColor("_BaseColor", neutralColor);
                mats[i].DisableKeyword("_EMISSION");
                mats[i].SetColor("_EmissionColor", Color.black);
            }
        }
        
        dummyRenderer.materials = mats;
    }
}