using Unity.Netcode;
using UnityEngine;

public class WriteInstEraser : WriteInstItem
{
    /*
     
     지우개 (1) : (스테미나, 파괴) 자신의 모든 디버프 효과 제거
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_21_RecoveryField";
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
            GameManager.GetInstance().RemoveDeBuff(GameManager.BUFTYPE.INCATTACK, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().RemoveDeBuffServerRpc(GameManager.BUFTYPE.INCATTACK, NetworkManager.Singleton.IsHost);
    }
}
