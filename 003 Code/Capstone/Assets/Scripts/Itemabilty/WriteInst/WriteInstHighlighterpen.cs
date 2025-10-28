using Unity.Netcode;
using UnityEngine;

public class WriteInstHighlighterpen : WriteInstItem
{
    /*
     
     형광펜 (1) : (스테미나, 파괴) 자신에게 받는 데미지 20% 감소 + 공격력 15% 증가, 3턴
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
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
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.2f, 3, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 0.15f, 3, NetworkManager.Singleton.IsHost);
        }
        else
        {
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.2f, 3, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCATTACK, 0.15f, 3, NetworkManager.Singleton.IsHost);
        }
    }
}
