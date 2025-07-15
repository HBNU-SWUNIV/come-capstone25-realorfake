using Unity.Netcode;
using UnityEngine;

public class KitchenCup : KitchenItem
{
    /*
     
     컵 (2) : (스태미나, 체력) 70% 데미지 + 20% 확률로 스태미나 소모 없이 공격 가능

     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        Shoot();

        if (GameManager.GetInstance().Attack((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost))
        {
            if (Random.Range(0, 5) == 0)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.ZEROSTAMINA, 1, 1, !NetworkManager.Singleton.IsHost);
        }
    }
}
