using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSplineCurveGeom : CurveGeomBase
{
    public override void AppendCtlPt(ControlPoint pt)
    {
        base.AppendCtlPt(pt);
        if (_cltPtsTransforms.Count > 1 && _kv != null && _kv.Count == _cltPtsTransforms.Count + Order)
        {
            _innerCrv = new BSplineCurve<Vector3>(_cltPtsTransforms.Select(p => p.position), _kv, Blend);
        }
    }

    public void SetKVToUniformOpen()
    {
        if (_cltPtsTransforms.Count < Order)
        {
            return;
        }
        if (_kv == null)
        {
            _kv = BSplineCurve<Vector3>.CreateOpenKV(Order, _cltPtsTransforms.Count).ToList();
        }
        else
        {
            float[] newkV = BSplineCurve<Vector3>.CreateOpenKV(Order, _cltPtsTransforms.Count);
            for (int i = 0; i < Mathf.Min(_kv.Count, newkV.Length); i++)
            {
                _kv[i] = newkV[i];
            }
            if (_kv.Count > newkV.Length)
            {
                _kv.RemoveRange(newkV.Length, newkV.Length - _kv.Count);
            }
            else if (_kv.Count < newkV.Length)
            {
                _kv.AddRange(newkV.Skip(_kv.Count));
            }
        }
        _innerCrv = new BSplineCurve<Vector3>(_cltPtsTransforms.Select(p => p.position), _kv, Blend);
    }

    public void ReplaceKV(IReadOnlyList<float> newKV)
    {
        if (_innerCrv.ReplaceKV(newKV))
        {
            _kv.Clear();
            _kv.AddRange(newKV);
            TryRender();
        }
    }

    protected override ICurve<Vector3> InnerCurve => _innerCrv;

    public override bool CanRender()
    {
        return (_cltPtsTransforms.Count >= Order && _cltPtsTransforms.Count + Order == _kv.Count);
    }

    public override int Order => _order;

    public void SetOrder(int order)
    {
        _order = order;
    }

    public override CurveGeomBase Copy()
    {
        BSplineCurveGeom copy = GeomObjectFactory.CreateBSplineCurve();

        foreach (Transform tr in CtlPts)
        {
            ControlPoint pt = GeomObjectFactory.CreateCtlPt(tr.position).GetComponent<ControlPoint>();
            pt.transform.SetParent(copy.transform);
            pt.ContainingCurve = copy;
            copy._cltPtsTransforms.Add(pt.transform);
        }

        copy.SetOrder(Order);
        copy._kv = new List<float>(_kv);

        copy._innerCrv = new BSplineCurve<Vector3>(copy._cltPtsTransforms.Select(p => p.position), copy._kv, Blend);

        return copy;
    }
    public IReadOnlyList<float> KnotVector => _kv;

    private int _order;
    private List<float> _kv;
    private BSplineCurve<Vector3> _innerCrv = null;
}
