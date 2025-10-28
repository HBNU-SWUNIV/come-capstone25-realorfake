using Unity.Netcode;
using UnityEngine;

public class KitchenMcup : KitchenItem
{

    /*
     
     계량컵 (1) : (스태미나, 체력) 100% 데미지, 매 턴 2턴 동안 자신의 공격력 +15%
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceBlockCrash";
        _particleScale = 0.3f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _interactionType = InteractionType.Throw;
    }

    public override void Use()
    {
        base.Use();
        // 코드 실행
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);
        // 데미지 계산
        // GameManager 함수를 호출해서 적 hp 감소

        float damage = _stat * 1.0f * KitchenPassive.GetInstance().AdditionalDamage();

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)damage, _stamina);
        else
            GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 막대기가 떨어진 것을 감지
        // 다시 호출하기에는 문자열이므로

        // 2턴 동안 공격력 +15%
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 0.15f, 2, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCATTACK, 0.15f, 2, NetworkManager.Singleton.IsHost);
    }
}
