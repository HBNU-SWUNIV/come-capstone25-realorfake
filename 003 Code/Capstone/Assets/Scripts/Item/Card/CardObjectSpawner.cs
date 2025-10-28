using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityGLTF;
using UnityGLTF.Loader;

public class CardObjectSpawner : MonoBehaviour
{
    [Header("참조")]
    public XRRayInteractor rayInteractor; // XR 레이 인터랙터
    public TextAsset jsonData; // 카드 데이터 JSON 파일
    public LayerMask placementLayerMask; // 오브젝트 배치용 레이어
    public float gridSize = 1f; // 그리드 크기
    public Material ghostMaterial; // 유령 오브젝트용 머티리얼 (반투명)

    [Header("설정")]
    public float placementRayDistance = 10f; // 바닥 감지 거리
    public bool useGLBLoader = true; // GLB 로더 사용 여부

    private GameObject selectedCard; // 현재 선택된 카드
    private GameObject selectedObject;
    private Dictionary<string, GameObject> objectMap = new Dictionary<string, GameObject>(); // oid-프리팹 매핑
    private WebcamToUI glbLoader; // GLB 로더 참조
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>(); // 이미 배치된 위치
    private Dictionary<string, GameObject> tempObjects = new Dictionary<string, GameObject>(); // 임시 오브젝트 저장
    private GameObject currentGhost; // 현재 유령 오브젝트
    private string currentObjectId; // 현재 소환할 오브젝트 ID
    private bool isPlacing = false; // 배치 모드 여부
    private bool isEnabled = false;

    public PresetManager presetManager;
    private string currentSceneName;
    public CardInteractor cardInteractor; // CardInteractor 참조 추가

    private string localPath;
    private string _kitchenBasicJson = "{\r\n    \"oid\": \"0\",\r\n    \"uid\": \"0\",\r\n    \"bigClass\": \"Kitchen\",\r\n    \"smallClass\": \"basic\",\r\n    \"abilityType\": \"A\",\r\n    \"sellState\": \"N\",\r\n    \"cost\": \"0\",\r\n    \"expireCount\": \"-1\",\r\n    \"stat\": \"0\",\r\n    \"grade\": \"Normal\"\r\n  }";

    void Awake()
    {
        /*
        // 싱글톤 패턴으로 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }
        */
    }

    // 싱글톤 인스턴스
    //public static CardObjectSpawner Instance { get; private set; }

    void OnDisable()
    {
        Debug.Log("CardObjectSpawner Disabled");
    }

    void Start()
    {
        LoadObjectsFromJson();
        currentSceneName = SceneManager.GetActiveScene().name;
        localPath = Application.persistentDataPath;
        
        // GLBLoader 확인
        if (useGLBLoader)
        {
            glbLoader = FindAnyObjectByType<WebcamToUI>();
            if (glbLoader == null)
            {
                Debug.LogWarning("WebcamToUI(GLBLoader) 인스턴스를 찾을 수 없습니다. WebcamToUI 컴포넌트가 씬에 있는지 확인해주세요.");
            }
        }
        
        // PresetManager 자동 할당
        if (presetManager == null && currentSceneName != "CaptureScene")
        {
            GameObject obj = GameObject.Find("PresetManager");
            if (obj != null)
                presetManager = obj.GetComponent<PresetManager>();
            if (presetManager == null)
            {
                Debug.LogWarning("씬에서 PresetManager를 찾을 수 없습니다.");
            }
        }

        // XRRayInteractor 자동 할당
        if (rayInteractor == null)
        {
            rayInteractor = FindAnyObjectByType<XRRayInteractor>();
            if (rayInteractor == null)
            {
                Debug.LogWarning("씬에서 XRRayInteractor를 찾을 수 없습니다.");
            }
        }

        // CardInteractor 자동 할당
        //cardInteractor = FindAnyObjectByType<CardInteractor>();
        cardInteractor = this.gameObject.GetComponent<CardInteractor>();
        if (cardInteractor == null)
        {
            Debug.LogWarning("씬에서 CardInteractor를 찾을 수 없습니다.");
        }

        // 씬 전환 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 씬 전환 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        HandleCardSelection();
        HandlePlacementInput();
        
        // FightScene에서만 고스트 위치 업데이트
        if (currentSceneName == "FightScene")
        {
            UpdateGhostPosition();
        }
    }

