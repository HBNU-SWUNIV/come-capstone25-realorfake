using Unity.Netcode;
using UnityEngine;

public class WriteInstEraser : WriteInstItem
{
    /*
     
     지우개 (1) : (스테미나, 파괴) 자신의 모든 디버프 효과 제거
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 1;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
        {
            GameManager.GetInstance().RemoveDeBuff(GameManager.BUFTYPE.INCATTACK, NetworkManager.Singleton.IsHost);
        }
    }
}
