using UnityEngine;

public class SimpleFaceCamera : MonoBehaviour
{
    void Update()
    {
        // Face the main camera
        transform.LookAt(Camera.main.transform);
        
        // Flip it around so the front faces the camera
        transform.Rotate(0, 180, 0);
    }
}