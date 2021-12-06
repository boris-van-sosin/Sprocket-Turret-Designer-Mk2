﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSplineCurve<T> : ICurve<T>
{
    public BSplineCurve(IEnumerable<T> CtlPoints, IEnumerable<float> KV, Func<T, float, T, float, T> blendFunc)
    {
        _ctlMesh = CtlPoints.ToArray();
        _kv = KV.ToArray();
        if (_kv.Length <= 1)
        {
            throw new Exception("Attempted to create B-spline in which empty knot vector");
        }
        Order = _kv.Length - _ctlMesh.Length;
        if (Order < 0)
        {
            throw new Exception("Attempted to create B-spline in which order is greater than length");
        }
        _blendFunc = blendFunc;
        _tmpEvalArray = new float[Order];
    }

    public static BSplineCurve<T> UniformOpen(IEnumerable<T> CtlPoints, int order, Func<T, float, T, float, T> blendFunc)
    {
        int length = CtlPoints.Count();
        if (length < order)
        {
            throw new Exception("Attempted to create B-spline in which order is greater than length");
        }
        return new BSplineCurve<T>(CtlPoints, CreateOpenKV(order, length), blendFunc);
    }

    public static float[] CreateOpenKV(int order, int ctlMeshLen)
    {
        int kvLen = ctlMeshLen + order;
        int knotItervals = ctlMeshLen - order + 1;
        float[] kv = new float[kvLen];
        int i = 1, j = ctlMeshLen - order, k = 0;
        while (k < order)
        {
            kv[k] = 0;
            ++k;
        }
        while (i <= j)
        {
            kv[k] = ((float)i) / knotItervals;
            ++k;
            ++i;
        }
        for (j = 0; j < order; j++)
        {
            kv[k++] = ((float)i) / knotItervals;
        }
        return kv;
    }

    public bool ReplaceKV(IReadOnlyList<float> newKV)
    {
        if (newKV.Count != _kv.Length)
        {
            return false;
        }
        for (int i = 0; i < newKV.Count - 1; ++i)
        {
            if (newKV[i] > newKV[i + 1])
                return false;
        }
        for (int i = 0; i < newKV.Count; ++i)
            _kv[i] = newKV[i];

        return true;
    }

    private int FindKnotInterval(float x)
    {
        if (x < _kv[0])
        {
            throw new Exception("parameter outside domain of B-spline");
        }
        for (int i = 0; i < _kv.Length - 1; ++i)
        {
            if (_kv[i] <= x && x < _kv[i + 1] || (x <= _kv[i + 1] && Mathf.Approximately(_kv[i + 1], _kv.Last())))
            {
                return i;
            }
        }
        throw new Exception("parameter outside domain of B-spline");
    }

    public virtual T Eval(float x)
    {
        ValueTuple<IList<float>, int> coeffs = BSplineUtils.DeBoorEvalBasisReduced(_kv, Order, x, false, _tmpEvalArray);
        T res = _ctlMesh[0];
        bool first = true;

        int ctlPtIdx = coeffs.Item2;
        for (int i = 0; i < Order; ++i, ++ctlPtIdx)
        {
            if (first)
            {
                res = _blendFunc(_ctlMesh[ctlPtIdx], coeffs.Item1[i], _ctlMesh[0], 0f);
                first = false;
            }
            else
            {
                res = _blendFunc(res, 1f, _ctlMesh[ctlPtIdx], coeffs.Item1[i]);
            }
        }

        return res;
    }

    private IEnumerable<T> DeriveCtlMesh(Func<T, float, T, float, T> blendFunc)
    {
        for (int i = 0; i < _ctlMesh.Length - 1; ++i)
        {
            float coeff = (Order - 1) / (_kv[i + Order] - _kv[i + 1]);
            yield return blendFunc(_ctlMesh[i + 1], coeff, _ctlMesh[i], -coeff);
        }
    }

    public virtual BSplineCurve<T> Derivative()
    {
        return new BSplineCurve<T>(DeriveCtlMesh(_blendFunc), _kv.Skip(1).Take(_kv.Length - 2), _blendFunc);
    }

    public virtual BSplineCurve<T> RefineAtParam(float t)
    {
        int cutIdx = 0;
        float[] newKV = new float[_kv.Length + 1];
        bool inserted = false;
        for (int i = 0; i < _kv.Length; ++i)
        {
            if (!inserted)
            {
                if (_kv[i] < t)
                {
                    newKV[i] = _kv[i];
                }
                else
                {
                    cutIdx = i - 1;
                    if (t - _kv[i] < 1e-5)
                    {
                        // Snap to previous knot:
                        t = _kv[i];
                    }
                    else if (_kv[i + 1] - t < 1e-5)
                    {
                        // Snap to next knot:
                        t = _kv[i + 1];
                    }
                    newKV[i] = t;
                    newKV[i + 1] = _kv[i];
                    inserted = true;
                }
            }
            else
            {
                newKV[i + 1] = _kv[i];
            }
        }

        T[] newCtlPts = new T[_ctlMesh.Length + 1];
        for (int i = 0; i < newCtlPts.Length; ++i)
        {
            if (i <= cutIdx - Order)
            {
                newCtlPts[i] = _ctlMesh[i];
            }
            else if (cutIdx - Order + 1 <= i && i <= cutIdx)
            {
                float a = (newKV[i + Order + 1] - t) / (newKV[i + Order + 1] - newKV[i]),
                      b = (t - newKV[i]) / (newKV[i + Order + 1] - newKV[i]);
                newCtlPts[i] = _blendFunc(_ctlMesh[i - 1], a, _ctlMesh[i], b);
            }
            else if (cutIdx < i)
            {
                newCtlPts[i] = _ctlMesh[i - 1];
            }
        }

        return new BSplineCurve<T>(newCtlPts, newKV, _blendFunc);
    }

    public void UpdateControlPoint(int idx, T newPt)
    {
        _ctlMesh[idx] = newPt;
    }

    public (float, float) Domain => BSplineUtils.GetDomain(_kv, Order);

    public readonly int Order;
    protected readonly T[] _ctlMesh;
    protected readonly float[] _kv;
    protected readonly Func<T, float, T, float, T> _blendFunc;
    protected readonly float[] _tmpEvalArray;
}

