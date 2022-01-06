using System;
using UnityEngine;

public class CircularArcGeom : CurveGeomBase
{
    public override void AppendCtlPt(ControlPoint pt)
    {
        if (_cltPtsTransforms.Count >= 3)
        {
            throw new Exception("Circular arcs have only teo control points and an angle");
        }
        base.AppendCtlPt(pt);
        if (_cltPtsTransforms.Count == 3)
        {
            _innerCrv = new CircularArc<Vector3>(_cltPtsTransforms[0].position,
                                                 _cltPtsTransforms[1].position,
                                                 AngleFromVectors(_cltPtsTransforms[0].position, _cltPtsTransforms[1].position, _cltPtsTransforms[2].position),
                                                 Blend, Magnitude, Rotate, AngleFromVectors);
            _cltPtsTransforms[2].position = _innerCrv.End;
            ForcePositiveX();
        }
        else
        {
            _innerCrv = null;
        }
    }

    public static float AngleFromVectors(Vector3 start, Vector3 center, Vector3 end)
    {
        return Vector3.SignedAngle(start - center, end - center, Vector3.up);
    }

    private void ForcePositiveX()
    {
        if (_innerCrv == null || (-180f < _innerCrv.Angle && _innerCrv.Angle < 180f) || _innerCrv.Center.x > _innerCrv.Radius)
        {
            return;
        }

        float angle = Vector3.SignedAngle(_innerCrv.Start, new Vector3(_innerCrv.Center.x - _innerCrv.Radius, _innerCrv.Center.y, _innerCrv.Center.z), Vector3.up);
        if (angle < 0f && _innerCrv.Angle > 0f || angle > 0f && _innerCrv.Angle < 0f)
        {
            return;
        }
        Vector3 pt = Eval(angle / _innerCrv.Angle);
        if (pt.x < 0)
        {
            SetAngle(-_innerCrv.Angle);
        }
    }

    public void SetAngle(float angle)
    {
        _innerCrv.SetAngle(angle);
        if (_cltPtsTransforms.Count == 3)
        {
            _cltPtsTransforms[2].position = _innerCrv.End;
        }
    }

    public override int Order => 3;

    protected override ICurve<Vector3> InnerCurve => _innerCrv;

    public override bool CanRender() => _innerCrv != null;

    public override void UpdateControlPoint(ControlPoint pt)
    {
        base.UpdateControlPoint(pt);
        _cltPtsTransforms[2].position = _innerCrv.End;
        ForcePositiveX();
    }

    public override CurveGeomBase Copy()
    {
        if (_cltPtsTransforms.Count < 3)
        {
            throw new Exception("Cannot copy an incomplete circular arc");
        }

        CircularArcGeom copy = GeomObjectFactory.CreateCircularArc();

        for (int i = 0; i < 3; ++i)
        {
            ControlPoint pt = GeomObjectFactory.CreateCtlPt(CtlPts[i].position).GetComponent<ControlPoint>();
            pt.transform.SetParent(copy.transform);
            pt.ContainingCurve = copy;
            copy._cltPtsTransforms.Add(pt.transform);
        }

        copy._innerCrv = new CircularArc<Vector3>(_cltPtsTransforms[0].position,
                                                 _cltPtsTransforms[1].position,
                                                 AngleFromVectors(_cltPtsTransforms[0].position, _cltPtsTransforms[1].position, _cltPtsTransforms[2].position),
                                                 Blend, Magnitude, Rotate, AngleFromVectors);
        copy.SetAngle(_innerCrv.Angle);
        copy.ForcePositiveX();

        return copy;
    }

    public static float Magnitude(Vector3 vec) => vec.magnitude;
    public static Vector3 Rotate(Vector3 vec, float angle) => Quaternion.AngleAxis(angle, Vector3.up) * vec;

    private CircularArc<Vector3> _innerCrv = null;
}
