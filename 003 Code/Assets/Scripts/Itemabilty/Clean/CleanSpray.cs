using Unity.Netcode;
using UnityEngine;

public class CleanSpray : CleanItem
{
    /*
     * 스프레이(2) : (공격력, 방어) 맞는 대상의 효과를 무작위로. 
     * 랜덤 효과 (일반 공격 - 적 공격력 10% 감소, 행주로 공격 - 적 방어력 10% 감소)
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;  // 투척형으로 설정
        base.Awake();
    }

    public override void Use()
    {
        _stamina = 2;

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

        // 발생 방향 설정 후 발생
        transform.rotation = Quaternion.Euler(shootRotate);

        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.AddForce(shootDir);

        // 데미지 계산
        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
        {
            if (Random.Range(0, 2) == 0)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, -0.2f, 100, NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, -0.2f, 100, NetworkManager.Singleton.IsHost);
        }
    }
}