public class WeightedlBSplineCurve<T> : BSplineCurve<T>
{
    public WeightedlBSplineCurve(IEnumerable<T> CtlPoints, IEnumerable<float> Weights, IEnumerable<float> KV, Func<T, float, T, float, T> blendFunc)
        : base(CtlPoints, KV, blendFunc)
    {
        int numWeights = Weights.Count();
        if (numWeights != _ctlMesh.Length)
        {
            throw new Exception("Number of weights different from number of control points");
        }
        _weights = Weights.ToArray();
    }

    public WeightedlBSplineCurve(IEnumerable<Tuple<T, float>> CtlPoints, IEnumerable<float> KV, Func<T, float, T, float, T> blendFunc)
        : this(CtlPoints.Select(V => V.Item1), CtlPoints.Select(V => V.Item2), KV, blendFunc)
    {
    }

    public WeightedlBSplineCurve(IEnumerable<ValueTuple<T, float>> CtlPoints, IEnumerable<float> KV, Func<T, float, T, float, T> blendFunc)
    : this(CtlPoints.Select(V => V.Item1), CtlPoints.Select(V => V.Item2), KV, blendFunc)
    {
    }

    public static WeightedlBSplineCurve<T> UniformOpen(IEnumerable<T> CtlPoints, IEnumerable<float> Weights, int order, Func<T, float, T, float, T> blendFunc)
    {
        int length = CtlPoints.Count();
        if (length < order)
        {
            throw new Exception("Attempted to create B-spline in which order is greater than length");
        }
        return new WeightedlBSplineCurve<T>(CtlPoints, Weights, CreateOpenKV(order, length), blendFunc);
    }

