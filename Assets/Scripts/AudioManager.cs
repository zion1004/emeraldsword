using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;

    public AudioClip intro;
    public AudioClip background;

    private void Start()
    {
        StartCoroutine(PlayMusic());

    }
    private IEnumerator PlayMusic()
    {
        musicSource.clip = intro;
        musicSource.Play();
        while(musicSource.isPlaying){ 
            yield return null;
        }

        musicSource.loop = true;
        musicSource.clip = background;
        musicSource.Play();
    }

}
