using UnityEngine;

/// <summary>Rotates an object continuously (coins, gems, gears, saw blades).</summary>
public class Spinner : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 90f;

    void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
    }
}
