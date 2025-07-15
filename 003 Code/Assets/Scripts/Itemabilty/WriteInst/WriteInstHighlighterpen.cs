using Unity.Netcode;
using UnityEngine;

public class WriteInstHighlighterpen : WriteInstItem
{
    /*
     
     형광펜 (1) : (스테미나, 파괴) 자신에게 받는 데미지 20% 감소 + 공격력 15% 증가, 3턴
     
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
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.2f, 3, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 0.15f, 3, NetworkManager.Singleton.IsHost);
        }
    }
}
