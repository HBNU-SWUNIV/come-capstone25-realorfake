using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

public class TimerObjectXR : MonoBehaviour
{
    [Header("시계 설정")]
    public GameObject clockPrefab;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRayInteractor;
    public float throwForce = 15f;
    public Vector3 holdOffset = new Vector3(0, 0, 0.5f);

    private bool isGrabbed = false;
    private Vector3 originalPosition;
    private GameManager gameManager;
    private bool isReset = false;

    void Start()
    {
        originalPosition = transform.position;

        // GameManager 참조 가져오기
        gameManager = GameManager.GetInstance();
    }

    void Update()
    {

        if (this.transform.position.y > 20f)
        {
            ResetClock();

            if (!isReset)
            {
                gameManager.EndTurn(NetworkManager.Singleton.IsHost);
                isReset = true;
            }
        } else
        {
            isReset = false;
        }

            return;
        HandleGrabInput();
        HandleThrowInput();
        CheckReset();
    }

    void HandleGrabInput()
    {
        if (isGrabbed || leftRayInteractor == null) return;

        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && isPressed)
        {
            if (leftRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == this)
                {
                    GrabClock();
                }
            }
        }
    }

    void GrabClock()
    {
        isGrabbed = true;
        this.GetComponent<Rigidbody>().isKinematic = true;
        Debug.Log("시계 잡음!");
    }

    void HandleThrowInput()
    {
        if (!isGrabbed || leftRayInteractor == null) return;

        // 컨트롤러 위치 추적
        this.transform.position = leftRayInteractor.transform.position +
                                   leftRayInteractor.transform.TransformDirection(holdOffset);

        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && !isPressed)
        {
            ThrowClock();
        }
    }

    void ThrowClock()
    {
        isGrabbed = false;
        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(leftRayInteractor.transform.forward * throwForce, ForceMode.Impulse);

    }

    void CheckReset()
    {
        if (this.transform.position.y < -1f)
        {
            ResetClock();
        }
    }

    void ResetClock()
    {
        this.transform.position = originalPosition;
        this.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }
}
