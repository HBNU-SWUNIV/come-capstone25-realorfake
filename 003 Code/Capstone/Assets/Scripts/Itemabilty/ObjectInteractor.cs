using Oculus.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ObjectInteractor : MonoBehaviour
{

    XRRayInteractor _rayInteractor;
    InputDevice rightHand;

    void Start()
    {
        _rayInteractor = this.gameObject.GetComponent<XRRayInteractor>();
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        HandleInteraction();
    }

    void HandleInteraction()
    {
        if (_rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject hitObject = hit.collider?.gameObject;
            if (hitObject == null) return;
            // ī�� ���̾��� ��쿡�� ó��
            if (hitObject.tag == "Item" && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) &&
            isPressed)
            {
                Debug.Log("ObjectInteractor InstallActive");
                if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, hitObject.GetComponent<BaseItem>().GetStamina()))
                {
                    GameManager.GetInstance().SetStaminaObject(GameManager.GetInstance().GetStamina(NetworkManager.Singleton.IsHost));
                    hitObject.GetComponent<BaseItem>().InstallActive();
                    hitObject.GetComponent<BaseItem>().PlayUseParticle(GameManager.GetInstance().GetEnemy());
                }
                    
            }
        }
    }
}
