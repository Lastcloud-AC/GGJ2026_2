using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string id;
        public AudioClip clip;
        public float volume = 1f;
        public float pitch = 1f;
        public bool loop = false;
    }

    public Sound[] sounds;
    public bool dontDestroyOnLoad = true;

    private static AudioManager instance;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    public static AudioManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlaySfx(string id)
    {
        Sound sound = FindSound(id);
        if (sound == null || sound.clip == null)
        {
            return;
        }

        sfxSource.pitch = sound.pitch;
        sfxSource.PlayOneShot(sound.clip, Mathf.Clamp01(sound.volume));
    }

    public void PlayMusic(string id)
    {
        Sound sound = FindSound(id);
        if (sound == null || sound.clip == null)
        {
            return;
        }

        musicSource.clip = sound.clip;
        musicSource.volume = Mathf.Clamp01(sound.volume);
        musicSource.pitch = sound.pitch;
        musicSource.loop = sound.loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private Sound FindSound(string id)
    {
        if (string.IsNullOrEmpty(id) || sounds == null)
        {
            return null;
        }

        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i] != null && sounds[i].id == id)
            {
                return sounds[i];
            }
        }

        return null;
    }
}
