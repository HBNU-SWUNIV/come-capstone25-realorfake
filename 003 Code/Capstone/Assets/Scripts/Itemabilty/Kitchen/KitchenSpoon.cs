using Unity.Netcode;
using UnityEngine;

public class KitchenSpoon : KitchenItem
{

    /*
     
     숟가락 (1) : (스태미나, 체력) 90% 데미지 + 체력 10% 회복
     
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
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);
        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), _stamina);
        else
            GameManager.GetInstance().Attack((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);

        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.HEAL, 0.1f, 1, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.HEAL, 0.1f, 1, NetworkManager.Singleton.IsHost);
    }
}
