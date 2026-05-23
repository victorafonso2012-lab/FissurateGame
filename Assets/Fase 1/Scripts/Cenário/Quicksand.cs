using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Quicksand : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMove player = other.GetComponent<PlayerMove>();
        if (player != null)
        {
            player.EnterQuicksand();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMove player = other.GetComponent<PlayerMove>();
        if (player != null)
        {
            player.ExitQuicksand();
        }
    }
}