using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GLBResultUI : MonoBehaviour
{
    private GameObject loadedObject;
    
    public void SetLoadedObject(GameObject obj)
    {
        loadedObject = obj;
    }
    
    public GameObject GetLoadedObject()
    {
        return loadedObject;
    }
} 