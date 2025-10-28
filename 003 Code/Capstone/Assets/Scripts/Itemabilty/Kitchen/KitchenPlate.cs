using Unity.Netcode;
using UnityEngine;

public class KitchenPlate : KitchenItem
{
    /*
     
    접시 (2) : (스태미나, 소모) 데미지를 막아주는 보호막 생성 (최대 100% 데미지까지)
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_09_HolyShield";
        _particleScale = 1.5f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        GameManager.GetInstance().SetShield((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), NetworkManager.Singleton.IsHost);

    }
}
