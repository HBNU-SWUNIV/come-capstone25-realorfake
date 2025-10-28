using Unity.Netcode;
using UnityEngine;

public class WriteInstBallpen : WriteInstItem
{
    /*
     
    효과 (1) : (스테미나, 데미지) 120% 증가
     
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
        _interactionType = InteractionType.Throw;
    }

    public override void Use()
    {
        base.Use();
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 1.2f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina);
        else
            GameManager.GetInstance().Attack((int)(_stat * 1.2f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }
}
