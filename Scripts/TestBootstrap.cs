using UnityEngine;

// TESTING ONLY — remove or disable before final APK build
public class TestBootstrap : MonoBehaviour
{
    [Tooltip("Type a machineId here to test that machine's cards")]
    public string testMachineId = "pec_fly_machine";

    void Awake()
    {
        WorkoutSession.SelectedMachineId = testMachineId;
    }
}