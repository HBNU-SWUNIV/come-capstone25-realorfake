using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[InitializeOnLoad]
public class GLBViewerUIEditor
{
    static GLBViewerUIEditor()
    {
        // 에디터가 시작될 때 UI3DModel 레이어가 있는지 확인하고 없으면 생성
        CreateUI3DModelLayerIfNeeded();
    }
    
    [MenuItem("Tools/Create UI3DModel Layer")]
    public static void CreateUI3DModelLayerIfNeeded()
    {
        // Tags and Layers 설정 파일 경로
        string tagsAndLayersPath = "ProjectSettings/TagManager.asset";

        
        // TagManager 에셋 로드
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(tagsAndLayersPath)[0]);
        
        // layers 배열 찾기
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        // UI3DModel 레이어가 이미 있는지 확인
        bool layerExists = false;
        for (int i = 8; i < layers.arraySize; i++) // 8번부터 사용자 레이어 시작
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == "UI3DModel")
            {
                layerExists = true;
                Debug.Log($"UI3DModel 레이어가 이미 존재합니다. (인덱스: {i})");
                break;
            }
        }
        
        // 레이어가 없으면 생성
        if (!layerExists)
        {
            // 빈 슬롯 찾기
            int emptySlot = -1;
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    emptySlot = i;
                    break;
                }
            }
            
            if (emptySlot != -1)
            {
                // 빈 슬롯에 UI3DModel 레이어 추가
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(emptySlot);
                layerSP.stringValue = "UI3DModel";
                tagManager.ApplyModifiedProperties();
                Debug.Log($"UI3DModel 레이어가 생성되었습니다. (인덱스: {emptySlot})");
            }
            else
            {
                Debug.LogWarning("사용 가능한 레이어 슬롯이 없습니다. 수동으로 레이어를 추가해주세요.");
            }
        }
    }
}
#endif