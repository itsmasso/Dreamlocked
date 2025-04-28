using UnityEngine;

public class MenuMusicPlayer : MonoBehaviour
{
 
    void Start()
    {
        AudioManager.Instance.PlayMenuMusic();
    }

}
