using Unity.Netcode;
using UnityEngine;

public class CleanSponge : CleanItem
{
    /*
     * 스펀지(1) : (방어, 회피) 자신의 방어력을 10% 증가
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
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.1f, 100, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.1f, 100, NetworkManager.Singleton.IsHost);

    }
}
