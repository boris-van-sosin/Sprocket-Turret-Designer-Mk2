using System;
using System.Collections.Generic;
using UnityEngine;

public interface ICurve<T>
{
    T Eval(float t);
    void UpdateControlPoint(int idx, T newPt);
    ValueTuple<float, float> Domain { get; }
}

public abstract class CurveGeomBase : MonoBehaviour
{
    protected virtual void Awake()
    {
        _curveLR = GeomObjectFactory.AddLineRendererToObject(gameObject);
        _ctlMeshLR = GeomObjectFactory.AddLineRendererToObject(gameObject);
        _curveLR.enabled = false;
        _ctlMeshLR.enabled = false;
        NumSamples = 60;
    }
    public virtual void AppendCtlPt(ControlPoint pt)
    {
        _cltPtsTransforms.Add(pt.transform);
    }

    public void TryRender()
    {
        if (!CanRender())
        {
            _curveLR.enabled = false;
            return;
        }
        _curveLR.enabled = true;

        ValueTuple<float, float> domain = Domain;
        _curveLR.positionCount = NumSamples + 1;
        float step = (domain.Item2 - domain.Item1) / (float)(NumSamples);
        float t = domain.Item1;
        for (int i = 0; i < NumSamples + 1; ++i, t += step)
        {
            _curveLR.SetPosition(i, InnerCurve.Eval(t));
        }
    }

    public abstract bool CanRender();
    protected virtual (float, float) Domain => InnerCurve.Domain;
    protected abstract ICurve<Vector3> InnerCurve { get; }
    protected static Vector3 Blend(Vector3 P1, float a, Vector3 P2, float b) => (P1 * a) + (P2 * b);
    public IReadOnlyList<Transform> CtlPts => _cltPtsTransforms;
    public void UpdateControlPoint(ControlPoint pt)
    {
        int idx;
        if ((idx = _cltPtsTransforms.IndexOf(pt.transform)) >= 0)
        {
            InnerCurve.UpdateControlPoint(idx, pt.transform.position);
        }
    }

    public int NumSamples { get; set; }
    protected List<Transform> _cltPtsTransforms = new List<Transform>();
    public abstract int Order { get; }
    private LineRenderer _ctlMeshLR;
    private LineRenderer _curveLR;
}