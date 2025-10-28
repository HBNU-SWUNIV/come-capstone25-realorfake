using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 아이템의 기본 클래스
/// 모든 아이템이 상속받아야 하는 기본 기능을 제공합니다.
/// </summary>
public abstract class BaseItem : MonoBehaviour
{
    public enum InteractionType
    {
        Throw,      // 투척형
        Consume,    // 소모형
        Install     // 설치형
    }

    public enum ParticleType
    {
        Use,
        Install,
        Buff
    }

    public enum ParticleTarget
    {
        Self,
        Enemy
    }

    protected InteractionType _interactionType;
    protected int _stamina;        // 아이템 사용에 필요한 스태미나
    protected bool _isShooted;     // 아이템 사용 여부
    protected int _oid;           // 아이템 고유 ID
    protected int _stat;          // 아이템 효과 수치
    protected int _expireCount;    // 아이템 효과 지속 턴 수
    protected bool _isInstalled;   // 아이템 설치 여부
    protected string _particlePath;
    protected float _particleScale = 1.0f;
    protected ParticleTarget _particleTarget = ParticleTarget.Self;
    protected string _installParticlePath;
    protected float _installParticleScale = 1.0f;
    protected ParticleTarget _installParticleTarget = ParticleTarget.Self;
    protected string _soundName;

    /// <summary>
    /// 아이템 초기화
    /// </summary>
    public virtual void Init(int oid, int stat, int expireCount)
    {
        _isShooted = false;
        _isInstalled = false;
        _oid = oid;
        _stat = stat;
        _expireCount = expireCount;
    }

    private void OnDisable()
    {
        _isShooted = false;
        _isInstalled = false;
        _expireCount = 0;
    }

    /// <summary>
    /// 아이템 설치
    /// </summary>
    public virtual void Install()
    {
        if (!_isInstalled)
        {
            _isInstalled = true;
            // InstallPassive();
        }
    }

    /// <summary>
    /// 아이템 효과 만료 여부 확인
    /// </summary>
    public bool IsExpired()
    {
        if (_expireCount != 0)
            return false;
        return true;
    }

    /// <summary>
    /// 아이템 효과 지속 턴 수 증가
    /// </summary>
    public void IncreaseExpireCount()
    {
        if (_expireCount != -1)
            _expireCount++;
    }

    /// <summary>
    /// 아이템 효과 수치 증가
    /// </summary>
    public void IncreaseStats(float value)
    {
        _stat = (int)(_stat * (1 + value));
    }

    /// <summary>
    /// 설치된 아이템의 효과 적용
    /// </summary>
    public virtual void InstallPassive()
    {
        // 기본 구현은 비어있음. 자식 클래스에서 오버라이드하여 구현
    }

    public virtual void InstallActive()
    {

    }

    /// <summary>
    /// 아이템 효과 지속 턴 수 감소
    /// </summary>
    public void DecreaseExpireCount()
    {
        if (_expireCount != -1)
            _expireCount--;
    }

    /// <summary>
    /// 현재 남은 턴 수 반환
    /// </summary>
    public int GetExpireCount()
    {
        return _expireCount;
    }

    public int GetOid()
    {
        return _oid;
    }

    public int GetStat()
    {
        return _stat;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public virtual void Uninstall()
    {
        _isInstalled = false;
    }

    /// <summary>
    /// 설치 여부 확인
    /// </summary>
    public bool IsInstalled()
    {
        return _isInstalled;
    }

    /// <summary>
    /// 아이템 사용 여부 확인
    /// </summary>
    public bool IsShooted()
    {
        return _isShooted;
    }

    public void ClearShoot()
    {
        _isShooted = false;
    }

    /// <summary>
    /// 아이템 사용
    /// </summary>
    public abstract void Use();

    /// <summary>
    /// 아이템 사용에 필요한 스태미나 반환
    /// </summary>
    public int GetStamina()
    {
        return _stamina;
    }

    public InteractionType GetInteractionType()
    {
        return _interactionType;
    }
    public virtual void Shoot()
    {
        if (_isShooted)
            return;

        Collider col = this.GetComponent<Collider>();
        col.enabled = false;

        _isShooted = true;

        Vector3 lookDir = transform.forward;
        Vector3 shootRotate;
        Vector3 shootDir;

        if (lookDir.z > 0)
        {
            shootRotate = new Vector3(45, 0, 0);
            shootDir = new Vector3(0, 300, 1000);
        }
        else
        {
            shootRotate = new Vector3(-45, 0, 0);
            shootDir = new Vector3(0, 300, -1000);
        }

        // 발사 방향 설정 후 발사
        transform.rotation = Quaternion.Euler(shootRotate);

        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddForce(shootDir);
    }

    public void PlayInstallParticle()
    {
        GameObject r = Resources.Load(_installParticlePath) as GameObject;
        StartCoroutine(PlayParticleCoroutine(r, ParticleType.Install));

        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().PlayParticleClientRpc(_oid);
        else
            GameManager.GetInstance().PlayParticleServerRpc(_oid);

        FindAnyObjectByType<SoundManager>()?.PlaySound(_soundName);
    }

    public void PlayUseParticle(Transform _enemy)
    {
        // 상대방 위치면 다른 위치에 파티클 보여줘야 함
        GameObject r = Resources.Load(_particlePath) as GameObject;
        if (r != null)
            StartCoroutine(PlayParticleCoroutine(r, ParticleType.Use, _enemy));

        FindAnyObjectByType<SoundManager>()?.PlaySound(_soundName);
    }

    IEnumerator PlayParticleCoroutine(GameObject r, ParticleType type, Transform _enemy = null)
    {

        GameObject go = Instantiate(r);
        if (type == ParticleType.Install)
        {
            go.transform.localScale = new Vector3(_installParticleScale, _installParticleScale, _installParticleScale);
            go.transform.localScale = new Vector3(_particleScale, _particleScale, _particleScale);
            if (_installParticleTarget == ParticleTarget.Self)
            {
                go.transform.position = transform.position;
            }
            else if (_installParticleTarget == ParticleTarget.Enemy)
            {
                // 상대방 위치 
                if (_enemy != null)
                    go.transform.position = _enemy.position;
            }   
        } 
        else if (type == ParticleType.Use)
        {
            go.transform.localScale = new Vector3(_particleScale, _particleScale, _particleScale);
            if (_particleTarget == ParticleTarget.Self)
            {
                go.transform.position = transform.position;
            } else if (_particleTarget == ParticleTarget.Enemy)
            {
                // 상대방 위치 
                if (_enemy != null)
                    go.transform.position = _enemy.position;
            }
        } 
        else if (type == ParticleType.Buff)
        {
            if (_enemy != null)
                go.transform.position = _enemy.position;
        }

        yield return new WaitForSeconds(5);
        Destroy(go);
    }



}
