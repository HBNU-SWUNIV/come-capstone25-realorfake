using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// 카드 툴팁 UI를 관리하고, VR 컨트롤러의 Ray Interactor 이벤트를 직접 감지하여
/// 툴팁을 표시하거나 숨기는 기능을 모두 담당하는 통합 매니저 스크립트입니다.
/// XR Origin이 씬에 동적으로 생성되는 경우에도 대응할 수 있도록 코루틴으로 Interactor를 탐색합니다.
/// </summary>
public class CardTooltipUIManager : MonoBehaviour
{
    // --- 싱글턴 인스턴스 ---
    public static CardTooltipUIManager Instance;

    // --- UI 요소 연결 ---
    [Header("UI Elements")]
    [Tooltip("툴팁의 부모가 되는 패널 UI 게임 오브젝트")]
    public GameObject tooltipPanel;
    [Tooltip("BigClass 설명을 표시할 TextMeshPro UI")]
    public TextMeshProUGUI bigClassText;
    [Tooltip("SmallClass 설명을 표시할 TextMeshPro UI")]
    public TextMeshProUGUI smallClassText;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    private bool isInteractorInitialized = false;

    /// <summary>
    /// 스크립트 인스턴스가 로드될 때 호출됩니다.
    /// 싱글턴 인스턴스를 설정하고 UI를 초기화합니다.
    /// </summary>
    void Awake()
    {
        // 싱글턴 패턴 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 툴팁 패널이 연결되어 있는지 확인하고 비활성화
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[CardTooltipUIManager] 'tooltipPanel'이 인스펙터에 연결되지 않았습니다!", gameObject);
        }
    }

    /// <summary>
    /// 컴포넌트가 활성화될 때 호출됩니다.
    /// Interactor가 아직 초기화되지 않았다면 탐색을 시작합니다.
    /// </summary>
    private void OnEnable()
    {
        if (!isInteractorInitialized)
        {
            StartCoroutine(FindAndInitializeInteractor());
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 호출됩니다.
    /// 연결된 Interactor 이벤트 리스너를 안전하게 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (rayInteractor != null && isInteractorInitialized)
        {
            rayInteractor.hoverEntered.RemoveListener(ShowTooltipOnHover);
            rayInteractor.hoverExited.RemoveListener(HideTooltipOnHover);
        }
    }

    /// <summary>
    /// 씬에 XRRayInteractor가 생성될 때까지 주기적으로 탐색하고,
    /// 찾으면 이벤트 리스너를 연결하여 초기화하는 코루틴입니다.
    /// </summary>
    private IEnumerator FindAndInitializeInteractor()
    {
        Debug.Log("[CardTooltipUIManager] XRRayInteractor 탐색을 시작합니다...");

        // rayInteractor를 찾을 때까지 무한 반복
        while (rayInteractor == null)
        {
            // 현재 씬에 로드된 XRRayInteractor 타입의 컴포넌트를 찾습니다.
            rayInteractor = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();

            // 찾지 못했다면 0.5초 대기 후 다시 시도합니다.
            if (rayInteractor == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Interactor를 성공적으로 찾았을 경우
        Debug.Log("[CardTooltipUIManager] XRRayInteractor를 찾았습니다! 이벤트를 초기화합니다.", rayInteractor.gameObject);

        // 이벤트 리스너 연결
        rayInteractor.hoverEntered.AddListener(ShowTooltipOnHover);
        rayInteractor.hoverExited.AddListener(HideTooltipOnHover);

        // 초기화 완료 플래그 설정
        isInteractorInitialized = true;
    }

    /// <summary>
    /// 특정 카드 데이터를 기반으로 툴팁을 화면에 표시합니다.
    /// </summary>
    /// <param name="card">툴팁을 표시할 대상 CardDisplay 컴포넌트</param>
    public void ShowTooltip(CardDisplay card)
    {
        if (tooltipPanel == null || card == null) return;
        if (DescriptionManager.Instance == null) return;

        // DescriptionManager를 통해 설명 텍스트를 가져옵니다.
        string bigDesc = DescriptionManager.Instance.GetBigClassDescription(card.GetBigClass());
        string smallDesc = DescriptionManager.Instance.GetSmallClassDescription(card);

        // UI 텍스트 업데이트
        bigClassText.text = bigDesc;
        smallClassText.text = smallDesc;

        // 툴팁 위치를 카드 옆으로 조정
        tooltipPanel.transform.position = card.transform.position + new Vector3(0.5f, 0f, -0.1f);

        //서버 적용시 카드 위치 조정
        /*
        if (NetworkManager.Singleton.IsHost){
            tooltipPanel.transform.position = card.transform.position + new Vector3(0.5f, 0f, -0.1f);
        }
        if (NetworkManager.Singleton.IsClient){
            tooltipPanel.transform.position = card.transform.position + new Vector3(0.5f, 0f, 0.1f);
        }
        */
        
        // 툴팁 활성화
        tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// 화면에 표시된 툴팁을 숨깁니다.
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    /// <summary>
    /// XRRayInteractor의 hoverEntered 이벤트에 연결될 콜백 함수입니다.
    /// </summary>
    private void ShowTooltipOnHover(HoverEnterEventArgs args)
    {
        // [디버그 추가] hoverEntered 이벤트가 발생했는지, 어떤 오브젝트에 닿았는지 확인합니다.
        Debug.Log($"[!!!] Ray Hover 감지! 닿은 오브젝트: {args.interactableObject.transform.name} {args.interactableObject.transform.position}", args.interactableObject.transform.gameObject);

        // Ray가 닿은 객체에서 CardDisplay 컴포넌트를 가져옵니다.
        CardDisplay cardDisplay = args.interactableObject.transform.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            // CardDisplay를 찾았다면 툴팁을 정상적으로 표시합니다.
            ShowTooltip(cardDisplay);
        }
        else
        {
            // [디버그 추가] CardDisplay 컴포넌트를 찾지 못했을 경우를 대비한 로그
            Debug.LogWarning($"[!!!] {args.interactableObject.transform.name} 오브젝트에는 CardDisplay 컴포넌트가 없습니다.", args.interactableObject.transform.gameObject);
        }
    }

    /// <summary>
    /// XRRayInteractor의 hoverExited 이벤트에 연결될 콜백 함수입니다.
    /// </summary>
    private void HideTooltipOnHover(HoverExitEventArgs args)
    {
        HideTooltip();
    }
}