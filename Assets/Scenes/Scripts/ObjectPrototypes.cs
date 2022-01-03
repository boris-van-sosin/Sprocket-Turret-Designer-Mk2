using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPrototypes : MonoBehaviour
{
    public GeometryManager GeomManager;
    public Transform CtlPtPrototype;
    public LineRenderer CtlMeshPolylinePrototype;
    public PlaneLayer LayerPrototype;
    public BezierCurveGeom BezierPrototype;
    public BSplineCurveGeom BSplinePrototype;
    public CircularArcGeom CircularArcPrototype;
    public CapsuleCollider CurveSegColliderPrototype;
    public Trihedron MoveTrihedton;
    //public Trihedron RotateTrihedton;
    public CurveActions CurveActionPanel;
    public CtlPtEditPanel ControlPointEditPanel;
    public LayerActions LayerActionPanel;
    public HelpPanel HelpTextPanel;
    public CameraControl CamControl;
    public UploadFileReceiver FileReceiver;
    public Transform PreviewObject;
    public Material CurveMtlDefault;
    public Material CurveMtlSelected;
    public Material CtlPtMtlDefault;
    public Material CtlPtMtlEditing;

    public GeometryManager GetGeometryManager()
    {
        return GeomManager;
    }

    public Transform CreateCtlPt(Vector3 pos)
    {
        Transform pt = Instantiate(CtlPtPrototype, pos, CtlPtPrototype.rotation);
        return pt;
    }

    public LineRenderer CreatePolyline(Vector3 pos)
    {
        LineRenderer pl = Instantiate(CtlMeshPolylinePrototype, pos, CtlMeshPolylinePrototype.transform.rotation);
        return pl;
    }

    public LineRenderer AddLineRendererToObject(GameObject obj)
    {
        LineRenderer res = CreatePolyline(obj.transform.position);
        res.transform.SetParent(obj.transform);
        res.sharedMaterial = CtlMeshPolylinePrototype.sharedMaterial;
        res.widthCurve = CtlMeshPolylinePrototype.widthCurve;
        res.alignment = LineAlignment.View;
        return res;
    }

    public BezierCurveGeom CreateBezierCurve()
    {
        return Instantiate(BezierPrototype);
    }

    public BSplineCurveGeom CreateBSplineCurve()
    {
        return Instantiate(BSplinePrototype);
    }

    public CircularArcGeom CreateCircularArc()
    {
        return Instantiate(CircularArcPrototype);
    }

    public PlaneLayer CreateLayer(float elevation)
    {
        return Instantiate(LayerPrototype, new Vector3(0f, elevation, 0f), Quaternion.identity);
    }

    public CapsuleCollider CreateCurveSegCollider()
    {
        return Instantiate(CurveSegColliderPrototype);
    }

    public Trihedron GetMoveTridehron()
    {
        return MoveTrihedton;
    }

    //public Trihedron GetRotateTridehron()
    //{
    //    return RotateTrihedton;
    //}

    public CurveActions GetCurveActionPanel()
    {
        return CurveActionPanel;
    }

    public CtlPtEditPanel GetCtlPtEditPanel()
    {
        return ControlPointEditPanel;
    }

    public LayerActions GetLayerActionPanel()
    {
        return LayerActionPanel;
    }

    public HelpPanel GetHelpPanel()
    {
        return HelpTextPanel;
    }

    public CameraControl GetCameraControl()
    {
        return CamControl;
    }

    public UploadFileReceiver GetFileReceiver()
    {
        return FileReceiver;
    }

    public Transform GetPreviewObject()
    {
        return PreviewObject;
    }

    public Material GetCurveMtlDefault()
    {
        return CurveMtlDefault;
    }

    public Material GetCurveMtlSelected()
    {
        return CurveMtlSelected;
    }

    public Material GetCtlPtMtlDefault()
    {
        return CtlPtMtlDefault;
    }

    public Material GetCtlPtMtlEditing()
    {
        return CtlPtMtlEditing;
    }
}
