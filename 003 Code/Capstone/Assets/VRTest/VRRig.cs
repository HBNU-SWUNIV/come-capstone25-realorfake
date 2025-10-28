using UnityEngine;
using UnityEngine.Analytics;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;
    public void Map()
    {
        ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

public class VRRig : MonoBehaviour
{
    [Range(0, 1)]
    public float turnSmoothness = 0.1f;

    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    // ���� ������ ���� ���ο� ���� �߰�
    [Range(-3f, 3f)] // �ʿ��� ������ ���� ����
    public float heightOffset = 0f;

    void LateUpdate()
    {
        if (head.vrTarget == null || leftHand.vrTarget == null || rightHand.vrTarget == null)
            return;

        // ���� �������� �����Ͽ� ���� ��ġ ����
        Vector3 adjustedPosition = head.ikTarget.position + headBodyPositionOffset;
        adjustedPosition.y += heightOffset; // ���� ������ ����
        transform.position = adjustedPosition;

        // ���� ȸ���� ����� Yaw ���� �°� �ε巴�� ����
        float yaw = head.vrTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z),
            turnSmoothness
        );

        // VR Ÿ�ٰ� IK Ÿ�� ����
        head.Map();
        leftHand.Map();
        rightHand.Map();
    }
}