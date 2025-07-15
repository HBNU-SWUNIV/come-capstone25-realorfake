using Unity.Netcode;
using UnityEngine;

public class CleanMopSqueezer : CleanItem
{
    /*
     * 행주 짜개(2) : (공격력, 치명) 자신의 공격력을 2배 증가시킴
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
        base.Awake();
    }

    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 1.0f, 1, NetworkManager.Singleton.IsHost);
    }
}
