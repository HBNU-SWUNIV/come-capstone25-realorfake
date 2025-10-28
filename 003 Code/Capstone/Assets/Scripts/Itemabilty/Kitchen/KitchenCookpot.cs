using Unity.Netcode;
using UnityEngine;

public class KitchenCookpot : KitchenItem
{
    /*
     
    냄비 (3) : (스태미나, 설치) 3턴 동안 적의 데미지 30% 감소 + 매턴 20% 데미지
     
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
        _expireCount = 3;
        _interactionType = InteractionType.Install;
    }

    public override void InstallPassive()
    {
        --_expireCount;

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 0.2f * KitchenPassive.GetInstance().AdditionalDamage()), _stamina);
        else
            GameManager.GetInstance().Attack((int)(_stat * 0.2f * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.3f, 3, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.3f, 3, NetworkManager.Singleton.IsHost);

    }

}
