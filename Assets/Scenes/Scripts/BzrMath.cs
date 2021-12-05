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

    public T Eval(float x)
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

    public BezierCurve<T> Derivative()
    {
        return new BezierCurve<T>(DeriveCtlMesh(_blendFunc), _blendFunc);
    }

    public void UpdateControlPoint(int idx, T newPt)
    {
        _ctlMesh[idx] = newPt;
    }

    public (float, float) Domain => (0f, 1f);

    public readonly int Order;
    private readonly T[] _ctlMesh;
    private readonly Func<T, float, T, float, T> _blendFunc;
    private readonly float[] _tmpEvalArray;
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
