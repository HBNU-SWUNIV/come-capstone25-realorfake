using Unity.Netcode;
using UnityEngine;

public class WriteInstRuler : WriteInstItem
{
    /*
     
    자 (2) : (스테미나, 데미지) 80% 증가 및 2회 공격
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_26_Napalm";
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
        Shoot();

        base.Use();
        if (!NetworkManager.Singleton.IsHost)
        {
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 0.8f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina);
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 0.8f * WriteInstPassive.GetInstance().AdditionalDamage()), 0);
        }
        else
        {
            GameManager.GetInstance().Attack((int)(_stat * 0.8f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().Attack((int)(_stat * 0.8f * WriteInstPassive.GetInstance().AdditionalDamage()), 0, NetworkManager.Singleton.IsHost);
        }
            

    }
}
