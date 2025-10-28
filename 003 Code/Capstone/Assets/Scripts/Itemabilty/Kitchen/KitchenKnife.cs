using Unity.Netcode;
using UnityEngine;

public class KitchenKnife : KitchenItem
{

    /*
     
    칼(2) : (스태미나, 체력) 150% 데미지 공격, 20% 확률로 3턴 출혈(매턴10% 데미지)
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_11_LightInFullBloom";
        _particleScale = 0.3f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
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

        float damage = _stat * 1.5f * KitchenPassive.GetInstance().AdditionalDamage();

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)damage, _stamina);
        else
            GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 막대기가 떨어진 것을 감지
        // 다시 호출하기에는 문자열이므로

        // 출혈 적용
        if (Random.Range(0, 5) == 0)
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.BLEED, _stat * 0.1f, 3, !NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.BLEED, _stat * 0.1f, 3, !NetworkManager.Singleton.IsHost);
        }
            
    }
}
