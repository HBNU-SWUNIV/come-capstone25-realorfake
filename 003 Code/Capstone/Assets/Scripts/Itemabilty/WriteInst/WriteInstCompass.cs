using Unity.Netcode;
using UnityEngine;

public class WriteInstCompass : WriteInstItem
{
    /*
     
    나침반 (3) : (스테미나, 위치) 방어 무시 60% 증가(데미지 증가) 효과 (3턴 지속)
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_15_PowerOfGravity";
        _particleScale = 0.5f;
        _installParticlePath = "Effect/Effect_31_LumenJudgement";
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

        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().IgnoreGuardAttack((int)(_stat * 0.6f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().IgnoreGuardAttackServerRpc((int)(_stat * 0.6f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        base.Use();
    }
}
