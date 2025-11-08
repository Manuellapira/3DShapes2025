using UnityEngine;

public class PerspectiveCamera : MonoBehaviour
{
    public static PerspectiveCamera Instance;
    public float focalLenth = 5f;
    public Vector2 vanishingPoint = Vector2.zero;

    void Awake() { Instance = this; }

    public float GetPerspective(float z)
    {
        return focalLenth / (focalLenth + z);
    }
}
