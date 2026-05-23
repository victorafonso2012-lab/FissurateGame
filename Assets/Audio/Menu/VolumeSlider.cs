using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    public Slider volumeSlider;

    private void Start()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("volume", 1f);
        volumeSlider.onValueChanged.AddListener(OnVolumeChange);
        AudioManager.Instance.SetVolume(volumeSlider.value);
    }

    private void OnVolumeChange(float value)
    {
        AudioManager.Instance.SetVolume(value);
        PlayerPrefs.SetFloat("volume", value); // salva o volume
    }
}
