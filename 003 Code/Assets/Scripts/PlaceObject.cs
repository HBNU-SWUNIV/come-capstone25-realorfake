using System.Collections.Generic;
using UnityEngine;

public class PlaceObject : MonoBehaviour
{
    public GameObject placeObject;
    private GameObject ghostObject;
    public float gridSize = 1f;
    private HashSet<Vector3> pushedPosition = new HashSet<Vector3>();

    /// <summary>
    /// 사용 예제
    ///
    /// 아이템 카드로 오브젝트 소환 시 CreateGhostObject 호출, placeObject 변수에는 소환한 오브젝트의 프리팹 넣어야 함
    ///
    /// 이후 UpdateGhostPostition에서 레이캐스트로 필드 위에 표시.
    /// 이 부분은 VR 기기에서 사용할 컨트롤러의 레이캐스트를 이용해야 할 듯함
    ///
    /// 설치 시 FPlaceObject 함수를 호출하여 해당 위치에 고정시킴
    /// 설치 완료 시 GhostObject 비활성화 하고, UpdateGhostPostition도 비활성화 -> 설치에 대한 반응도 비활성화 해야 함
    ///
    /// </summary>

    private void Start()
    {
        CreateGhostObject();
    }

    private void Update()
    {
        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
            FPlaceObject();
    }

    void CreateGhostObject()
    {
        ghostObject = Instantiate(placeObject);
        ghostObject.GetComponent<Collider>().enabled = false;

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;

            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.name != "ObjectField")
            {
                return;
            }

            Vector3 point = hit.point;

            Vector3 snappedPosition = new Vector3(
            Mathf.Round(point.x / gridSize) * gridSize,
            Mathf.Round(point.y / gridSize) * gridSize,
            Mathf.Round(point.z / gridSize) * gridSize);

            ghostObject.transform.position = snappedPosition;

            if (pushedPosition.Contains(snappedPosition))
                SetGhostColor(Color.red);
            else
                SetGhostColor(new Color(1f, 1f, 1f, 0.5f));
        }
    }

    void SetGhostColor(Color color)
    {
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.color = color;
        }
    }

    void FPlaceObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.name != "ObjectField")
                return;
        }
        else
        {
            return;
        }


        Vector3 placementPosition = ghostObject.transform.position;

        if (!pushedPosition.Contains(placementPosition))
        {
            Instantiate(placeObject, placementPosition, Quaternion.identity);
            pushedPosition.Add(placementPosition);
        }
    }
}