using Unity.Netcode;
using UnityEngine;

public class CleanDustpan : CleanItem
{

    /*
     
    �����ޱ�(1) : (��Ƽ��, �Ҹ�) 30%�� Ȯ���� ���带 1ȸ �ο��ϰ� �Ҹ��Ѵ�
     
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
        base.Awake();
    }
    public override void Use()
    {
        // ���

        _stamina = 1;

        // 30%�� Ȯ���� ���� �ο�
        if (Random.Range(0, 10) < 3)
        {
            if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 100, NetworkManager.Singleton.IsHost);
        }
    }
}
