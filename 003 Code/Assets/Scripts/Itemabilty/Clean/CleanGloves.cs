using Unity.Netcode;
using UnityEngine;

public class CleanGloves : CleanItem
{
    /*
     
    장갑(2) : (방어, 회피) 버프를 1턴 동안 부여합니다. (중첩 불가, 지속/해제 불가), ex) 장갑
     
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
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 1, NetworkManager.Singleton.IsHost);
    }
}
