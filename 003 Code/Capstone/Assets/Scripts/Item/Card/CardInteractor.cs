using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.SceneManagement;

public class CardInteractor : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    public Transform zoomTarget;
    public float zoomSpeed = 5f;

    private GameObject selectedCard;
    private Transform originalParent;
    private Vector3 originalWorldPosition; // 월드 좌표 저장
    private Vector3 originalLocalScale;
    private bool isZooming = false;
    public bool IsZooming => isZooming;
    private bool isMoving = false;
    private InputDevice rightController;

    void Start()
    {
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        if (rayInteractor == null) return;

        if (!isZooming && !isMoving && rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject hitObject = hit.collider?.gameObject;

            if (hitObject != null && hitObject.layer == LayerMask.NameToLayer("Card"))
            {
                GameObject cardParent = hitObject.transform.root.gameObject;

                if (selectedCard != cardParent)
                {
                    ClearSelection();
                    SelectCard(cardParent);
                }

                EnableOutline(selectedCard);

                if (IsTriggerPressed())
                {
                    ZoomIn();
                    rayInteractor.enabled = false;
                }
            }
        }
        else if (isZooming && !IsTriggerPressed() && !isMoving)
        {
            ZoomOut();
            rayInteractor.enabled = true;
        }
    }

    void SelectCard(GameObject card)
    {
        selectedCard = card;
        originalParent = card.transform.parent;
        originalWorldPosition = card.transform.position; // 월드 좌표 저장
        originalLocalScale = card.transform.localScale;
    }

    void ClearSelection()
    {
        if (selectedCard != null && !isZooming)
        {
            DisableOutline(selectedCard);
            selectedCard = null;
            originalParent = null;
        }
    }

    void ZoomIn()
    {
        if (selectedCard != null && !isZooming && !isMoving)
        {
            isZooming = true;
            isMoving = true;
            selectedCard.transform.SetParent(null);
            StopAllCoroutines();
            StartCoroutine(MoveToPosition(selectedCard.transform, zoomTarget.position, zoomTarget.localScale, () =>
            {
                isMoving = false;
            }));
        }
    }

    void ZoomOut()
    {
        if (selectedCard == null)
        {
            isZooming = false;
            rayInteractor.enabled = true;
            return;
        }

        isZooming = false;
        isMoving = true;
        StopAllCoroutines();

        // 원래 월드 좌표로 이동
        StartCoroutine(MoveToPosition(selectedCard.transform, originalWorldPosition, originalLocalScale, () =>
        {
            // 부모 재설정 (월드 좌표 유지)
            if (originalParent != null)
            {
                selectedCard.transform.SetParent(originalParent, true); // 월드 좌표 유지
            }
            isMoving = false;
        }));
    }

    private IEnumerator MoveToPosition(Transform cardTransform, Vector3 targetPosition, Vector3 targetScale, System.Action onComplete)
    {
        Vector3 startPosition = cardTransform.position;
        Vector3 startScale = cardTransform.localScale;

        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * zoomSpeed;
            cardTransform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            cardTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime);
            yield return null;
        }

        cardTransform.position = targetPosition;
        cardTransform.localScale = targetScale;

        onComplete?.Invoke();
    }


    void EnableOutline(GameObject card)
    {
        Outline outline = card.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    void DisableOutline(GameObject card)
    {
        Outline outline = card.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    bool IsTriggerPressed()
    {
        if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed))
        {
            return isPressed;
        }
        return false;
    }

    // 씬 전환 등에서 참조 초기화
    void OnDestroy()
    {
        ClearSelection();
    }
}
