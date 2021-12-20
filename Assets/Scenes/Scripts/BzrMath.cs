using System;
using System.Collections.Generic;
using System.Linq;

public class BezierCurve<T> : ICurve<T>
{
    public BezierCurve(IEnumerable<T> CtlPoints, Func<T, float, T, float, T> blendFunc)
    {
        _ctlMesh = CtlPoints.ToArray();
        Order = _ctlMesh.Length;
        if (Order <= 0)
        {
            throw new Exception("Attempted to create an empty Bezier curve");
        }
        _blendFunc = blendFunc;
        _tmpEvalArray = new float[Order];
    }

    public virtual T Eval(float x)
    {
        int i = 0;
        foreach (float b in BezierUtils.EvalBezierBasis(Order, x))
        {
            _tmpEvalArray[i++] = b;
        }
        T res = _ctlMesh[0];
        bool first = true;

        for (i = 0; i < Order; ++i)
        {
            if (first)
            {
                res = _blendFunc(_ctlMesh[i], _tmpEvalArray[i], _ctlMesh[0], 0f);
                first = false;
            }
            else
            {
                res = _blendFunc(res, 1f, _ctlMesh[i], _tmpEvalArray[i]);
            }
        }

        return res;
    }

    private IEnumerable<T> DeriveCtlMesh(Func<T, float, T, float, T> blendFunc)
    {
        for (int i = 0; i < _ctlMesh.Length - 1; ++i)
        {
            float coeff = (float)(Order - 1);
            yield return blendFunc(_ctlMesh[i + 1], coeff, _ctlMesh[i], -coeff);
        }
    }

    public virtual BezierCurve<T> Derivative()
    {
        return new BezierCurve<T>(DeriveCtlMesh(_blendFunc), _blendFunc);
    }

    public virtual (BezierCurve<T>, BezierCurve<T>) Subdivide(float t)
    {
        T[] firstPts = new T[_ctlMesh.Length],
            secondPts = new T[_ctlMesh.Length];
        float tc = 1f - t;

        for (int i = 0; i < _ctlMesh.Length; ++i)
        {
            secondPts[i] = _blendFunc(_ctlMesh[i], 1f, _ctlMesh[i], 0f);
        }
        firstPts[0] = _blendFunc(_ctlMesh[0], 1f, _ctlMesh[0], 0f);

        /* Apply the recursive algorithm to secondPts, and update firstPts with the */
        /* temporary results. Note we updated the first point of firstPts above.    */
        for (int i = 1; i < _ctlMesh.Length; ++i)
        {
            for (int j = 0; j < _ctlMesh.Length - i; ++j)
            {
                secondPts[j] = _blendFunc(_ctlMesh[j], tc, _ctlMesh[j + 1], t);
            }
            firstPts[i] = _blendFunc(secondPts[0], 1f, secondPts[0], 0f);
        }

        return (new BezierCurve<T>(firstPts, _blendFunc), new BezierCurve<T>(secondPts, _blendFunc));
    }

    public void UpdateControlPoint(int idx, T newPt)
    {
        _ctlMesh[idx] = newPt;
    }

    public virtual BezierCurve<T> RaiseDegree()
    {
        T[] raisedCtlPts = new T[_ctlMesh.Length + 1];

        raisedCtlPts[0] = _ctlMesh[0];
        raisedCtlPts[_ctlMesh.Length] = _ctlMesh[_ctlMesh.Length - 1];
        for (int i = 1; i < _ctlMesh.Length; ++i)
        {
            float a = ((float)i) / ((float)_ctlMesh.Length + 1f);
            raisedCtlPts[i] = _blendFunc(_ctlMesh[i - 1], a, _ctlMesh[i], 1f - a);
        }

        return new BezierCurve<T>(raisedCtlPts, _blendFunc);
    }

    public virtual BSplineCurve<T> ToBSpline()
    {
        return BSplineCurve<T>.UniformOpen(_ctlMesh, _ctlMesh.Length, _blendFunc);
    }

    public WeightedlBezierCurve<T> ToWeightedBezier()
    {
        return new WeightedlBezierCurve<T>(_ctlMesh, _blendFunc);
    }

    public (float, float) Domain => (0f, 1f);

    public IEnumerable<T> ControlPoints => _ctlMesh;

    public readonly int Order;
    protected readonly T[] _ctlMesh;
    protected readonly Func<T, float, T, float, T> _blendFunc;
    protected readonly float[] _tmpEvalArray;
}

public static class BezierUtils
{
    static public int EvalBinomial(int n, int k)
    {
        if (k > n >> 1)
        {
            return EvalBinomial(n, n - k);
        }
        if (k < 0 || k > n)
        {
            return 0;
        }

        if (_binomialCache.Count > n)
        {
            return _binomialCache[n][k];
        }

        if (n == 0)
        {
            _binomialCache.Add(new List<int>() { 1 });
            return 1;
        }

        List<int> n_thRow = new List<int>();
        for (int i = 0; i <= n >> 1; ++i)
        {
            n_thRow.Add(EvalBinomial(n - 1, i - 1) + EvalBinomial(n - 1, i));
        }
        _binomialCache.Add(n_thRow);
        return _binomialCache[n][k];
    }

    static public IEnumerable<float> EvalBezierBasis(int order, float t)
    {
        float tFactor = 1f;
        foreach (float x in EvalBezierBasisOneMinusT(order, t).Reverse())
        {
            yield return x * tFactor;
            tFactor *= t;
        }
    }

