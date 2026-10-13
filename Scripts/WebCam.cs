using UnityEngine;

public class CameraTest : MonoBehaviour
{
    private WebCamTexture camTexture;

    void Start()
    {
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            Debug.Log(device.name);
        }

        camTexture = new WebCamTexture("Camo Camera"); // Replace with the exact name
        GetComponent<Renderer>().material.mainTexture = camTexture;
        camTexture.Play();
    }
}