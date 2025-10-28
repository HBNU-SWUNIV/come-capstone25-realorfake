using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    
    private static StartSceneManager instance;
    public static StartSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<StartSceneManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(StartSceneManager).Name;
                    instance = obj.AddComponent<StartSceneManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
} 