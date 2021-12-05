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

    public override int Order => _cltPtsTransforms.Count;

    private BezierCurve<Vector3> _innerCrv = null;
}