    public static WeightedlBSplineCurve<T> UniformOpen(IEnumerable<Tuple<T, float>> CtlPoints, int order, Func<T, float, T, float, T> blendFunc)
    {
        return UniformOpen(CtlPoints.Select(V => V.Item1), CtlPoints.Select(V => V.Item2), order, blendFunc);
    }

    public static WeightedlBSplineCurve<T> UniformOpen(IEnumerable<ValueTuple<T, float>> CtlPoints, int order, Func<T, float, T, float, T> blendFunc)
    {
        return UniformOpen(CtlPoints.Select(V => V.Item1), CtlPoints.Select(V => V.Item2), order, blendFunc);
    }

    public override T Eval(float x)
    {
        ValueTuple<IList<float>, int> coeffs = BSplineUtils.DeBoorEvalBasisReduced(_kv, Order, x, false, _tmpEvalArray);
        T res = _ctlMesh[0];
        float resW = _weights[0];
        bool first = true;

        int ctlPtIdx = coeffs.Item2;
        for (int i = 0; i < Order; ++i, ++ctlPtIdx)
        {
            if (first)
            {
                res = _blendFunc(_ctlMesh[ctlPtIdx], coeffs.Item1[i], _ctlMesh[0], 0f);
                resW = _weights[ctlPtIdx] * coeffs.Item1[i];
                first = false;
            }
            else
            {
                res = _blendFunc(res, 1f, _ctlMesh[ctlPtIdx], coeffs.Item1[i]);
                resW += _weights[ctlPtIdx] * coeffs.Item1[i];
            }
        }

        return _blendFunc(res, 1f / resW, res, 0f);
    }

    public override BSplineCurve<T> RefineAtParam(float t)
    {
        int cutIdx = 0;
        float[] newKV = new float[_kv.Length + 1];
        bool inserted = false;
        for (int i = 0; i < _kv.Length; ++i)
        {
            if (!inserted)
            {
                if (_kv[i] < t)
                {
                    newKV[i] = _kv[i];
                }
                else
                {
                    cutIdx = i - 1;
                    if (t - _kv[i] < 1e-5)
                    {
                        // Snap to previous knot:
                        t = _kv[i];
                    }
                    else if (_kv[i + 1] - t < 1e-5)
                    {
                        // Snap to next knot:
                        t = _kv[i + 1];
                    }
                    newKV[i] = t;
                    newKV[i + 1] = _kv[i];
                    inserted = true;
                }
            }
            else
            {
                newKV[i + 1] = _kv[i];
            }
        }

        T[] newCtlPts = new T[_ctlMesh.Length + 1];
        float[] newWeights = new float[_ctlMesh.Length + 1];
        for (int i = 0; i < newCtlPts.Length; ++i)
        {
            if (i <= cutIdx - Order)
            {
                newCtlPts[i] = _ctlMesh[i];
                newWeights[i] = _weights[i];
            }
            else if (cutIdx - Order + 1 <= i && i <= cutIdx)
            {
                float a = (newKV[i + Order + 1] - t) / (newKV[i + Order + 1] - newKV[i]),
                      b = (t - newKV[i]) / (newKV[i + Order + 1] - newKV[i]);
                newCtlPts[i] = _blendFunc(_ctlMesh[i - 1], a, _ctlMesh[i], b);
                newWeights[i] = _weights[i - 1] * a + _weights[i] * b;
            }
            else if (cutIdx < i)
            {
                newCtlPts[i] = _ctlMesh[i - 1];
                newWeights[i] = _weights[i - 1];
            }
        }

        return new WeightedlBSplineCurve<T>(newCtlPts, newWeights, newKV, _blendFunc);
    }

    public override BSplineCurve<T> Derivative()
    {
        throw new NotImplementedException();
    }

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

public static class BSplineUtils
{
    public static ValueTuple<IList<float>, int> DeBoorEvalBasisReduced(IReadOnlyList<float> KV, int Order, float t, bool Periodic)
    {
        int OrigLen = KV.Count - Order;
        int Len = !Periodic ? OrigLen : OrigLen + Order - 1;
        return DeBoorEvalBasisReduced(KV, Order, t, Periodic, new float[Len]);
    }

