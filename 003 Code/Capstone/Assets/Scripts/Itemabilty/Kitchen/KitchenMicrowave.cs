using Unity.Netcode;
using UnityEngine;

public class KitchenMicrowave : KitchenItem
{
    /*
     
    전자레인지 (3) : (스태미나, 적군) 데미지를 주는 모든 아이템들이 2배로 증가
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 3;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        // X2는 자신이 들고 있어야해서 어쩔 수 없음
        base.Use();
        GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.X2, 1, 1, NetworkManager.Singleton.IsHost);

    }
}
