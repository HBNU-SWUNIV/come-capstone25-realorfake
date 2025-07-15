using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Transform cameraTransform; // 메인 카메라 Transform
    public float distanceFromCamera = 2f; // 카메라로부터의 거리
    public float smoothSpeed = 5f; // 부드럽게 따라오는 속도

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        // 목표 위치 계산 (카메라 앞쪽으로 지정된 거리만큼 떨어진 곳)
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // 항상 카메라를 바라보도록 회전
        transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
    }
}