    static private IEnumerable<float> EvalBezierBasisOneMinusT(int order, float t)
    {
        float oneMinusTFactor = 1f;
        for (int i = order - 1; i >= 0; --i)
        {
            yield return EvalBinomial(order - 1, i) * oneMinusTFactor;
            oneMinusTFactor *= 1f - t;
        }
    }

    static readonly List<List<int>> _binomialCache = new List<List<int>>();
}

public class WeightedlBezierCurve<T> : BezierCurve<T>
{
    public WeightedlBezierCurve(IEnumerable<T> ctlPoints, IEnumerable<float> weights, Func<T, float, T, float, T> blendFunc)
        : base(ctlPoints, blendFunc)
    {
        int numWeights = weights.Count();
        if (numWeights != _ctlMesh.Length)
        {
            throw new Exception("Number of weights different from number of control points");
        }
        _weights = weights.ToArray();
    }

    public WeightedlBezierCurve(IEnumerable<T> ctlPoints, Func<T, float, T, float, T> blendFunc)
    : base(ctlPoints, blendFunc)
    {
        _weights = new float[_ctlMesh.Length];
        for (int i = 0; i < _ctlMesh.Length; ++i)
        {
            _weights[i] = 1f;
        }
    }

    public virtual T Eval(float x)
    {
        int i = 0;
        foreach (float b in BezierUtils.EvalBezierBasis(Order, x))
        {
            _tmpEvalArray[i++] = b;
        }
        T res = _ctlMesh[0];
        float resWeight = _weights[0];
        bool first = true;

        for (i = 0; i < Order; ++i)
        {
            if (first)
            {
                res = _blendFunc(_ctlMesh[i], _tmpEvalArray[i], _ctlMesh[0], 0f);
                resWeight = _weights[i] * _tmpEvalArray[i];
                first = false;
            }
            else
            {
                res = _blendFunc(res, 1f, _ctlMesh[i], _tmpEvalArray[i]);
                resWeight += _weights[i] * _tmpEvalArray[i];
            }
        }

        return _blendFunc(res, 1f / resWeight, res, 0f);
    }

    public override BezierCurve<T> RaiseDegree()
    {
        T[] raisedCtlPts = new T[_ctlMesh.Length + 1];
        float[] raisedWeights = new float[_ctlMesh.Length + 1];

        raisedCtlPts[0] = _ctlMesh[0];
        raisedWeights[0] = _weights[0];
        raisedCtlPts[_ctlMesh.Length] = _ctlMesh[_ctlMesh.Length - 1];
        raisedWeights[_ctlMesh.Length] = _weights[_ctlMesh.Length - 1];
        for (int i = 1; i < _ctlMesh.Length; ++i)
        {
            float a = ((float)i) / ((float)_ctlMesh.Length + 1f);
            raisedCtlPts[i] = _blendFunc(_ctlMesh[i - 1], a, _ctlMesh[i], 1f - a);
            raisedWeights[i] = _weights[i - 1] * a + _weights[i] * 1f - a;
        }

        return new WeightedlBezierCurve<T>(raisedCtlPts, raisedWeights, _blendFunc);
    }

    public override BSplineCurve<T> ToBSpline()
    {
        return WeightedlBSplineCurve<T>.UniformOpen(_ctlMesh, _weights, Order, _blendFunc);
    }

    public override BezierCurve<T> Derivative()
    {
        throw new NotImplementedException();
    }

    public override (BezierCurve<T>, BezierCurve<T>) Subdivide(float t)
    {
        T[] firstPts = new T[_ctlMesh.Length],
            secondPts = new T[_ctlMesh.Length];
        float[] firstWeights = new float[_ctlMesh.Length],
                secondWeights = new float[_ctlMesh.Length];
        float tc = 1f - t;

        for (int i = 0; i < _ctlMesh.Length; ++i)
        {
            secondPts[i] = _blendFunc(_ctlMesh[i], 1f, _ctlMesh[i], 0f);
            secondWeights[i] = _weights[i];
        }
        firstPts[0] = _blendFunc(_ctlMesh[0], 1f, _ctlMesh[0], 0f);
        firstWeights[0] = _weights[0];

        /* Apply the recursive algorithm to secondPts, and update firstPts with the */
        /* temporary results. Note we updated the first point of firstPts above.    */
        for (int i = 1; i < _ctlMesh.Length; ++i)
        {
            for (int j = 0; j < _ctlMesh.Length - i; ++j)
            {
                secondPts[j] = _blendFunc(_ctlMesh[j], tc, _ctlMesh[j + 1], t);
                secondWeights[j] = _weights[j] * tc + _weights[j + 1] * t;
            }
            firstPts[i] = _blendFunc(secondPts[0], 1f, secondPts[0], 0f);
            firstWeights[i] = secondWeights[0];
        }

        return (new WeightedlBezierCurve<T>(firstPts, firstWeights, _blendFunc), new WeightedlBezierCurve<T>(secondPts, secondWeights, _blendFunc));
    }

    public IEnumerable<float> Weights => _weights;

    public void UpdateControlPoint(int idx, T newPt, float w)
    {
        _ctlMesh[idx] = newPt;
        _weights[idx] = w;
    }
    public void UpdateWeight(int idx, float newW)
    {
        _weights[idx] = newW;
    }

    private readonly float[] _weights;
}