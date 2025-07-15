using Unity.Netcode;
using UnityEngine;

public class KitchenBasic : KitchenItem
{
    /*
     
     기본(0) : 공격 불가 아이템, 공격 불가 상태에서만 사용 가능
     
     */

    public override void Init(int uid, int stat, int expireCount)
    {
        // CleanPassive.GetInstance().UseCleanItem();

        _isShooted = false;
        _uid = 0;
        _stat = 0;
        _expireCount = 1;
    }

    public override void Use()
    {
        _stamina = 0;

        KitchenPassive.GetInstance().ResetUseCount();
        GameManager.GetInstance().RemoveBuff(GameManager.BUFTYPE.ZEROSTAMINA, NetworkManager.Singleton.IsHost);
    }
}
