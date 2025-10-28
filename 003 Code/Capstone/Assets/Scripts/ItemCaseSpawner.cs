using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ItemCaseSpawner : MonoBehaviour
{
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private InputDevice leftController;
    [SerializeField] private InputDevice rightController;
    [SerializeField] private GameObject itemCase;
    private ArrangementControl arrangementControl;

    private void Start()
    {
        if (rayInteractor == null)
        {
            Debug.LogError("Ray Interactor가 할당되지 않았습니다!");
            return;
        }

        // ArrangementControl 찾기
        arrangementControl = FindAnyObjectByType<ArrangementControl>();
        if (arrangementControl == null)
        {
            Debug.Log("XR Origin에서 ArrangementControl을 찾을 수 없습니다!");
            return;
        }

        // 컨트롤러 초기화
        var leftHandDevices = new List<InputDevice>();
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftHandDevices);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

        if (leftHandDevices.Count > 0)
            leftController = leftHandDevices[0];
        if (rightHandDevices.Count > 0)
            rightController = rightHandDevices[0];

        // ItemCase가 할당되지 않았다면 경고
        if (itemCase == null)
        {
            Debug.LogWarning("ItemCase 오브젝트가 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (IsTriggerPressed())
        {
            ActivateItemCase();
        }
    }

    private bool IsTriggerPressed()
    {
        if (!leftController.isValid && !rightController.isValid) return false;
        
        bool triggerPressed = false;
        if (leftController.isValid)
            leftController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        if (!triggerPressed && rightController.isValid)
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
            
        return triggerPressed;
    }

    private void ActivateItemCase()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // 히트된 오브젝트가 이 ItemCaseSpawner가 붙어있는 오브젝트인지 확인
            if (hit.collider.gameObject == gameObject)
            {
                if (itemCase != null)
                {
                    // ItemCase가 비활성화되어 있는지 확인
                    if (!itemCase.activeSelf)
                    {
                        // ItemCase 활성화
                        itemCase.SetActive(true);
                        Debug.Log($"아이템 케이스 활성화: {itemCase.name}");
                        
                        // CardManager 찾기
                        CardManager cardManager = FindAnyObjectByType<CardManager>();
                        if (cardManager != null)
                        {
                            // CardManager의 참조 설정
                            cardManager.spawnPoint = itemCase.transform;
                            
                            // CardArrangement를 찾도록 호출
                            cardManager.FindCardArrangement();
                            
                            // 카드 데이터가 로드되었다면 카드 정렬 시작
                            if (cardManager.allCardData != null)
                            {
                                cardManager.LoadAndArrangePage(0, 18); // 첫 페이지의 카드 로드
                            }
                        }
                        else
                        {
                            Debug.LogWarning("CardManager를 찾을 수 없습니다!");
                        }
                    }
                    else
                    {
                        Debug.Log($"아이템 케이스가 이미 활성화되어 있습니다: {itemCase.name}");
                    }
                }
                else
                {
                    Debug.LogError("ItemCase 오브젝트가 할당되지 않았습니다!");
                }
            }
            else
            {
                Debug.Log($"레이캐스트가 ItemCaseSpawner가 붙어있는 오브젝트를 히트하지 않았습니다. 히트된 오브젝트: {hit.collider.gameObject.name}");
            }
        }
        else
        {
            Debug.Log("레이캐스트가 아무것도 히트하지 않았습니다.");
        }
    }
} 