    // 카드 선택 처리
    void HandleCardSelection()
    {
        if (SceneManager.GetActiveScene().name == "MainScene")
            return;

        if (!isEnabled)
            return;

        // CardInteractor가 zoom 상태가 아닐 때는 아무것도 하지 않음
        if (cardInteractor == null || !cardInteractor.IsZooming) //
            return;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {

            GameObject hitObject = hit.collider?.gameObject;
            if (hitObject == null) return;

            // 카드 레이어인 경우에만 처리
            if (hitObject.layer == LayerMask.NameToLayer("Card"))
            {
                GameObject cardParent = hitObject.transform.root.gameObject;
                if (selectedCard != cardParent)
                {
                    selectedCard = cardParent;

                    CardDisplay display = selectedCard.GetComponentInChildren<CardDisplay>();
                    string jsonData = display.GetJsonData();
                    string objectId = ExtractObjectIdFromJson(jsonData);

                    GameObject pObject = GameObject.Find($"ItemParent");
                    GameObject sObject = pObject.transform.Find($"Item_{objectId}").gameObject;
                    selectedObject = sObject;

                    Debug.Log($"CardObjectSpawner {GameManager.GetInstance().GetStamina(NetworkManager.Singleton.IsHost)}");
                    Debug.Log($"CardObjectSpawner {selectedObject.GetComponent<BaseItem>().GetStamina()}");

                    if (GameManager.GetInstance().GetStamina(NetworkManager.Singleton.IsHost) >= selectedObject.GetComponent<BaseItem>().GetStamina())
                    {
                        PrepareGhostObject();
                    } else
                    {
                        selectedCard = null;
                        selectedObject = null;
                    }
                }
            }
        }
    }

    // 카드 선택 시 유령 오브젝트 준비
    void PrepareGhostObject()
    {
        // MainScene에서는 유령 오브젝트를 생성하지 않음
        if (currentSceneName == "MainScene") return;

        // Destroy(currentGhost);
        currentGhost = null;

        if (selectedCard == null) return;

        CardDisplay display = selectedCard.GetComponentInChildren<CardDisplay>();
        if (display == null)
        {
            Debug.LogWarning("CardDisplay 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        string jsonData = display.GetJsonData();
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogWarning("카드의 JSON 데이터가 비어있습니다.");
            return;
        }

        string objectId = ExtractObjectIdFromJson(jsonData);
        if (string.IsNullOrEmpty(objectId))
        {
            Debug.LogWarning("objectId를 추출할 수 없습니다.");
            return;
        }

        // GLB 파일이 objectMap에 있는지 확인 (LoadObjectsFromJson에서 확인된 파일들)
        if (!objectMap.ContainsKey(objectId))
        {
            Debug.LogWarning($"objectId {objectId}에 해당하는 GLB 파일이 확인되지 않았습니다.");
            return;
        }

        // GLB 파일 경로 확인
        string objectsDir = Path.Combine(localPath, "objects");
        string glbFilePath = Path.Combine(objectsDir, $"{objectId}.glb");

        if (!File.Exists(glbFilePath))
        {
            Debug.LogWarning($"GLB 파일이 존재하지 않습니다: {glbFilePath}");
            return;
        }

        // GLB 파일을 직접 로드하여 유령 오브젝트 생성
        // StartCoroutine(CreateGhostFromGLB(glbFilePath, objectId));

        // 임시로 교체한 코드
        GameObject pObject = GameObject.Find($"ItemParent");
        GameObject selectedObject = pObject.transform.Find($"Item_{objectId}").gameObject;
        currentGhost = CreateGhost(selectedObject);
        currentObjectId = objectId;
        isPlacing = true;

    }

    /// <summary>
    /// GLB 파일을 로드하여 유령 오브젝트를 생성하는 코루틴
    /// </summary>
    /// <param name="glbFilePath">GLB 파일 경로</param>
    /// <param name="objectId">오브젝트 ID</param>
    private System.Collections.IEnumerator CreateGhostFromGLB(string glbFilePath, string objectId)
    {
        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        importer.SceneParent = new GameObject($"GhostGLB_{objectId}").transform;

        Debug.Log($"[CardObjectSpawner] 유령 오브젝트용 GLB 로딩 시작: {glbFilePath}");

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            Debug.LogError($"[CardObjectSpawner] 유령 오브젝트 GLB 로딩 실패: {loadSceneTask.Exception.Message}");
        }
        else
        {
            Debug.Log($"[CardObjectSpawner] 유령 오브젝트 GLB 로딩 완료: {objectId}");
            
            // 로드된 GLB 오브젝트를 유령 오브젝트로 변환
            // SceneParent의 첫 번째 자식이 실제 GLB 오브젝트
            GameObject loadedObject = importer.SceneParent.GetChild(0).gameObject;

            // 부모 오브젝트 제거하고 실제 GLB 오브젝트만 남기기
            loadedObject.transform.SetParent(null);
            Destroy(importer.SceneParent.gameObject);

            currentGhost = CreateGhost(loadedObject);
            currentObjectId = objectId;
            isPlacing = true;
        }
    }

