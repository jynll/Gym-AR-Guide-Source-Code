using UnityEngine;

[CreateAssetMenu(fileName = "NewMachineGuide", menuName = "GymAR/Machine Guide Data")]
public class MachineGuideData : ScriptableObject
{
    [Header("Machine Identity")]
    public string machineId;
    public string machineDisplayName;

    [Header("Guide Cards")]
    public GuideCard[] guideCards;

    [Header("AR Demo")]
    public GameObject arMachinePrefab;

    [Header("Machine Image w/0 model")]
    public Sprite machineImage;

    [Header("Machine Image w/ model")]
    public Sprite machineImageWithModel;
}
