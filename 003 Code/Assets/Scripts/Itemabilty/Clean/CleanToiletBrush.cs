using Unity.Netcode;
using UnityEngine;

public class CleanToiletBrush : CleanItem
{
    /*
     * 변기솔(3) : (공격력, 방어) 200%의 데미지를 주고, 나 자신에게 50%의 데미지를 줌 
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;  // 투척형으로 설정
        base.Awake();
    }

    public override void Use()
    {
        // 코드 발생

        // 플레이어 앞에 카드뭉치가 있어야 함
        // 카드 사용 필요
        _stamina = 3;

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
        // GameManager 함수를 호출해서 hp 감소
        // userInfo에서 자신이거나 상대를 구분할 수 있는 변수가 있음

        float enemyDamage = _stat * 2.0f * CleanPassive.GetInstance().AdditionalDamage();
        float selfDamage = _stat * 0.5f * CleanPassive.GetInstance().AdditionalDamage();
        
        GameManager.GetInstance().Attack((int)enemyDamage, _stamina, NetworkManager.Singleton.IsHost);
        GameManager.GetInstance().Attack((int)selfDamage, _stamina, !NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서
    }
}
