using UnityEngine;
using UnityEngine.UI;

namespace LCVR.UI;

public class SkewedImage : Image
{
    [SerializeField] private float skewX;
    [SerializeField] private float skewY;

    public override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);

        var r = GetPixelAdjustedRect();
        var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);

        var color32 = color;
            
        vh.Clear();
        vh.AddVert(new Vector3(v.x - skewX, v.y - skewY), color32, new Vector2(0, 0));
        vh.AddVert(new Vector3(v.x + skewX, v.w - skewY), color32, new Vector2(0, 1));
        vh.AddVert(new Vector3(v.z + skewX, v.w + skewY), color32, new Vector2(1, 1));
        vh.AddVert(new Vector3(v.z - skewX, v.y + skewY), color32, new Vector2(1, 0));
            
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}