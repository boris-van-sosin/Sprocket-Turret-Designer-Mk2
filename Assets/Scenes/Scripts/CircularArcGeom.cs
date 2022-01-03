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
        }
        else
        {
            _innerCrv = null;
        }
    }

    private float AngleFromVectors(Vector3 start, Vector3 center, Vector3 end)
    {
        return Vector3.SignedAngle(start - center, end - center, Vector3.up);
    }

    public void SetAngle(float angle)
    {
        _innerCrv.SetAngle(angle);
    }

    public override int Order => 3;

    protected override ICurve<Vector3> InnerCurve => throw new NotImplementedException();

    public override bool CanRender() => _innerCrv != null;

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

        return copy;
    }

    private float Magnitude(Vector3 vec) => vec.magnitude;
    private Vector3 Rotate(Vector3 vec, float angle) => Quaternion.AngleAxis(angle, Vector3.up) * vec;

    private CircularArc<Vector3> _innerCrv = null;
}
