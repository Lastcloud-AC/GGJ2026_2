using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Clips (12)")]
    public AudioClip[] sfxClips; // 长度 = 12
    public string[] sfxIds;      // 对应的 id，长度也 = 12

    private Dictionary<string, AudioClip> sfxDict;
    private AudioSource audioSource;

    private void Awake()
    {
        // 单例
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();

        // 建表
        sfxDict = new Dictionary<string, AudioClip>();
        for (int i = 0; i < sfxClips.Length; i++)
        {
            if (!sfxDict.ContainsKey(sfxIds[i]))
            {
                sfxDict.Add(sfxIds[i], sfxClips[i]);
            }
        }
    }

    public void PlaySfx(string id)
    {
        if (sfxDict.TryGetValue(id, out AudioClip clip))
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX id not found: {id}");
        }
    }
}