    // 유령 오브젝트 생성
    GameObject CreateGhost(GameObject prefab)
    {
        GameObject ghost = prefab;
        ghost.SetActive(true);

        // 실제 게임할때는 Collider가 없는게 좋을 수도 있음
        ghost.AddComponent<BoxCollider>();
        
        foreach (Collider col in ghost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            if (ghostMaterial != null)
                renderer.material = ghostMaterial;
            else
            {
                // PBR 머티리얼을 안전하게 처리
                Material mat = renderer.material;
                ApplyTransparencyToMaterial(mat);
            }
        }
        return ghost;
    }

    /// <summary>
    /// 머티리얼에 투명도를 적용하는 메서드 (PBR 머티리얼 지원)
    /// </summary>
    /// <param name="mat">처리할 머티리얼</param>
    private void ApplyTransparencyToMaterial(Material mat)
    {
        try
        {
            // 머티리얼이 _Color 프로퍼티를 가지고 있는지 확인
            if (mat.HasProperty("_Color"))
            {
                Color color = mat.color;
                color.a = 0.5f;
                mat.color = color;
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                // PBR 머티리얼의 경우 _BaseColor 사용
                Color color = mat.GetColor("_BaseColor");
                color.a = 0.5f;
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_MainColor"))
            {
                // 다른 PBR 셰이더의 경우 _MainColor 사용
                Color color = mat.GetColor("_MainColor");
                color.a = 0.5f;
                mat.SetColor("_MainColor", color);
            }

            // 알파 블렌딩 설정
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 2); // Transparent 모드
            }
            
            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            
            if (mat.HasProperty("_DstBlend"))
            {
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            
            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetInt("_ZWrite", 0);
            }

            // 알파 관련 키워드 설정
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            
            // 렌더 큐 설정
            mat.renderQueue = 3000;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"머티리얼 투명도 적용 중 오류 발생: {e.Message}");
        }
    }

    // 유령 오브젝트 위치 갱신
    void UpdateGhostPosition()
    {
        if (!isPlacing || currentGhost == null) return;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Vector3 snappedPos = SnapToGrid(hit.point);
            currentGhost.transform.position = snappedPos;
            UpdateGhostColor(snappedPos);
        }
    }

    // 그리드 스냅
    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    // 유령 오브젝트 색상 피드백
    void UpdateGhostColor(Vector3 position)
    {
        Color ghostColor = occupiedPositions.Contains(position) ? Color.red : new Color(1, 1, 1, 0.5f);
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
        {
            SetMaterialColor(renderer.material, ghostColor);
        }
    }

    /// <summary>
    /// 머티리얼의 색상을 안전하게 설정하는 메서드 (PBR 머티리얼 지원)
    /// </summary>
    /// <param name="mat">처리할 머티리얼</param>
    /// <param name="color">설정할 색상</param>
    private void SetMaterialColor(Material mat, Color color)
    {
        try
        {
            if (mat.HasProperty("_Color"))
            {
                mat.color = color;
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_MainColor"))
            {
                mat.SetColor("_MainColor", color);
            }
            else
            {
                Debug.LogWarning($"머티리얼에 색상 프로퍼티를 찾을 수 없습니다: {mat.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"머티리얼 색상 설정 중 오류 발생: {e.Message}");
        }
    }

    // XR 입력 처리 및 실제 배치
    void HandlePlacementInput()
    {
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // B 버튼 (MainScene)
        if (currentSceneName == "MainScene" &&
            rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isBPressed) &&
            isBPressed)
        {
            if (presetManager != null)
            {
                presetManager.HandleBButton();
            }
            else
            {
                Debug.LogWarning("PresetManager가 할당되지 않았습니다.");
            }
            return;
        }

        // A 버튼 (FightScene)
        if (!isPlacing || currentGhost == null) return;

        if (currentSceneName == "FightScene" &&
            rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed) &&
            isPressed)
        {
            Vector3 placementPos = currentGhost.transform.position;
            if (occupiedPositions.Contains(placementPos)) return;

            // 스태미나 감소 및 설치
            if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, selectedObject.GetComponent<BaseItem>().GetStamina())) {
                GameManager.GetInstance().SetStaminaObject(GameManager.GetInstance().GetStamina(NetworkManager.Singleton.IsHost));
                TryPlaceObject();
            } else
            {
                // GhostObject 설치 취소
                // Destroy(currentGhost);
                currentGhost.SetActive(false);
                currentGhost = null;
                isPlacing = false;
                selectedCard = null;
                selectedObject = null;
                currentObjectId = null;
            }
            
        }

        // B 버튼 (FightScene)
        if (currentSceneName == "FightScene" && rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isBPressed2) && isBPressed2)
        {
            // GhostObject 설치 취소
            //Destroy(currentGhost);
            currentGhost.SetActive(false);
            currentGhost = null;
            isPlacing = false;
            selectedCard = null;
            currentObjectId = null;
        }
    }

    // 실제 오브젝트 배치
    void TryPlaceObject()
    {
        Vector3 placementPos = currentGhost.transform.position;
        if (occupiedPositions.Contains(placementPos)) return;

        // GLB 파일 경로 확인
        string objectsDir = Path.Combine(localPath, "objects");
        string glbFilePath = Path.Combine(objectsDir, $"{currentObjectId}.glb");

        if (!File.Exists(glbFilePath))
        {
            Debug.LogError($"배치할 GLB 파일이 존재하지 않습니다: {glbFilePath}");
            return;
        }

        // GLB 파일을 로드하여 실제 오브젝트 배치
        // StartCoroutine(PlaceGLBObject(glbFilePath, placementPos));

        // 아래는 임시로 추가한 파트
        Collider col = currentGhost.GetComponent<Collider>();
        if (col == null)
            col = currentGhost.GetComponentInChildren<Collider>();
        col.enabled = true;
        if (col != null)
        {
            // Rigidbody 설정
            Rigidbody rb = currentGhost.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = currentGhost.AddComponent<Rigidbody>();
                
            }
            rb.constraints = RigidbodyConstraints.FreezeAll;

            // bounds.min.y가 바닥(y) 위치가 되도록 이동 (일단 비활성화)
            /*
            float bottomY = col.bounds.min.y;
            float desiredY = placementPos.y;
            float offsetY = desiredY - bottomY;
            currentGhost.transform.position += Vector3.up * offsetY;
            */

            if (NetworkManager.Singleton.IsHost)
            {
                
                if (currentGhost.GetComponent<BaseItem>().GetInteractionType() == BaseItem.InteractionType.Install)
                {
                    GameManager.GetInstance().SpawnItemObjectClientRpc(currentObjectId, placementPos, currentGhost.GetComponent<BaseItem>().GetInteractionType());
                    GameManager.GetInstance().AddHostInstallItemList(currentGhost);
                    rb.isKinematic = true; // 물리 시뮬레이션 비활성화
                    occupiedPositions.Add(placementPos);
                    // 설치 파티클은 전부 자신 위치에서 소환,, 바꿀 필요 없음 + 설치아이템 상호작용은 따로 또 해야함
                    currentGhost.GetComponent<BaseItem>().PlayInstallParticle();
                } else if (currentGhost.GetComponent<BaseItem>().GetInteractionType() == BaseItem.InteractionType.Consume)
                {
                    currentGhost.SetActive(false);
                    rb.isKinematic = true;
                    // 소모 아이템은 아이템마다 다름
                    //currentGhost.GetComponent<BaseItem>().PlayUseParticle(GameManager.GetInstance().GetEnemy());
                    GameManager.GetInstance().PlayParticleClientRpc(currentGhost.GetComponent<BaseItem>().GetOid());
                } else
                {
                    GameManager.GetInstance().SpawnItemObjectClientRpc(currentObjectId, placementPos, currentGhost.GetComponent<BaseItem>().GetInteractionType());
                    // 전부 상대방 위치에서 파티클 소환 해야함
                    currentGhost.GetComponent<BaseItem>().PlayUseParticle(GameManager.GetInstance().GetEnemy());
                    GameManager.GetInstance().PlayParticleClientRpc(currentGhost.GetComponent<BaseItem>().GetOid());
                }
                GameManager.GetInstance().UseItem(currentGhost, NetworkManager.Singleton.IsHost);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                
                if (currentGhost.GetComponent<BaseItem>().GetInteractionType() == BaseItem.InteractionType.Install)
                {
                    GameManager.GetInstance().SpawnItemObjectServerRpc(currentObjectId, placementPos, currentGhost.GetComponent<BaseItem>().GetInteractionType());
                    GameManager.GetInstance().AddClientInstallItemList(currentGhost);
                    rb.isKinematic = true; // 물리 시뮬레이션 비활성화
                    occupiedPositions.Add(placementPos);
                    currentGhost.GetComponent<BaseItem>().PlayInstallParticle();
                }
                else if (currentGhost.GetComponent<BaseItem>().GetInteractionType() == BaseItem.InteractionType.Consume)
                {
                    currentGhost.SetActive(false);
                    rb.isKinematic = true;
                    //currentGhost.GetComponent<BaseItem>().PlayUseParticle(GameManager.GetInstance().GetEnemy());
                    GameManager.GetInstance().PlayParticleServerRpc(currentGhost.GetComponent<BaseItem>().GetOid());
                } else
                {
                    GameManager.GetInstance().SpawnItemObjectServerRpc(currentObjectId, placementPos, currentGhost.GetComponent<BaseItem>().GetInteractionType());
                    currentGhost.GetComponent<BaseItem>().PlayUseParticle(GameManager.GetInstance().GetEnemy());
                    GameManager.GetInstance().PlayParticleServerRpc(currentGhost.GetComponent<BaseItem>().GetOid());
                }
                GameManager.GetInstance().UseItem(currentGhost, NetworkManager.Singleton.IsHost);
            }
        }

        // 한번 사용한 카드는 비활성화
        selectedCard.SetActive(false);

        currentGhost.AddComponent<XRGrabInteractable>();
        currentGhost.name = $"Item_{currentObjectId}";
        //currentGhost.layer = LayerMask.NameToLayer("Item");
        currentGhost.tag = "Item";
        currentGhost = null;
        isPlacing = false;
        selectedCard = null;
        currentObjectId = null;
    }

    /// <summary>
    /// GLB 파일을 로드하여 실제 오브젝트를 배치하는 코루틴
    /// </summary>
    /// <param name="glbFilePath">GLB 파일 경로</param>
    /// <param name="placementPos">배치할 위치</param>
    private System.Collections.IEnumerator PlaceGLBObject(string glbFilePath, Vector3 placementPos)
    {
        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        importer.SceneParent = new GameObject($"PlacedGLB_{currentObjectId}").transform;

        Debug.Log($"[CardObjectSpawner] 실제 오브젝트용 GLB 로딩 시작: {glbFilePath}");

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            Debug.LogError($"[CardObjectSpawner] 실제 오브젝트 GLB 로딩 실패: {loadSceneTask.Exception.Message}");
        }
        else
        {
            Debug.Log($"[CardObjectSpawner] 실제 오브젝트 GLB 로딩 완료: {currentObjectId}");
            
            // 로드된 GLB 오브젝트를 배치
            // SceneParent의 첫 번째 자식이 실제 GLB 오브젝트
            GameObject placedObj = importer.SceneParent.GetChild(0).gameObject;
            
            // 부모 오브젝트 제거하고 실제 GLB 오브젝트만 남기기
            placedObj.transform.SetParent(null);
            Destroy(importer.SceneParent.gameObject);
            
            placedObj.transform.position = placementPos;

            // Collider 찾기 (자식 포함)
            Collider col = placedObj.GetComponent<Collider>();
            if (col == null)
                col = placedObj.GetComponentInChildren<Collider>();

            if (col != null)
            {
                // bounds.min.y가 바닥(y) 위치가 되도록 이동
                float bottomY = col.bounds.min.y;
                float desiredY = placementPos.y;
                float offsetY = desiredY - bottomY;
                placedObj.transform.position += Vector3.up * offsetY;
            }

            occupiedPositions.Add(placementPos);

            // Rigidbody 설정
            Rigidbody rb = placedObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // 물리 시뮬레이션 비활성화
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                // Rigidbody가 없으면 추가하고 설정
                rb = placedObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            placedObj.AddComponent<XRGrabInteractable>();

            Destroy(currentGhost);
            currentGhost = null;
            isPlacing = false;
            selectedCard = null;
            currentObjectId = null;
        }
    }

    // JSON 데이터 기반 GLB 파일 확인
    void LoadObjectsFromJson()
    {
        // localPath가 초기화되지 않았으면 초기화
        if (string.IsNullOrEmpty(localPath))
        {
            localPath = Application.persistentDataPath;
        }
        
        objectMap.Clear();
        tempObjects.Clear();
        
        if (jsonData == null)
        {
            Debug.LogWarning("jsonData가 null입니다. LoadObjectsFromJson을 건너뜁니다.");
            return;
        }
        
        JSONNode cardDataArray = JSON.Parse(jsonData.text);
        cardDataArray.Add(JSON.Parse(_kitchenBasicJson));
        foreach (JSONNode cardData in cardDataArray)
        {
            string objectId = cardData["oid"].Value.Trim();
            
            // GLB 파일 존재 여부만 확인
            string objectsDir = Path.Combine(localPath, "objects");
            string glbFilePath = Path.Combine(objectsDir, $"{objectId}.glb");
            
            if (File.Exists(glbFilePath))
            {
                Debug.Log($"GLB 파일 확인됨: {objectId}");
                // GLB 파일이 존재하면 objectMap에 추가 (실제 로딩은 나중에)
                objectMap[objectId] = null; // 실제 오브젝트는 나중에 로드
            }
            else
            {
                Debug.LogWarning($"GLB 파일이 존재하지 않습니다: {glbFilePath}");
            }
        }
    }

    // 카드 JSON에서 objectId 추출
    private string ExtractObjectIdFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("JSON 데이터가 비어있습니다.");
            return null;
        }

        try
        {
            var parsed = JSON.Parse(json);
            if (parsed == null || !parsed.HasKey("oid"))
            {
                Debug.LogWarning("JSON 데이터에 'oid' 키가 없습니다.");
                return null;
            }
            return parsed["oid"].Value;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 파싱 중 오류 발생: {e.Message}");
            return null;
        }
    }

    void OnEnable()
    {
        // localPath가 초기화된 후에만 LoadObjectsFromJson 호출
        if (!string.IsNullOrEmpty(localPath))
        {
            LoadObjectsFromJson();
        }
    }

    public void SetCardInteraction(bool enable)
    {
        Debug.Log($"SetCardInteraction {enable}");
        isEnabled = enable;

        // 카드 상호작용이 비활성화될 때 현재 상태 초기화
        if (!enable)
        {
            isPlacing = false;
            selectedCard = null;
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
            currentObjectId = null;
        }
    }

    public void DeleteOccupiedPosition(Vector3 pos)
    {
        occupiedPositions.Remove(pos);
    }

    /// <summary>
    /// GLB 로딩이 완료된 후 임시 오브젝트를 실제 GLB 오브젝트로 교체
    /// </summary>
    /// <param name="objectId">교체할 오브젝트 ID</param>
    /// <param name="loadedGLBObject">로드된 GLB 오브젝트</param>
    public GameObject ReplaceTempObjectWithGLB(string objectId, GameObject loadedGLBObject)
    {
        if (tempObjects.ContainsKey(objectId))
        {
            GameObject tempObject = tempObjects[objectId];
            
            // 임시 오브젝트의 위치와 회전을 유지
            Vector3 position = tempObject.transform.position;
            Quaternion rotation = tempObject.transform.rotation;
            
            // 임시 오브젝트 제거
            Destroy(tempObject);
            tempObjects.Remove(objectId);
            
            // 실제 GLB 오브젝트로 교체
            if (loadedGLBObject != null)
            {
                loadedGLBObject.transform.position = position;
                loadedGLBObject.transform.rotation = rotation;
                objectMap[objectId] = loadedGLBObject;
                Debug.Log($"임시 오브젝트를 GLB 오브젝트로 교체 완료: {objectId}");
                return loadedGLBObject;
            }
        }

        return null;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        LoadObjectsFromJson();
        
        // XRRayInteractor 재할당
        if (rayInteractor == null)
        {
            rayInteractor = FindObjectOfType<XRRayInteractor>();
        }

        // PresetManager 재할당
        if (presetManager == null && currentSceneName != "CaptureScene")
        {
            GameObject obj = GameObject.Find("PresetManager");
            if (obj != null)
                presetManager = obj.GetComponent<PresetManager>();
            if (presetManager == null)
            {
                Debug.LogWarning($"씬 {scene.name}에서 PresetManager를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log($"씬 {scene.name}에서 PresetManager를 찾았습니다.");
            }
        }

        // CardInteractor 재할당
        if (cardInteractor == null)
        {
            cardInteractor = FindObjectOfType<CardInteractor>();
            if (cardInteractor == null)
            {
                Debug.LogWarning($"씬 {scene.name}에서 CardInteractor를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log($"씬 {scene.name}에서 CardInteractor를 찾았습니다.");
            }
        }
    }

    /// <summary>
    /// GLB 파일을 로드하는 메서드
    /// </summary>
    /// <param name="objectId">로드할 오브젝트의 ID</param>
    public void GLBLoader(string objectId)
    {
        // objects 폴더 경로
        string objectsDir = Path.Combine(localPath, "objects");
        string glbFilePath = Path.Combine(objectsDir, $"{objectId}.glb");

        Debug.Log($"[CardObjectSpawner] 로드할 .glb 파일: {glbFilePath}");

        if (!Directory.Exists(objectsDir))
        {
            Debug.LogError("[CardObjectSpawner] objects 디렉토리를 찾을 수 없습니다: " + objectsDir);
            return;
        }

        if (!File.Exists(glbFilePath))
        {
            Debug.LogError("[CardObjectSpawner] GLB 파일이 존재하지 않습니다: " + glbFilePath);
            return;
        }

        // GLB 로딩은 코루틴으로 비동기 실행
        StartCoroutine(LoadGLBModelCoroutine(glbFilePath, objectId));
    }

    /// <summary>
    /// GLB 모델을 비동기로 로드하는 코루틴
    /// </summary>
    /// <param name="glbFilePath">GLB 파일 경로</param>
    /// <param name="objectId">오브젝트 ID</param>
    private System.Collections.IEnumerator LoadGLBModelCoroutine(string glbFilePath, string objectId)
    {
        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        importer.SceneParent = new GameObject($"LoadedGLB_{objectId}").transform;

        Debug.Log($"[CardObjectSpawner] GLB 로딩 시작: {glbFilePath}");

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            Debug.LogError($"[CardObjectSpawner] GLB 로딩 실패: {loadSceneTask.Exception.Message}");
        }
        else
        {
            Debug.Log($"[CardObjectSpawner] GLB 로딩 완료: {objectId}");
            
            // 로딩된 GLB 오브젝트를 임시 오브젝트와 교체
            // SceneParent의 첫 번째 자식이 실제 GLB 오브젝트
            GameObject loadedObject = importer.SceneParent.GetChild(0).gameObject;
            
            // 부모 오브젝트 제거하고 실제 GLB 오브젝트만 남기기
            loadedObject.transform.SetParent(null);
            Destroy(importer.SceneParent.gameObject);
            
            ReplaceTempObjectWithGLB(objectId, loadedObject);
        }
    }
}