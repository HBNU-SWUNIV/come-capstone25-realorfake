using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class CaptureManager : MonoBehaviour
{
    void Update()
    {
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed) && isTriggerPressed)
        {
            SceneManager.LoadScene("SampleScene");
        }
    }
}
