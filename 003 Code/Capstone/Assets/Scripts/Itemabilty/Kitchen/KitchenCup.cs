using Unity.Netcode;
using UnityEngine;

public class KitchenCup : KitchenItem
{
    /*
     
     컵 (2) : (스태미나, 체력) 70% 데미지 + 20% 확률로 상대 다음턴 스태미나 0

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
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);


        if (NetworkManager.Singleton.IsHost)
        {
            GameManager.GetInstance().Attack((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
        }
        else
        {
            GameManager.GetInstance().AttackServerRpc((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), _stamina);
        }


        if (Random.Range(0, 5) == 0)
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.ZEROSTAMINA, 1, 1, !NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.ZEROSTAMINA, 1, 1, !NetworkManager.Singleton.IsHost);
        }
    }
}
