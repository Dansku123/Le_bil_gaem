using UnityEngine;

public class Racer : MonoBehaviour
{

    public float speed = 50f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float turnSpeed = 200f;
    void Start()
    {
        Debug.Log("Racer script has started.");
    }

    // Update is called once per frame
    void Update()
    {
        float move = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;

        transform.Translate(Vector3.forward * move);
        transform.Rotate(Vector3.up * turn);

    }
}
