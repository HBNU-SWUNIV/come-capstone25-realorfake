using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ItemCase : MonoBehaviour
{
    private const int CARDS_PER_PAGE = 18;

    [SerializeField] private XRRayInteractor rightHandRayInteractor;

    private CardArrangement cardArrangement;
    private CardManager cardManager;
    private int currentPage;

    private void Start()
    {
        InitializeComponents();
        StartCoroutine(LoadInitialCardsAsync());
    }

    private void InitializeComponents()
    {
        InitializeCardArrangement();
        InitializeCardManager();
        ValidateRayInteractor();
    }

    private void InitializeCardArrangement()
    {
        cardArrangement = GetComponent<CardArrangement>();
        if (cardArrangement == null)
        {
            Debug.LogError("CardArrangement 컴포넌트를 찾을 수 없습니다!");
        }
    }

    private void InitializeCardManager()
    {
        cardManager = FindAnyObjectByType<CardManager>();
        if (cardManager == null)
        {
            Debug.LogError("CardManager 인스턴스를 찾을 수 없습니다!");
        }
    }

    private void ValidateRayInteractor()
    {
        if (rightHandRayInteractor == null)
        {
            Debug.LogWarning("Right Hand XR Ray Interactor가 ItemCase 스크립트에 할당되지 않았습니다.");
        }
    }

    private System.Collections.IEnumerator LoadInitialCardsAsync()
    {
        if (cardManager != null)
        {
            // 카드 데이터가 로드될 때까지 대기
            while (cardManager.allCardData == null)
            {
                yield return null;
            }
            UpdateCardDisplay();
        }
    }

    public void UpdateCardDisplay()
    {
        if (cardManager == null)
        {
            Debug.LogError("CardManager가 할당되지 않았습니다.");
            return;
        }

        int startIndex = currentPage * CARDS_PER_PAGE;
        int endIndex = startIndex + CARDS_PER_PAGE;
        cardManager.LoadAndArrangePage(startIndex, endIndex);
    }
}
