using Unity.Netcode;
using UnityEngine;

public class CleanMop : CleanItem
{

    /*
     
    대걸레(1) : (공격력, 방어) 100%의 데미지를 주고, (5% - 공격력 2배, 5% - 공격실패, 90% - 정상 공격)
     
     */


    protected override void Awake()
    {
        _particlePath = "Effect/Effect_26_Napalm";
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _interactionType = InteractionType.Throw;  // 이 한 줄만 추가
    }
    public override void Use()
    {
        base.Use();
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);
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
        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)damage, _stamina);
        else
            GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서
    }
}
