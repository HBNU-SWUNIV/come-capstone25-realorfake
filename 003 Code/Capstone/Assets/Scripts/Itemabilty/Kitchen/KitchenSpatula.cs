using Unity.Netcode;
using UnityEngine;

public class KitchenSpatula : KitchenItem
{
    /*
     
    뒤집개 (1) : (스태미나, 적군) 스태미나 소모 없이 모든 버프/디버프 효과 반전 (예: 공격력 +20% -> -20%)
    
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_29_LumenCrash";
        _particleScale = 0.5f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ReverseBuff(!NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().ReverseBuffServerRpc(!NetworkManager.Singleton.IsHost);

    }
}
