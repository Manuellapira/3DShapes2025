// File: Assets/Scripts/DemoRotator.cs
// Small helper: rotates the drawer and cycles shape types for demo/video.
using UnityEngine;

[RequireComponent(typeof(GLShapeDrawer))]
public class DemoRotator : MonoBehaviour
{
    public GLShapeDrawer drawer;
    public float rotateSpeed = 25f;
    public float tiltSpeed = 12f;
    public float cycleSeconds = 4f;

    private float _timer;
    private int _index;

    private void Awake()
    {
        if (drawer == null) drawer = GetComponent<GLShapeDrawer>();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, tiltSpeed * Time.deltaTime, Space.Self);

        _timer += Time.deltaTime;
        if (_timer >= cycleSeconds)
        {
            _timer = 0f;
            _index = (_index + 1) % System.Enum.GetValues(typeof(GLShapeDrawer.ShapeType)).Length;
            drawer.shape = (GLShapeDrawer.ShapeType)_index;
            // optional small randomization for visibility
            drawer.color = Color.Lerp(Color.white, Random.ColorHSV(0f,1f,0.8f,1f,0.8f,1f), 0.6f);
        }
    }
}