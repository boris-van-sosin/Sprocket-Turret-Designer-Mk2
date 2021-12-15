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
        _colliderDensity = 5;
        int numColliders = NumSamples / _colliderDensity;
        _segColliders = new CapsuleCollider[numColliders];
        for (int i = 0; i < numColliders; ++i)
        {
            _segColliders[i] = GeomObjectFactory.CreateCurveSegCollider(0.05f / 2.0f + 0.01f, this);
            _segColliders[i].enabled = false;
        }
        _selected = false;
    }
    public virtual void AppendCtlPt(ControlPoint pt)
    {
        _cltPtsTransforms.Add(pt.transform);
        pt.ContainingCurve = this;
    }

    public void TryRender()
    {
        if (!CanRender())
        {
            _curveLR.enabled = false;
            foreach (CapsuleCollider cap in _segColliders)
            {
                cap.enabled = false;
            }
            return;
        }
        _curveLR.enabled = true;

        ValueTuple<float, float> domain = Domain;
        _curveLR.positionCount = NumSamples + 1;
        float step = (domain.Item2 - domain.Item1) / (float)(NumSamples);
        float t = domain.Item1;
        for (int i = 0, colliderIdx = 0; i < NumSamples + 1; ++i, t += step)
        {
            _curveLR.SetPosition(i, InnerCurve.Eval(t));
            if (i > 0 && i % _colliderDensity == 0)
            {
                Vector3 pt1 = _curveLR.GetPosition(i - _colliderDensity);
                Vector3 pt2 = _curveLR.GetPosition(i);
                _segColliders[colliderIdx].enabled = true;
                _segColliders[colliderIdx].transform.position = (pt1 + pt2) / 2.0f;
                _segColliders[colliderIdx].transform.rotation = Quaternion.LookRotation(pt2 - pt1, Vector3.up);
                _segColliders[colliderIdx].direction = 2;
                _segColliders[colliderIdx].height = (pt2 - pt1).magnitude + 2f * _segColliders[colliderIdx].radius;
                ++colliderIdx;
            }
        }
    }

    public bool Selected
    {
        get
        {
            return _selected;
        }
        set
        {
            _selected = value;
            if (_selected)
            {
                _curveLR.sharedMaterial = GeomObjectFactory.GetCurveMtlSelected();
                _curveLR.startColor = _curveLR.endColor = GlobalData.SelectedCrvColor;
            }
            else
            {
                _curveLR.sharedMaterial = GeomObjectFactory.GetCurveMtlDefault();
                _curveLR.startColor = _curveLR.endColor = GlobalData.DefaultCrvColor;
            }
        }
    }

    public abstract bool CanRender();
    public Vector3 Eval(float t) => InnerCurve.Eval(t);
    public Vector3 EvalStart() => InnerCurve.Eval(Domain.Item1);
    public Vector3 EvalEnd() => InnerCurve.Eval(Domain.Item2);
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
    public abstract CurveGeomBase Copy();

    public int NumSamples { get; set; }
    private int _colliderDensity;
    protected List<Transform> _cltPtsTransforms = new List<Transform>();
    public abstract int Order { get; }
    private LineRenderer _ctlMeshLR;
    private LineRenderer _curveLR;
    private CapsuleCollider[] _segColliders;
    private bool _selected;
}