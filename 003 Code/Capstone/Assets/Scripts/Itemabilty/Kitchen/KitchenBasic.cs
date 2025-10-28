using Unity.Netcode;
using UnityEngine;

public class KitchenBasic : KitchenItem
{
    /*
     
     기본(0) : 공격 불가 아이템, 공격 불가 상태에서만 사용 가능
     
     */

    public override void Init(int oid, int stat, int expireCount)
    {
        _stamina = 0;
        _isShooted = false;
        _oid = 0;
        _stat = 0;
        _expireCount = 1;
    }

    private void OnEnable()
    {
        _particlePath = "Effect/Effect_21_RecoveryField";
        _stamina = 0;
        _isShooted = false;
        _oid = 0;
        _stat = 0;
        _expireCount = 1;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        KitchenPassive.GetInstance().ResetUseCount();
        
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().RemoveBuff(GameManager.BUFTYPE.ZEROSTAMINA, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().RemoveBuffServerRpc(GameManager.BUFTYPE.ZEROSTAMINA, NetworkManager.Singleton.IsHost);
    }
}
