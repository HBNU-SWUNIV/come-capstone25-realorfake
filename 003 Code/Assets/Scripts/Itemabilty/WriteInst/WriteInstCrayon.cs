using Unity.Netcode;
using UnityEngine;

public class WriteInstCrayon : WriteInstItem
{
    /*
     
     크레용 (2) : (스테미나, 데미지) 증가 및 위치 이동 2회 변경
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
        {
            Shoot();
            GameManager.GetInstance().DestroyItem(2, NetworkManager.Singleton.IsHost);
        }
    }
}
