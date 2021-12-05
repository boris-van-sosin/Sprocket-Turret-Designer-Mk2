using System;
using UnityEngine;

public class CircularArcGeom : CurveGeomBase
{
    public override void AppendCtlPt(ControlPoint pt)
    {
        if (_cltPtsTransforms.Count >= 2)
        {
            throw new Exception("Circular arcs have only teo control points and an angle");
        }
        base.AppendCtlPt(pt);
        if (_cltPtsTransforms.Count == 2)
        {
            _innerCrv = new CircularArc<Vector3>(_cltPtsTransforms[0].position, _cltPtsTransforms[1].position, Blend, Magnitude, Rotate);
        }
        else
        {
            _innerCrv = null;
        }
    }

    public void SetAngle(float angle)
    {
        _innerCrv.SetAngle(angle);
    }

    public override int Order => 3;

    protected override ICurve<Vector3> InnerCurve => throw new NotImplementedException();

    public override bool CanRender() => _innerCrv != null;

    private float Magnitude(Vector3 vec) => vec.magnitude;
    private Vector3 Rotate(Vector3 vec, float angle) => Quaternion.AngleAxis(angle, Vector3.up) * vec;

    private CircularArc<Vector3> _innerCrv = null;
}
