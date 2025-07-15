using Unity.Netcode;
using UnityEngine;

public class CleanDuster : CleanItem
{

    /*
     
    먼지털이개(2) : (공격력, 방어) 70%의 데미지를 주고, 맞은 쪽의 데미지 +30% / 맞은 방어력 + 30%
     
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;  // 소모형으로 설정
        base.Awake();
    }
    public override void Use()
    {
        // 코드 발생

        // 플레이어 앞에 카드뭉치가 있어야 함
        // 카드 사용 필요

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

        float damage = _stat * 0.7f * CleanPassive.GetInstance().AdditionalDamage();

        // 공격
        GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서


        // 맞은 쪽 데미지 + 30%, 맞은 방어력 +30%
        GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 0.3f, 1, NetworkManager.Singleton.IsHost);
        GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, -0.3f, 1, NetworkManager.Singleton.IsHost);
    }
}
