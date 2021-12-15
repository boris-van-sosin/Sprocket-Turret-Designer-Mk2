using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BezierCurveGeom : CurveGeomBase
{
    public override void AppendCtlPt(ControlPoint pt)
    {
        base.AppendCtlPt(pt);
        _innerCrv = new BezierCurve<Vector3>(_cltPtsTransforms.Select(p => p.position), Blend);
    }

    protected override ICurve<Vector3> InnerCurve => _innerCrv;

    public override bool CanRender()
    {
        return _cltPtsTransforms.Count > 0;
    }

    public override CurveGeomBase Copy()
    {
        BezierCurveGeom copy = GeomObjectFactory.CreateBezierCurve();

        foreach (Transform tr in CtlPts)
        {
            ControlPoint pt = GeomObjectFactory.CreateCtlPt(tr.position).GetComponent<ControlPoint>();
            pt.transform.SetParent(copy.transform);
            pt.ContainingCurve = copy;
            copy._cltPtsTransforms.Add(pt.transform);
        }

        copy._innerCrv = new BezierCurve<Vector3>(copy._cltPtsTransforms.Select(p => p.position), Blend);

        return copy;
    }

    public override int Order => _cltPtsTransforms.Count;

    private BezierCurve<Vector3> _innerCrv = null;
}
