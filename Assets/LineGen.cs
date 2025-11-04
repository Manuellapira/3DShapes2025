using UnityEngine;

public class LineGen : MonoBehaviour
{
    public Material material;
    public float lineLength = 5;

    private void OnPostRender()
    {
        DrawLine();
    }

    public void DrawLine()
    {
        if (material == null)
        {
            Debug.LogError("You need to add a material");
            return;
        }
        GL.PushMatrix();

        GL.Begin(GL.LINES);
        material.SetPass(0);


        GL.Vertex3(-lineLength, 0, 0);
        GL.Vertex3(lineLength, 0, 0);

        GL.End();
        GL.PopMatrix();
    }
}
