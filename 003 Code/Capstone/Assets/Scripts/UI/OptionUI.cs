using UnityEngine;
using UnityEngine.UI;
using TMPro; // Adding TMPro just in case, although mainly using UnityEngine.UI
using UnityEngine.Audio; // Add this namespace for AudioMixer
using System.Collections.Generic; // Add for List
// using UnityEngine.XR; // XR Input System 네임스페이스 제거

public class OptionUI : MonoBehaviour
{
    public Slider musicSlider; // 음악 볼륨 조절 슬라이더
    public Slider volumeSlider; // 전체 볼륨 조절 슬라이더
    private AudioMixer audioMixer; // 게임 내 오디오 믹서

    // AudioMixer에 노출(Expose)시킨 볼륨 파라미터 이름
    public string musicVolumeParameter = "MusicVolume"; 
    public string masterVolumeParameter = "MasterVolume";

    // 그래픽 품질 설정을 위한 드롭다운
    public TMP_Dropdown qualityDropdown; // Using TMP_Dropdown for TextMeshPro dropdown

    // [Header("VR Settings")] // VR Settings 헤더 제거
    // public float rayDistance = 10f; // 레이캐스트 거리 제거
    // public LayerMask uiLayerMask; // UI 레이어 마스크 제거
    public Transform xrOrigin; // XR Origin 트랜스폼 (Audio Mixer 가져오기 위해 유지)

    // private Slider currentSlider; // 제거
    // private Vector3 lastControllerPosition; // 제거
    // private bool isGrabbing = false; // 제거
    // private UnityEngine.XR.InputDevice rightController; // 제거
    // private UnityEngine.XR.InputDevice leftController; // 제거

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // XR Origin에서 오디오 믹서 가져오기
        if (xrOrigin != null)
        {
            var audioSource = xrOrigin.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.outputAudioMixerGroup != null)
            {
                audioMixer = audioSource.outputAudioMixerGroup.audioMixer;
            }
        }

        // 게임 시작 시 저장된 볼륨 값을 불러와 슬라이더에 적용
        if (audioMixer != null)
        {
            float musicVol;
            if (audioMixer.GetFloat(musicVolumeParameter, out musicVol))
            {
                if (musicSlider != null) musicSlider.value = Mathf.Pow(10, musicVol / 20);
            }

            float masterVol;
            if (audioMixer.GetFloat(masterVolumeParameter, out masterVol))
            {
                if (volumeSlider != null) volumeSlider.value = Mathf.Pow(10, masterVol / 20);
            }
        }

        // 슬라이더 값이 변경될 때 호출될 메서드 연결
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // 그래픽 품질 설정 드롭다운 초기화
        if (qualityDropdown != null)
        {
            // 품질 수준 목록 가져오기
            List<string> qualityLevels = new List<string>(QualitySettings.names);
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(qualityLevels);

            // 현재 품질 수준으로 드롭다운 값 설정
            qualityDropdown.value = QualitySettings.GetQualityLevel();

            // 드롭다운 값이 변경될 때 호출될 메서드 연결
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        // VR 컨트롤러 초기화 로직 제거
        // InitializeControllers();
    }

    // InitializeControllers 메서드 제거
    // private void InitializeControllers() { ... }

    // Update is called once per frame
    void Update()
    {
        // VR 컨트롤러 입력 처리 로직 제거 (Ray Interactor가 담당)
        // HandleVRInput();
    }

    // HandleVRInput 메서드 제거
    // private void HandleVRInput() { ... }

    public void InitializeUI()
    {
        InitializeSliders();
        InitializeQualityDropdown();
    }

    public void InitializeSliders()
    {
        if (audioMixer != null)
        {
            float musicVol;
            if (audioMixer.GetFloat(musicVolumeParameter, out musicVol))
                if (musicSlider != null) musicSlider.value = Mathf.Pow(10, musicVol / 20);

            float masterVol;
            if (audioMixer.GetFloat(masterVolumeParameter, out masterVol))
                if (volumeSlider != null) volumeSlider.value = Mathf.Pow(10, masterVol / 20);
        }

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    public void InitializeQualityDropdown()
    {
        if (qualityDropdown != null)
        {
            List<string> qualityLevels = new List<string>(QualitySettings.names);
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(qualityLevels);
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }
    }

    public void SetMusicVolume(float value)
    {
        if (audioMixer != null)
        {
            float volume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(musicVolumeParameter, volume);
        }
    }

    public void SetMasterVolume(float value)
    {
        if (audioMixer != null)
        {
            float volume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(masterVolumeParameter, volume);
        }
    }

    public float GetMusicVolume()
    {
        if (audioMixer != null)
        {
            float musicVol;
            if (audioMixer.GetFloat(musicVolumeParameter, out musicVol))
                return Mathf.Pow(10, musicVol / 20);
        }
        return 1f;
    }

    public float GetMasterVolume()
    {
        if (audioMixer != null)
        {
            float masterVol;
            if (audioMixer.GetFloat(masterVolumeParameter, out masterVol))
                return Mathf.Pow(10, masterVol / 20);
        }
        return 1f;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public int GetQuality()
    {
        return QualitySettings.GetQualityLevel();
    }


    public void OnMusicVolumeChanged(float value)
    {
        SetMusicVolume(value);
        Debug.Log("Music Volume Changed to: " + value);
    }

    public void OnVolumeChanged(float value)
    {
        SetMasterVolume(value);
        Debug.Log("Master Volume Changed to: " + value);
    }

    public void OnQualityChanged(int qualityIndex)
    {
        SetQuality(qualityIndex);
        Debug.Log("Quality changed to: " + QualitySettings.names[qualityIndex]);
    }
}
