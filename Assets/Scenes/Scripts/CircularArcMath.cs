using System;
using System.Collections.Generic;
using UnityEngine;

public class CircularArc<T> : ICurve<T>
{
    public CircularArc(T startPt, T centerPt, float angle, Func<T, float, T, float, T> belndFunc, Func<T, float> magnitudeFunc, Func<T, float, T> rotateFunc, Func<T, T, T, float> invRotateFunc)
    {
        Start = startPt;
        Center = centerPt;
        _blendFunc = belndFunc;
        _magnitudeFunc = magnitudeFunc;
        _rotateFunc = rotateFunc;
        _invRotateFunc = invRotateFunc;
        Angle = angle;
    }

    public (float, float) Domain => (0f, 1f);

    public T Eval(float t)
    {
        return _blendFunc(_rotateFunc(RadiusVec, Mathf.Lerp(0f, Angle, t)), 1f, Center, 1f);
    }

    public void UpdateControlPoint(int idx, T newPt)
    {
        switch (idx)
        {
            case 0:
                Start = newPt;
                break;
            case 1:
                Center = newPt;
                break;
            case 2:
                SetAngle(_invRotateFunc(Start, Center, newPt));
                break;
            default:
                throw new Exception("Circular arc has only three control points");
        }
    }

    public void SetAngle(float angle)
    {
        if (angle > 360f || angle < -360f)
        {
            throw new Exception("Angle must be between -360 anf 360");
        }
        Angle = angle;
    }

    public T Center { get; private set; }
    public T Start { get; private set; }
    public float Angle { get; private set; }
    public T RadiusVec => _blendFunc(Start, 1f, Center, -1f);
    public float Radius => _magnitudeFunc(RadiusVec);
    public T End => Eval(1f);

    public IEnumerable<T> ControlPoints
    {
        get
        {
            yield return Start;
            yield return Center;
            yield return End;
        }
    }

    private readonly Func<T, float, T, float, T> _blendFunc;
    private readonly Func<T, float> _magnitudeFunc;
    private readonly Func<T, float, T> _rotateFunc;
    private readonly Func<T, T, T, float> _invRotateFunc;
}

