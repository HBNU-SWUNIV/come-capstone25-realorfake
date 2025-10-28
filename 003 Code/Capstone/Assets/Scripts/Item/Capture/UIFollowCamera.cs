using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Transform cameraTransform; // ���� ī�޶� Transform
    public float distanceFromCamera = 2f; // ī�޶�κ����� �Ÿ�
    public float smoothSpeed = 5f; // �ε巴�� ������� �ӵ�

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        // ��ǥ ��ġ ��� (ī�޶� �������� ������ �Ÿ���ŭ ������ ��)
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

        // �ε巴�� �̵�
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // �׻� ī�޶� �ٶ󺸵��� ȸ��
        transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
    }
}