    public static ValueTuple<IList<float>, int> DeBoorEvalBasisReduced(IReadOnlyList<float> KV, int Order, float t, bool Periodic, IList<float> preAllocCoeffs)
    {
        VerifyInDomain(KV, Order, t);
        int OrigLen = KV.Count - Order;
        int Len = !Periodic ? OrigLen : OrigLen + Order - 1;
        int KVLen = Order + Len; // Takes periodicity into account, if applicable.
        int Index = LastKnotIdxLE(KV, t);
        int IndexFirst;

        float[] Basis = new float[Len];

        if (Index >= KVLen - 1)
        {
            // We are at the end of the parametric domain and this is open-end condition.
            // Simply return last point.
            for (int j = 0; j < Basis.Length - 1; j++)
            {
                Basis[j] = 0f;
            }
            Basis[Order - 1] = 1f;
            IndexFirst = Len - Order;
            return new ValueTuple<float[], int>(Basis, IndexFirst);
        }
        else
        {
            Basis[0] = 1f;
        }

        for (int i = 2; i <= Order; ++i)
        {
            int KVIdx_l = Index + i - 1, 
                KVIdx_l1 = KVIdx_l + 1,
                KVIdx_li1 = Index,
                BasisIdx = i - 1;
            float s1, s2, s2inv;

            if ((s2 = KV[KVIdx_l] - KV[KVIdx_l + 1]) >= Mathf.Epsilon)
                s2inv = 1.0f / s2;
            else
                s2inv = 0.0f;

            for (int l = i - 1; l >= 0; --l)
            {  
                // And all basis funcs. of order i:
                if (s2inv == 0.0)
                {
                    Basis[BasisIdx--] = 0f;
                    KVIdx_l1--;
                }
                else
                    Basis[BasisIdx--] *= (KV[KVIdx_l1--] - t) * s2inv;

                if (l > 0 && (s1 = KV[KVIdx_l--] - KV[KVIdx_li1--]) >= Mathf.Epsilon)
                {
                    s2inv = 1f / s1;
                    Basis[BasisIdx + 1] += Basis[BasisIdx] * (t - KV[KVIdx_li1 + 1]) * s2inv;
                }
                else
                    s2inv = 0f;
            }
        }

        if ((IndexFirst = Index - Order + 1) >= OrigLen)
            IndexFirst -= OrigLen;

        return new ValueTuple<float[], int>(Basis, IndexFirst);
    }

    public static int LastKnotIdxLE(IReadOnlyList<float> KV, float t)
    {
        int KVLen = KV.Count;
        int Step = KV.Count >> 1;
        int StartIdx = 0;
        int i, KVIdx;

        // Rough binary search:
        while (Step > 2)
        {
            if (KV[StartIdx + Step] <= t ||
                Mathf.Approximately(KV[StartIdx + Step], t))
                StartIdx += Step;
            Step >>= 1;
        }

        // Find the exact location:
        KVIdx = StartIdx;
        for (i = StartIdx;
             i < KVLen && (KV[KVIdx] <= t || Mathf.Approximately(KV[KVIdx], t));
             ++i, ++KVIdx) ;

        return i - 1;
    }

    public static ValueTuple<float, float> GetDomain(IReadOnlyList<float> KV, int Order)
    {
        if (KV.Count < 2 * Order)
        {
            throw new Exception("Knot vector too short for this order");
        }
        return new ValueTuple<float, float>(KV[Order - 1], KV[KV.Count - Order]);
    }

    public static void VerifyInDomain(IReadOnlyList<float> KV, int Order, float x)
    {
        ValueTuple<float, float> domain = GetDomain(KV, Order);
        if (x < domain.Item1 || x > domain.Item2)
        {
            throw new Exception("parameter outside domain of B-spline");
        }
    }
}

public enum GeomType { Bezier, Bspline };