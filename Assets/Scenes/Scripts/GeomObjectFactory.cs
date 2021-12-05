using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeomObjectFactory
{
    public static Transform CreateCtlPt(Vector3 pos)
    {
        return _prototypes.CreateCtlPt(pos);
    }

    public static LineRenderer CreatePolyline(Vector3 pos)
    {
        return _prototypes.CreatePolyline(pos);
    }

    public static LineRenderer AddLineRendererToObject(GameObject obj)
    {
        return _prototypes.AddLineRendererToObject(obj);
    }

    public static Trihedron GetMoveTridehron(Vector3 pos)
    {
        Trihedron triheron = _prototypes.GetMoveTridehron();
        triheron.transform.position = pos;
        triheron.gameObject.SetActive(true);
        return triheron;
    }

    //public static Trihedron GetRotateTridehron(Vector3 pos)
    //{
    //    Trihedron triheron = _prototypes.GetRotateTridehron();
    //    triheron.transform.position = pos;
    //    triheron.gameObject.SetActive(true);
    //    return triheron;
    //}

    public static HelpPanel GetHelpPanel()
    {
        return _prototypes.GetHelpPanel();
    }

    public static CameraControl GetCameraControl()
    {
        return _prototypes.GetCameraControl();
    }

    private static readonly ObjectPrototypes _prototypes = GameObject.FindObjectOfType<ObjectPrototypes>();
}
