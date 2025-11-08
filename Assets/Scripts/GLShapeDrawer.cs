// File: Assets/Scripts/GLShapeDrawer.cs
// Draws wireframe primitives using GL; builds line segment list in local space and draws using object transform.
// Keep comments brief — explain why certain lines are necessary.
using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class GLShapeDrawer : MonoBehaviour
{
    public enum ShapeType { Pyramid, Cylinder, RectColumn, Sphere, Capsule }

    [Header("Shape")]
    public ShapeType shape = ShapeType.Pyramid;
    public Material material;            // assign Unlit/Color material in inspector
    public Color color = Color.white;

    [Header("Size")]
    public float radius = 1f;
    public float height = 2f;
    public float width = 1f;             // for RectColumn
    public float depth = 1f;             // for RectColumn

    [Header("Quality")]
    [Range(6, 128)] public int segments = 20;  // clamped; sphere/cylinder > 5

    [Header("Render")]
    public float lineThickness = 1f;     // GL line width not reliable cross-platform

    // internal edge list of (a,b) pairs in local space
    private readonly List<(Vector3 a, Vector3 b)> _edges = new List<(Vector3, Vector3)>();
    private ShapeType _lastShape;
    private int _lastSegments;
    private float _lastRadius, _lastHeight, _lastWidth, _lastDepth;
    private Color _lastColor;

    private void OnValidate()
    {
        segments = Mathf.Max(6, segments);
        radius = Mathf.Max(0.0001f, radius);
        height = Mathf.Max(0.0001f, height);
        width = Mathf.Max(0.0001f, width);
        depth = Mathf.Max(0.0001f, depth);
        RebuildIfNeeded();
    }

    private void Start()
    {
        RebuildIfNeeded();
    }

    private void Update()
    {
        // rebuild in editor/play if parameters changed
        RebuildIfNeeded();
    }

    // Rebuild edges when parameters change
    private void RebuildIfNeeded()
    {
        if (shape == _lastShape && segments == _lastSegments &&
            Mathf.Approximately(radius, _lastRadius) &&
            Mathf.Approximately(height, _lastHeight) &&
            Mathf.Approximately(width, _lastWidth) &&
            Mathf.Approximately(depth, _lastDepth) &&
            color == _lastColor) return;

        _lastShape = shape;
        _lastSegments = segments;
        _lastRadius = radius;
        _lastHeight = height;
        _lastWidth = width;
        _lastDepth = depth;
        _lastColor = color;

        _edges.Clear();
        switch (shape)
        {
            case ShapeType.Pyramid: _edges.AddRange(GeneratePyramid(radius, height)); break;
            case ShapeType.Cylinder: _edges.AddRange(GenerateCylinder(radius, height, segments)); break;
            case ShapeType.RectColumn: _edges.AddRange(GenerateRectColumn(width, height, depth)); break;
            case ShapeType.Sphere: _edges.AddRange(GenerateSphere(radius, segments)); break;
            case ShapeType.Capsule: _edges.AddRange(GenerateCapsule(radius, height, segments)); break;
        }
    }

    private void OnRenderObject()
    {
        if (material == null || _edges.Count == 0) return;

        // Set material and draw with model matrix = this transform so camera projection handles perspective
        material.SetPass(0); // required
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(color);

        foreach (var e in _edges)
        {
            GL.Vertex(e.a);
            GL.Vertex(e.b);
        }

        GL.End();
        GL.PopMatrix();
    }

    // ---------- Generators (produce local-space line pairs) ----------

    private IEnumerable<(Vector3, Vector3)> GeneratePyramid(float halfBase, float height)
    {
        // base on y = -height*0.0 (we put base centered around y = -height*0.5 so pyramid sits centered vertically)
        float half = halfBase;
        Vector3 bl = new Vector3(-half, -height * 0.5f, -half);
        Vector3 br = new Vector3(half, -height * 0.5f, -half);
        Vector3 tr = new Vector3(half, -height * 0.5f, half);
        Vector3 tl = new Vector3(-half, -height * 0.5f, half);
        Vector3 apex = new Vector3(0f, height * 0.5f, 0f);

        // base edges
        yield return (bl, br);
        yield return (br, tr);
        yield return (tr, tl);
        yield return (tl, bl);

        // sides
        yield return (bl, apex);
        yield return (br, apex);
        yield return (tr, apex);
        yield return (tl, apex);
    }

    private IEnumerable<(Vector3, Vector3)> GenerateCylinder(float radius, float height, int seg)
    {
        float halfH = height * 0.5f;
        // rings points
        Vector3[] top = new Vector3[seg];
        Vector3[] bot = new Vector3[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            top[i] = new Vector3(x, halfH, z);
            bot[i] = new Vector3(x, -halfH, z);
        }
        for (int i = 0; i < seg; i++)
        {
            int n = (i + 1) % seg;
            // top ring
            yield return (top[i], top[n]);
            // bottom ring
            yield return (bot[i], bot[n]);
            // vertical connector
            yield return (top[i], bot[i]);
        }
    }

    private IEnumerable<(Vector3, Vector3)> GenerateRectColumn(float width, float height, float depth)
    {
        float hx = width * 0.5f;
        float hy = height * 0.5f;
        float hz = depth * 0.5f;

        Vector3[] c = new Vector3[8];
        c[0] = new Vector3(-hx, -hy, -hz);
        c[1] = new Vector3(hx, -hy, -hz);
        c[2] = new Vector3(hx, -hy, hz);
        c[3] = new Vector3(-hx, -hy, hz);

        c[4] = new Vector3(-hx, hy, -hz);
        c[5] = new Vector3(hx, hy, -hz);
        c[6] = new Vector3(hx, hy, hz);
        c[7] = new Vector3(-hx, hy, hz);

        // bottom
        yield return (c[0], c[1]);
        yield return (c[1], c[2]);
        yield return (c[2], c[3]);
        yield return (c[3], c[0]);
        // top
        yield return (c[4], c[5]);
        yield return (c[5], c[6]);
        yield return (c[6], c[7]);
        yield return (c[7], c[4]);
        // verticals
        yield return (c[0], c[4]);
        yield return (c[1], c[5]);
        yield return (c[2], c[6]);
        yield return (c[3], c[7]);
    }

    private IEnumerable<(Vector3, Vector3)> GenerateSphere(float radius, int seg)
    {
        // latitudes (horizontal rings) and longitudes (vertical lines)
        int latSeg = Mathf.Max(6, seg / 2);
        int lonSeg = seg;

        // latitude rings
        for (int lat = 0; lat <= latSeg; lat++)
        {
            float v = lat / (float)latSeg; // 0..1
            float phi = (v - 0.5f) * Mathf.PI; // -pi/2..pi/2
            float y = Mathf.Sin(phi) * radius;
            float r = Mathf.Cos(phi) * radius;
            // ring segments
            for (int lon = 0; lon < lonSeg; lon++)
            {
                int n = (lon + 1) % lonSeg;
                float a0 = (lon / (float)lonSeg) * Mathf.PI * 2f;
                float a1 = (n / (float)lonSeg) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * r, y, Mathf.Sin(a0) * r);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * r, y, Mathf.Sin(a1) * r);
                yield return (p0, p1);
            }
        }

        // longitudes
        for (int lon = 0; lon < lonSeg; lon++)
        {
            float a = (lon / (float)lonSeg) * Mathf.PI * 2f;
            for (int lat = 0; lat < latSeg; lat++)
            {
                float t0 = lat / (float)latSeg;
                float t1 = (lat + 1) / (float)latSeg;
                float phi0 = (t0 - 0.5f) * Mathf.PI;
                float phi1 = (t1 - 0.5f) * Mathf.PI;
                Vector3 p0 = new Vector3(Mathf.Cos(a) * Mathf.Cos(phi0) * radius, Mathf.Sin(phi0) * radius, Mathf.Sin(a) * Mathf.Cos(phi0) * radius);
                Vector3 p1 = new Vector3(Mathf.Cos(a) * Mathf.Cos(phi1) * radius, Mathf.Sin(phi1) * radius, Mathf.Sin(a) * Mathf.Cos(phi1) * radius);
                yield return (p0, p1);
            }
        }
    }

    private IEnumerable<(Vector3, Vector3)> GenerateCapsule(float radius, float height, int seg)
    {
        // central cylinder height is height - 2*radius (clamped >=0)
        float core = Mathf.Max(0f, height - 2f * radius);
        float halfCore = core * 0.5f;
        int latSeg = Mathf.Max(4, seg / 4);

        // cylinder
        foreach (var e in GenerateCylinder(radius, core, seg)) yield return e;

        // hemisphere top (center at +halfCore)
        for (int lat = 0; lat <= latSeg; lat++)
        {
            float t = lat / (float)latSeg;
            float phi = (t) * (Mathf.PI / 2f); // 0..pi/2
            float y = Mathf.Sin(phi) * radius + halfCore;
            float r = Mathf.Cos(phi) * radius;
            for (int i = 0; i < seg; i++)
            {
                int n = (i + 1) % seg;
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = (n / (float)seg) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * r, y, Mathf.Sin(a0) * r);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * r, y, Mathf.Sin(a1) * r);
                yield return (p0, p1);
            }
        }

        // hemisphere bottom (mirror)
        for (int lat = 0; lat <= latSeg; lat++)
        {
            float t = lat / (float)latSeg;
            float phi = (t) * (Mathf.PI / 2f);
            float y = -Mathf.Sin(phi) * radius - halfCore;
            float r = Mathf.Cos(phi) * radius;
            for (int i = 0; i < seg; i++)
            {
                int n = (i + 1) % seg;
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = (n / (float)seg) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * r, y, Mathf.Sin(a0) * r);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * r, y, Mathf.Sin(a1) * r);
                yield return (p0, p1);
            }
        }
    }
}
