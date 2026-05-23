using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;          // Velocidade de movimento
    public float distance = 3f;       // Distância total de ida/volta

    private Vector2 startPos;

    void Start()
    {
        startPos = transform.position; // Posição inicial
    }

    void Update()
    {
        float move = Mathf.PingPong(Time.time * speed, distance) - (distance / 2);
        transform.position = startPos + new Vector2(move, 0);
    }
}
