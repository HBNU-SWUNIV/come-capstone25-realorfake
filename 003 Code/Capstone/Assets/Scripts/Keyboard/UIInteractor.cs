using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class UIInteractor : MonoBehaviour
{
    public XRRayInteractor _rayInteractor;
    InputDevice rightHand;
    InputDevice leftHand;
    static InputField _lastInputField;

    void Start()
    {
        _rayInteractor = GetComponent<XRRayInteractor>();
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    }

    void Update()
    {
        HandleInteraction();
    }

    void HandleInteraction()
    {
        if (_rayInteractor != null && _rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject hitObject = hit.collider?.gameObject;
            if (hitObject == null) return;
            Debug.Log($"Hitobj : {hitObject.name}");
            Debug.Log($"Hitobj : {hitObject.tag}");
            if (hitObject.tag == "InputField" && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) &&
            isPressed)
            {
                _lastInputField = hitObject.GetComponent<InputField>();
                Debug.Log("UIInteractor InstallActive");
                _lastInputField.Select();

            }
            if (hitObject.tag == "InputField" && leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed2) &&
            isPressed2)
            {
                _lastInputField = hitObject.GetComponent<InputField>();
                Debug.Log("UIInteractor InstallActive");
                _lastInputField.Select();

            }
        }
    }

    public InputField GetLastInputField()
    {
        return _lastInputField;
    }
 }
