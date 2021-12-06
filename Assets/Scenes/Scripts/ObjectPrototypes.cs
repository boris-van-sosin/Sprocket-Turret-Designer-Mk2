﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;

public class ObjectPrototypes : MonoBehaviour
{
    public Transform CtlPtPrototype;
    public LineRenderer CtlMeshPolylinePrototype;
    public PlaneLayer LayerPrototype;
    public BezierCurveGeom BezierPrototype;
    public CircularArcGeom CircularArcPrototype;
    public Trihedron MoveTrihedton;
    //public Trihedron RotateTrihedton;
    public HelpPanel HelpTextPanel;
    public CameraControl CamControl;

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

    public CircularArcGeom CreateCircularArc()
    {
        return Instantiate(CircularArcPrototype);
    }

    public PlaneLayer CreateLayer(float elevation)
    {
        return Instantiate(LayerPrototype, new Vector3(0f, elevation, 0f), Quaternion.identity);
    }

    public Trihedron GetMoveTridehron()
    {
        return MoveTrihedton;
    }

    //public Trihedron GetRotateTridehron()
    //{
    //    return RotateTrihedton;
    //}

    public HelpPanel GetHelpPanel()
    {
        return HelpTextPanel;
    }

    public CameraControl GetCameraControl()
    {
        return CamControl;
    }
}
