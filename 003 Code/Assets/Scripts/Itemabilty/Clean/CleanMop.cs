using Unity.Netcode;
using UnityEngine;

public class CleanMop : CleanItem
{

    /*
     
    대걸레(1) : (공격력, 방어) 100%의 데미지를 주고, (5% - 공격력 2배, 5% - 공격실패, 90% - 정상 공격)
     
     */


    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;  // 이 한 줄만 추가
        base.Awake();
    }
    public override void Use()
    {
        // 코드 발생

        // 플레이어 앞에 카드뭉치가 있어야 함
        // 카드 사용 필요
        _stamina = 1;

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

        float d = 1.0f;
        int r = Random.Range(0, 100);

        if (r < 5)
            d = 0; // 공격 실패
        else if (5 <= r && r < 10)
            d = 2.0f; // 공격력 2배
        
        // 최종 데미지 계산
        float damage = _stat * d * CleanPassive.GetInstance().AdditionalDamage();

        // 공격
        GameManager.GetInstance().Attack( (int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서
    }
}
