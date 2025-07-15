using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR;
using System.Collections.Generic;

public class ArrangementControl : MonoBehaviour
{
    private CardManager cardManager;
    private CardArrangement cardArrangement;
    private XRRayInteractor rayInteractor;
    private int currentPage = 0;
    private const int CARDS_PER_PAGE = 18;
    private InputDevice leftController;
    private InputDevice rightController;
    private bool isLeftControllerInitialized = false;
    private bool isRightControllerInitialized = false;
    [SerializeField] private float rayDistance = 10f;

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "StartScene")
        {
            InitializeComponents();
            InitializeControllers();
        }
    }

    private void InitializeComponents()
    {
        cardManager = GameObject.Find("CardManager").GetComponent<CardManager>();
        //cardManager = FindAnyObjectByType<CardManager>();
        if (cardManager == null)
        {
            Debug.LogError("CardManager를 찾을 수 없습니다!");
            return;
        }

        rayInteractor = GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            Debug.LogError("XRRayInteractor를 찾을 수 없습니다!");
            return;
        }
    }

    private void InitializeControllers()
    {
        var inputDevices = new List<InputDevice>();
        
        // 왼손 컨트롤러 초기화
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, inputDevices);
        if (inputDevices.Count > 0)
        {
            leftController = inputDevices[0];
            isLeftControllerInitialized = true;
        }
        else
        {
            StartCoroutine(InitializeLeftController());
        }

        // 오른손 컨트롤러 초기화
        inputDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);
        if (inputDevices.Count > 0)
        {
            rightController = inputDevices[0];
            isRightControllerInitialized = true;
        }
        else
        {
            StartCoroutine(InitializeRightController());
        }
    }

    private System.Collections.IEnumerator InitializeLeftController()
    {
        while (!isLeftControllerInitialized)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, inputDevices);
            if (inputDevices.Count > 0)
            {
                leftController = inputDevices[0];
                isLeftControllerInitialized = true;
            }
            yield return null;
        }
    }

    private System.Collections.IEnumerator InitializeRightController()
    {
        while (!isRightControllerInitialized)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);
            if (inputDevices.Count > 0)
            {
                rightController = inputDevices[0];
                isRightControllerInitialized = true;
            }
            yield return null;
        }
    }

    void Update()
    {
        if (cardManager == null || cardManager.allCardData == null) return;

        // CardArrangement 찾기
        if (cardArrangement == null)
        {
            cardArrangement = FindAnyObjectByType<CardArrangement>();
            if (cardArrangement != null)
            {
                UpdateCardDisplay();
            }
        }

        RaycastHit hit;
        if (CastRay(out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (hitObject.layer == LayerMask.NameToLayer("InteractiveUI"))
            {
                if (IsAnyTriggerPressed())
                {
                    switch (hitObject.tag)
                    {
                        case "NextItem":
                            ShowNextPage();
                            break;
                        case "PrevItem":
                            ShowPreviousPage();
                            break;
                        case "QuitItem":
                            DeactivateAll();
                            break;
                    }
                }
            }
        }
    }

    private bool CastRay(out RaycastHit hit)
    {
        return Physics.Raycast(transform.position, transform.forward, out hit, rayDistance);
    }

    bool IsAnyTriggerPressed()
    {
        bool leftTrigger = false;
        bool rightTrigger = false;

        if (isLeftControllerInitialized)
        {
            leftController.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger);
        }

        if (isRightControllerInitialized)
        {
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out rightTrigger);
        }

        return leftTrigger || rightTrigger;
    }

    private void ShowNextPage()
    {
        int totalCards = cardManager.allCardData != null ? cardManager.allCardData.Count : 0;
        int maxPages = Mathf.CeilToInt((float)totalCards / CARDS_PER_PAGE);

        if (currentPage < maxPages - 1)
        {
            currentPage++;
            UpdateCardDisplay();
        }
    }

    private void ShowPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateCardDisplay();
        }
    }

    public void UpdateCardDisplay()
    {
        if (cardManager == null || cardArrangement == null) return;

        int startIndex = currentPage * CARDS_PER_PAGE;
        int endIndex = startIndex + CARDS_PER_PAGE;
        cardManager.LoadAndArrangePage(startIndex, endIndex);
    }

    private void DeactivateAll()
    {
        if (cardManager != null)
        {
            foreach (var card in cardManager.currentActiveCards)
            {
                if (card != null)
                {
                    card.SetActive(false);
                }
            }
            cardManager.currentActiveCards.Clear();
        }
    }
} 