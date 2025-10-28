using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// 청소 아이템의 기본 클래스
/// 모든 청소 아이템이 상속받아야 하는 기본 기능을 제공합니다.
/// </summary>
public abstract class CleanItem : BaseItem
{
    protected XRGrabInteractable _grabInteractable;
    protected Rigidbody _rb;

    protected virtual void Awake()
    {
        // XR 상호작용 컴포넌트 설정
        SetupXRComponents();
    }

    private void SetupXRComponents()
    {
        // XR Grab Interactable 컴포넌트 추가
        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable == null)
        {
            _grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grabInteractable.throwOnDetach = true;
        }

        // Rigidbody 컴포넌트 추가
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.useGravity = true;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // 새로운 이벤트 시스템 사용
        _grabInteractable.selectEntered.AddListener(OnGrab);
        _grabInteractable.selectExited.AddListener(OnRelease);
    }

    protected virtual void OnGrab(SelectEnterEventArgs args)
    {
        // 잡았을 때의 기본 동작
        _isShooted = false;
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
        // 취소된 경우 처리하지 않음
        if (args.isCanceled) return;

        switch (_interactionType)
        {
            case InteractionType.Throw:
                HandleThrow();
                break;
            case InteractionType.Consume:
                HandleConsume();
                break;
            case InteractionType.Install:
                HandleInstall();
                break;
        }
    }

    protected virtual void HandleThrow()
    {
        // 던진 속도가 5f 이상일 때 효과 발동
        if (_rb.linearVelocity.magnitude > 5f)
        {
            Use();
        }
    }

    protected virtual void HandleConsume()
    {
        // 소모형 아이템은 바로 사용
        Use();
    }

    protected virtual void HandleInstall()
    {
        // 바닥 감지
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.1f))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                Install();
            }
        }
    }

    protected virtual void OnDestroy()
    {
        // 이벤트 해제
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    /// <summary>
    /// 아이템 초기화
    /// </summary>
    /// <param name="uid">아이템 고유 ID</param>
    /// <param name="stat">아이템 효과 수치</param>
    /// <param name="expireCount">아이템 효과 지속 턴 수</param>
    public override void Init(int uid, int stat, int expireCount)
    {
        base.Init(uid, stat, expireCount);
    }

    /// <summary>
    /// 아이템 사용
    /// 각 청소 아이템마다 구현해야 하는 추상 메서드
    /// </summary>
    public override void Use()
    {
        // _isShooted = true;
        CleanPassive.GetInstance().UseCleanItem();
    }

    
}
