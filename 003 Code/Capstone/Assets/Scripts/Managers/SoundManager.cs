using UnityEngine;

public class SoundManager : MonoBehaviour
{
    AudioSource audioSource;
    AudioClip nowClip;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(string soundName)
    {
        nowClip = Resources.Load<AudioClip>($"Sound/{soundName}");
        if (nowClip != null)
        {
            audioSource.clip = nowClip;
            audioSource.Play();
        }
    }
}
