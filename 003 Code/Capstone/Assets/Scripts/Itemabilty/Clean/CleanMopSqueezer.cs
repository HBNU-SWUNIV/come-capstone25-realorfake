using Unity.Netcode;
using UnityEngine;

public class CleanMopSqueezer : CleanItem
{
    /*
     * 행주 짜개(2) : (공격력, 치명) 1턴 자신의 공격력을 2배 증가시킴
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 1.0f, 1, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCATTACK, 1.0f, 1, NetworkManager.Singleton.IsHost);

    }
}
