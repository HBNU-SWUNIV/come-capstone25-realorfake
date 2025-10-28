using Unity.Netcode;
using UnityEngine;

public class KitchenCoffeepot : KitchenItem
{

    /*
     
    커피포트 (3) : (스태미나, 설치) 매 턴 50% 데미지 (2턴 지속), 2턴 후 사라짐
     
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_13_DangerClose";
        _particleScale = 0.3f;
        _installParticlePath = "Effect/Effect_38_GloryBoundary";
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 3;
        _expireCount = 2;
        _interactionType = InteractionType.Install;
    }

    public override void InstallPassive()
    {
        --_expireCount;
        _stamina = 0;

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 0.5f * KitchenPassive.GetInstance().AdditionalDamage()), _stamina);
        else
            GameManager.GetInstance().Attack((int)(_stat * 0.5f * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);

    }

    public override void Use()
    {
        base.Use();
    }
}
