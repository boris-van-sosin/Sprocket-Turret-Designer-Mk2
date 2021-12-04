using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class Trihedron : MonoBehaviour
{
    public Collider XTool;
    public Collider YTool;
    public Collider ZTool;

    public void ReleaseTrihedron()
    {
        gameObject.SetActive(false);
    }

    public Axis GetAxis(Collider c)
    {
        if (c == XTool)
            return Axis.X;
        if (c == YTool)
            return Axis.Y;
        if (c == ZTool)
            return Axis.Z;
        return Axis.None;
    }

    static public Trihedron FromCollider(Collider c)
    {
        return c.GetComponentInParent<Trihedron>();
    }
}
