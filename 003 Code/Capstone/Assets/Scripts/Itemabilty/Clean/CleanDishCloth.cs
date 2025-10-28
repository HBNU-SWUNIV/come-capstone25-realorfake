using Unity.Netcode;
using UnityEngine;

public class CleanDishCloth : CleanItem
{
    /*
     
    행주(1) : (방어, 회피) 자신의 공격력을 2% 증가
     
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
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 0.02f, 100, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCATTACK, 0.02f, 100, NetworkManager.Singleton.IsHost);

    }
}
