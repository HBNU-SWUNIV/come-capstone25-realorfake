using Unity.Netcode;
using UnityEngine;

public class WriteInstTape : WriteInstItem
{
    /*
     
     테이프 (2) : (스테미나, 설치) 상대 행동 불가(ZEROSTAMINA), 자신의 스테미나 최대 4 감소
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.ZEROSTAMINA, 1, 1, !NetworkManager.Singleton.IsHost);

            int playerstamina = GameManager.GetInstance().GetStamina(NetworkManager.Singleton.IsHost);
            
            if (playerstamina > 4)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCSTAMINA, -4, 1, NetworkManager.Singleton.IsHost);
        }
    }
}
