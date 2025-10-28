using Unity.Netcode;
using UnityEngine;

public class WriteInstTape : WriteInstItem
{
    /*
     
     테이프 (2) : (스테미나, 설치) 상대 행동 불가(ZEROSTAMINA), 자신의 스테미나 최대 4 고정
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_48_BondageChain";
        _particleScale = 0.5f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Consume;
    }
    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.ZEROSTAMINA, 1, 1, !NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.ZEROSTAMINA, 1, 1, !NetworkManager.Singleton.IsHost);

        int playerstamina = GameManager.GetInstance().GetMaxStamina(NetworkManager.Singleton.IsHost) + 1;

        if (playerstamina > 4)
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCSTAMINA, -(playerstamina - 4), 1, NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCSTAMINA, -(playerstamina - 4), 1, NetworkManager.Singleton.IsHost);
        }
    }
}
