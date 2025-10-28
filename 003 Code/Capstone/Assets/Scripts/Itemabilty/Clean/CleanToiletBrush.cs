using Unity.Netcode;
using UnityEngine;

public class CleanToiletBrush : CleanItem
{
    /*
     * 변기솔(3) : (공격력, 방어) 200%의 데미지를 주고, 나 자신에게 50%의 데미지를 줌 
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_26_Napalm";
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 3;
        _interactionType = InteractionType.Throw;  // 투척형으로 설정
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
        // userInfo에서 자신이거나 상대를 구분할 수 있는 변수가 있음

        float enemyDamage = _stat * 2.0f * CleanPassive.GetInstance().AdditionalDamage();
        float selfDamage = _stat * 0.5f * CleanPassive.GetInstance().AdditionalDamage();

        if (!NetworkManager.Singleton.IsHost)
        {
            GameManager.GetInstance().AttackServerRpc((int)enemyDamage, _stamina);
            GameManager.GetInstance().AttackServerRpc((int)selfDamage, _stamina, true);
        }
        else
        {
            GameManager.GetInstance().Attack((int)enemyDamage, _stamina, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().Attack((int)selfDamage, _stamina, !NetworkManager.Singleton.IsHost);
        }

        
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서
    }
}
