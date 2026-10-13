using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScaleAndRotateSlider : MonoBehaviour
{
    private Slider scaleSlider;
    private Slider rotateSlider;
    
    public float scaleMinValue = 20f;
    public float scaleMaxValue = 65f;
    public float rotMinValue = 0f;
    public float rotMaxValue = 360f;

    void Start()
    {
        // 1. Setup Scale Slider
        scaleSlider = GameObject.Find("ScaleSlider").GetComponent<Slider>();
        scaleSlider.minValue = scaleMinValue;
        scaleSlider.maxValue = scaleMaxValue;
        
        
        scaleSlider.value = (scaleMinValue + scaleMaxValue) / 2f; 
        scaleSlider.onValueChanged.AddListener(ScaleSliderUpdate);

        // 2. Setup Rotate Slider
        rotateSlider = GameObject.Find("RotateSlider").GetComponent<Slider>();
        rotateSlider.minValue = rotMinValue;
        rotateSlider.maxValue = rotMaxValue;
        
        
        rotateSlider.value = (rotMinValue + rotMaxValue) / 2f; 
        rotateSlider.onValueChanged.AddListener(RotateSliderUpdate);
    }

    void ScaleSliderUpdate(float value)
    {
        transform.localScale = new Vector3(value, value, value);
    }

    void RotateSliderUpdate(float value)
    {
        Vector3 currentRotation = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentRotation.x, value, currentRotation.z);
    }
}