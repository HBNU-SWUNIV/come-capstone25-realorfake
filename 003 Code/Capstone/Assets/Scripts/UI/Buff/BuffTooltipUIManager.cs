using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;

public class BuffTooltipUIManager : MonoBehaviour
{
    public static BuffTooltipUIManager Instance;

    [Header("버프 툴팁 UI")]
    public GameObject buffTooltipPanel;
    public TextMeshProUGUI buffNameText;
    public TextMeshProUGUI buffDescriptionText;

    [Header("VR Interaction")]
    private XRRayInteractor rayInteractor;
    private bool isInteractorInitialized = false; // 초기화 상태 플래그 추가

    [Header("Buff UI")]
    public GameObject userUI;
    public GameObject playerPanel;
    public GameObject opponentPanel;

    public GameObject playerBufTransform;
    public GameObject opponentBufTransform;

    public Slider playerHpSlider;
    public Slider opponentHpSlider;

    [Header("Buff")]
    public GameObject incAttack;
    public GameObject incDefense;
    public GameObject attackMiss;
    public GameObject guard;
    public GameObject heal;
    public GameObject zeroStamina;
    public GameObject bleed;
    public GameObject incStamina;
    public GameObject x2;
    public GameObject shield;
    public GameObject unheal;
    public GameObject reflect;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (buffTooltipPanel != null) buffTooltipPanel.SetActive(false);
        else Debug.LogError("[BuffTooltipUIManager] 'buffTooltipPanel'이 연결되지 않았습니다!");
    }

    // OnEnable로 변경하여 컴포넌트가 활성화될 때마다 Interactor를 찾도록 함
    private void OnEnable()
    {
        if (!isInteractorInitialized)
        {
            StartCoroutine(FindAndInitializeInteractor());
        }
    }

    private void OnDisable()
    {
        if (rayInteractor != null && isInteractorInitialized)
        {
            rayInteractor.hoverEntered.RemoveListener(ShowTooltipOnHover);
            rayInteractor.hoverExited.RemoveListener(HideTooltipOnHover);
        }
    }

    // CardTooltipUIManager.cs와 동일한 동적 탐색 코루틴
    private IEnumerator FindAndInitializeInteractor()
    {
        Debug.Log("[BuffTooltipUIManager] XRRayInteractor 탐색을 시작합니다...");

        while (rayInteractor == null)
        {
            rayInteractor = FindObjectOfType<XRRayInteractor>();

            if (rayInteractor == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("[BuffTooltipUIManager] XRRayInteractor를 찾았습니다! 이벤트를 초기화합니다.", rayInteractor.gameObject);

        rayInteractor.hoverEntered.AddListener(ShowTooltipOnHover);
        rayInteractor.hoverExited.AddListener(HideTooltipOnHover);

        isInteractorInitialized = true;
    }


    public void ShowTooltip(BuffIconDisplay buff)
    {
        if (buffTooltipPanel == null || buff == null) return;
        
        buffNameText.text = buff.buffName;

        string baseDescription = buff.buffDescription;
        string finalDescription = baseDescription;

        if (buff.durationText != null && !string.IsNullOrEmpty(buff.durationText.text))
        {
            finalDescription = $"{baseDescription} ({buff.durationText.text}턴)";
        }
        
        buffDescriptionText.text = finalDescription;

        buffTooltipPanel.transform.position = buff.transform.position + new Vector3(0.5f, 0f, -0.1f);
        //서버 적용시 카드 위치 조정
        /*
        if (NetworkManager.Singleton.IsHost){
            buffTooltipPanel.transform.position = buff.transform.position + new Vector3(0.5f, 0f, -0.1f);
        }
        if (NetworkManager.Singleton.IsClient){
            buffTooltipPanel.transform.position = buff.transform.position + new Vector3(0.5f, 0f, 0.1f);
        }
        */
        buffTooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (buffTooltipPanel != null)
        {
            buffTooltipPanel.SetActive(false);
        }
    }

    private void ShowTooltipOnHover(HoverEnterEventArgs args)
    {
        BuffIconDisplay buffIcon = args.interactableObject.transform.GetComponent<BuffIconDisplay>();
        if (buffIcon != null)
        {
            Debug.Log($"[BuffTooltip] Ray Hover 감지! 닿은 버프 이름: {buffIcon.buffName}"); 
            ShowTooltip(buffIcon);
        }
    }

    private void HideTooltipOnHover(HoverExitEventArgs args)
    {
        HideTooltip();
    }

    public void AddBuff(GameManager.BUFTYPE type, int turn, bool isPlayer)
    {
        if (isPlayer)
        {
            switch (type)
            {
                case GameManager.BUFTYPE.INCATTACK:
                    {
                        GameObject tmp = Instantiate(incAttack, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.INCDEFENSE:
                    {
                        GameObject tmp = Instantiate(incDefense, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.ATTACKMISS:
                    {
                        GameObject tmp = Instantiate(attackMiss, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.GUARD:
                    {
                        GameObject tmp = Instantiate(guard, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.HEAL:
                    {
                        GameObject tmp = Instantiate(heal, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.ZEROSTAMINA:
                    {
                        GameObject tmp = Instantiate(zeroStamina, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.BLEED:
                    {
                        GameObject tmp = Instantiate(bleed, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.INCSTAMINA:
                    {
                        GameObject tmp = Instantiate(incStamina, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.X2:
                    {
                        GameObject tmp = Instantiate(x2, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.SHIELD:
                    {
                        GameObject tmp = Instantiate(shield, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.UNHEAL:
                    {
                        GameObject tmp = Instantiate(unheal, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.REFLECT:
                    {
                        GameObject tmp = Instantiate(reflect, playerBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
            }
        }else
        {
            switch (type)
            {
                case GameManager.BUFTYPE.INCATTACK:
                    {
                        GameObject tmp = Instantiate(incAttack, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.INCDEFENSE:
                    {
                        GameObject tmp = Instantiate(incDefense, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.ATTACKMISS:
                    {
                        GameObject tmp = Instantiate(attackMiss, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.GUARD:
                    {
                        GameObject tmp = Instantiate(guard, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.HEAL:
                    {
                        GameObject tmp = Instantiate(heal, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.ZEROSTAMINA:
                    {
                        GameObject tmp = Instantiate(zeroStamina, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.BLEED:
                    {
                        GameObject tmp = Instantiate(bleed, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.INCSTAMINA:
                    {
                        GameObject tmp = Instantiate(incStamina, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.X2:
                    {
                        GameObject tmp = Instantiate(x2, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.SHIELD:
                    {
                        GameObject tmp = Instantiate(shield, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.UNHEAL:
                    {
                        GameObject tmp = Instantiate(unheal, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
                case GameManager.BUFTYPE.REFLECT:
                    {
                        GameObject tmp = Instantiate(reflect, opponentBufTransform.transform);
                        tmp.GetComponentInChildren<TextMeshProUGUI>().text = turn.ToString();
                    }
                    break;
            }
        }
        
    }

    public void RemoveAllBuff(bool isPlayer)
    {
        if (isPlayer)
        {
            foreach (Transform child in playerBufTransform.transform)
            {
                Destroy(child.gameObject);
            }
        } else
        {
            foreach (Transform child in opponentBufTransform.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void SetHp(float hp, bool isPlayer)
    {
        if (isPlayer)
            playerHpSlider.value = hp;
        else
            opponentHpSlider.value = hp;
    }
}