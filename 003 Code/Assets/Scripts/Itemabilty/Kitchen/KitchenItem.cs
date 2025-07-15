using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public abstract class KitchenItem : BaseItem
{
    // 상호작용 타입 정의
    public enum InteractionType
    {
        Throw,      // 투척형
        Consume,    // 소모형
        Install     // 설치형
    }

    protected InteractionType _interactionType;
    protected XRGrabInteractable _grabInteractable;
    protected Rigidbody _rb;

    protected virtual void Awake()
    {
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

        // 이벤트 시스템 설정
        _grabInteractable.selectEntered.AddListener(OnGrab);
        _grabInteractable.selectExited.AddListener(OnRelease);
    }

    protected virtual void OnGrab(SelectEnterEventArgs args)
    {
        _isShooted = false;
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
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
        Shoot();
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

    protected virtual void Install()
    {
        // 설치형 아이템의 기본 설치 동작
        _rb.isKinematic = true;
        transform.position = new Vector3(transform.position.x, 0, transform.position.z);
    }

    protected virtual void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    public virtual void Init(int uid, int stat, int expireCount)
    {
        float additionalStat = 1.0f;

        KitchenPassive.GetInstance().UseKitchenItem(ref additionalStat);

        _isShooted = false;
        _uid = uid;
        _stat = (int)(stat * additionalStat);
        _expireCount = expireCount;
    }

    public abstract void Use();

    public virtual void Shoot()
    {
        if (_isShooted)
            return;

        _isShooted = true;

        Vector3 lookDir = transform.forward;
        Vector3 shootRotate;
        Vector3 shootDir;

        if (lookDir.z > 0)
        {
            shootRotate = new Vector3(45, 0, 0);
            shootDir = new Vector3(0, 1000, 1000);
        }
        else
        {
            shootRotate = new Vector3(-45, 0, 0);
            shootDir = new Vector3(0, -1000, 1000);
        }

        // 발사 방향 설정 후 발사
        transform.rotation = Quaternion.Euler(shootRotate);

        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.AddForce(shootDir);
    }
}
