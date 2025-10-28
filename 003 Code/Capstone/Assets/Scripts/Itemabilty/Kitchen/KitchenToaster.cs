using Unity.Netcode;
using UnityEngine;

public class KitchenToaster : KitchenItem
{
    /*
     
     토스터기 (2) : (스태미나, 설치) 매 턴 40% 데미지 (2회 사용 가능), 3턴 후 사라짐
     
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
        _stamina = 2;
        _expireCount = 3;
        _interactionType = InteractionType.Install;
    }


    public override void InstallPassive()
    {
        --_expireCount;

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 0.4f * KitchenPassive.GetInstance().AdditionalDamage()), 0);
        else
            GameManager.GetInstance().Attack((int)(_stat * 0.4f * KitchenPassive.GetInstance().AdditionalDamage()), 0, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        base.Use();
    }
}
