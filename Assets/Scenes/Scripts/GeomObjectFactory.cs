using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeomObjectFactory
{
    public static GeometryManager GetGeometryManager()
    {
        return _prototypes.GetGeometryManager();
    }

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

    public static BezierCurveGeom CreateBezierCurve()
    {
        return _prototypes.CreateBezierCurve();
    }

    public static BSplineCurveGeom CreateBSplineCurve()
    {
        return _prototypes.CreateBSplineCurve();
    }

    public static CircularArcGeom CreateCircularArc()
    {
        return _prototypes.CreateCircularArc();
    }

    public static CapsuleCollider CreateCurveSegCollider(float radius, CurveGeomBase container)
    {
        CapsuleCollider coll = _prototypes.CreateCurveSegCollider();
        coll.radius = radius;
        coll.transform.SetParent(container.transform);
        return coll;
    }

    public static PlaneLayer CreateLayer(float elevation)
    {
        return _prototypes.CreateLayer(elevation);
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

    public static CurveActions GetCurveActionPanel()
    {
        return _prototypes.GetCurveActionPanel();
    }

    public static CtlPtEditPanel GetCtlPtEditPanel()
    {
        return _prototypes.GetCtlPtEditPanel();
    }

    public static LayerActions GetLayerActionPanel()
    {
        return _prototypes.GetLayerActionPanel();
    }

    public static CameraControl GetCameraControl()
    {
        return _prototypes.GetCameraControl();
    }

    public static UploadFileReceiver GetFileReceiver()
    {
        return _prototypes.GetFileReceiver();
    }

    public static Transform GetPreviewObject()
    {
        return _prototypes.GetPreviewObject();
    }

    public static Material GetCurveMtlDefault()
    {
        return _prototypes.GetCurveMtlDefault();
    }

    public static Material GetCurveMtlSelected()
    {
        return _prototypes.GetCurveMtlSelected();
    }

    public static Material GetCtlPtMtlDefault()
    {
        return _prototypes.GetCtlPtMtlDefault();
    }

    public static Material GetCtlPtMtlEditing()
    {
        return _prototypes.GetCtlPtMtlEditing();
    }

    private static readonly ObjectPrototypes _prototypes = GameObject.FindObjectOfType<ObjectPrototypes>();
}
