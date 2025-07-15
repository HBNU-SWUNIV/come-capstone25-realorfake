using Unity.Netcode;
using UnityEngine;

public class KitchenSpoon : KitchenItem
{

    /*
     
     숟가락 (1) : (스태미나, 체력) 90% 데미지 + 체력 10% 회복
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 1;

        Shoot();

        GameManager.GetInstance().Attack((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
        GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.HEAL, 0.1f, 1, NetworkManager.Singleton.IsHost);
    }
}
