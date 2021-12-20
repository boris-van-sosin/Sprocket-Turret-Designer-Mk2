using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ZeroSolver
{
    public ZeroSolver(BezierCurve<float> crv) : this(crv, 50) { }

    public ZeroSolver(BezierCurve<float> crv, int maxIters)
    {
        if (crv is WeightedlBezierCurve<float> weigtedCrv)
        {
            _crv = new BezierCurve<float>(weigtedCrv.ControlPoints, SolverUtils.BlendFloat);
        }
        else
        {
            _crv = crv;
        }
        _crvPts = crv.ControlPoints.ToArray();
        _dCrv = _crv.Derivative();
        _maxIters = maxIters;
    }

    private static float SolveNumeric(BezierCurve<float> crv, BezierCurve<float>dCrv, int maxIters, float tolerance, float guess)
    {
        float res = guess;
        float val = crv.Eval(res);
        int iter = 0;
        while (Mathf.Abs(val) > tolerance)
        {
            res -= val / dCrv.Eval(res);
            val = crv.Eval(res);
            if (++iter > maxIters)
            {
                throw new Exception("Newton's method failed to converge.");
            }
        }

        return res;
    }

    public float SolveNumeric(float tolerance)
    {
        return SolveNumeric(_crv, _dCrv, _maxIters, tolerance, _crv.Domain.Item2 * 0.5f + _crv.Domain.Item1 * 0.5f);
    }

    public (List<float>, SolverUtils.SolverResult) SolveSubdiv(float tolerance)
    {
        (List<float>, SolverUtils.SolverResult) res = SolveSubdivInner(_crv, _crvPts, _dCrv, _maxIters, _maxSubdivDepth, tolerance);
        GC.Collect();
        return res;
    }

    private static (List<float>, SolverUtils.SolverResult) SolveSubdivInner(BezierCurve<float> crv, float[] crvPts, BezierCurve<float> dCrv, int maxNumericIters, int maxSubdivDepth, float tolerance)
    {
        int numCrossings = 0;
        float crossingParam = crv.Domain.Item2 * 0.5f + crv.Domain.Item1 * 0.5f;
        bool allNegative = true, allPositive = true;

        for (int i = 0; i < crvPts.Length; ++i)
        {
            if (crvPts[i] > tolerance)
            {
                allNegative = false;
            }
            else if (crvPts[i] < -tolerance)
            {
                allPositive = false;
            }

            if (i > 0 && crvPts[i] * crvPts[i - 1] < 0f)
            {
                ++numCrossings;
                crossingParam = ((i - 1f) / (crvPts.Length - 1f)) * 0.5f + (i / (crvPts.Length - 1f)) * 0.5f;
            }
        }

        if (numCrossings == 0)
        {
            SolverUtils.SolverResult solverRes = (!(allNegative || allPositive)) ? SolverUtils.SolverResult.IllConditioned : (allPositive ? SolverUtils.SolverResult.AllPositive : SolverUtils.SolverResult.AllNegative);
            return (null, solverRes);
        }
        else if (numCrossings == 1)
        {
            try
            {
                return (new List<float>() { SolveNumeric(crv, dCrv, maxNumericIters, tolerance, crossingParam) }, SolverUtils.SolverResult.HasZeroCrossing);
            }
            catch (Exception exc)
            {
                Debug.LogWarning(string.Format("Solver error: {0}", exc));
                if (maxSubdivDepth <= 0)
                {
                    Debug.LogError(string.Format("Solver max subdivisions reached"));
                    throw;
                }
                (BezierCurve<float>, BezierCurve<float>) crvHalves = crv.Subdivide(crv.Domain.Item2 * 0.5f + crv.Domain.Item1 * 0.5f);
                (List<float>, SolverUtils.SolverResult) sol1 = SolveSubdivInner(crvHalves.Item1, crvHalves.Item1.ControlPoints.ToArray(), crvHalves.Item1.Derivative(), maxNumericIters, maxSubdivDepth - 1, tolerance);
                (List<float>, SolverUtils.SolverResult) sol2 = SolveSubdivInner(crvHalves.Item2, crvHalves.Item2.ControlPoints.ToArray(), crvHalves.Item2.Derivative(), maxNumericIters, maxSubdivDepth - 1, tolerance);
                if (sol1.Item2 == SolverUtils.SolverResult.HasZeroCrossing || sol2.Item2 == SolverUtils.SolverResult.HasZeroCrossing)
                {
                    sol1.Item2 = SolverUtils.SolverResult.HasZeroCrossing;
                }
                sol1.Item1.AddRange(sol2.Item1);
                return sol1;
            }
        }
        else
        {
            (BezierCurve<float>, BezierCurve<float>) crvHalves = crv.Subdivide(crv.Domain.Item2 * 0.5f + crv.Domain.Item1 * 0.5f);
            (List<float>, SolverUtils.SolverResult) sol1 = SolveSubdivInner(crvHalves.Item1, crvHalves.Item1.ControlPoints.ToArray(), crvHalves.Item1.Derivative(), maxNumericIters, maxSubdivDepth - 1, tolerance);
            (List<float>, SolverUtils.SolverResult) sol2 = SolveSubdivInner(crvHalves.Item2, crvHalves.Item2.ControlPoints.ToArray(), crvHalves.Item2.Derivative(), maxNumericIters, maxSubdivDepth - 1, tolerance);
            if (sol1.Item2 == SolverUtils.SolverResult.HasZeroCrossing || sol2.Item2 == SolverUtils.SolverResult.HasZeroCrossing)
            {
                sol1.Item2 = SolverUtils.SolverResult.HasZeroCrossing;
            }
            sol1.Item1.AddRange(sol2.Item1);
            return sol1;
        }
    }

    private BezierCurve<float> _crv, _dCrv;
    private float[] _crvPts;
    private readonly int _maxIters;
    private readonly int _maxSubdivDepth;
}

public static class SolverUtils
{
    public struct SplitBezier
    {
        public BezierCurve<float> X, Y, Z;
    }

    public enum SolverResult
    {
        AllNegative,
        HasZeroCrossing,
        AllPositive,
        IllConditioned
    }

    public static SplitBezier SplitCrv(BezierCurve<Vector3> crv)
    {
        List<float>
            XPts = new List<float>(crv.Order),
            YPts = new List<float>(crv.Order),
            ZPts = new List<float>(crv.Order),
            WPts = (crv is WeightedlBezierCurve<Vector3>) ? new List<float>() : null;
        WeightedlBezierCurve<Vector3> weightedCrv = crv as WeightedlBezierCurve<Vector3>;
        IEnumerator<float> weightIter = weightedCrv != null ? weightedCrv.Weights.GetEnumerator() : null;

        foreach (Vector3 crvPt in crv.ControlPoints)
        {
            XPts.Add(crvPt.x);
            YPts.Add(crvPt.y);
            ZPts.Add(crvPt.z);
            if (weightIter != null)
            {
                weightIter.MoveNext();
                WPts.Add(weightIter.Current);
            }
        }

        return new SplitBezier()
        {
            X = weightedCrv != null ? new WeightedlBezierCurve<float>(XPts, WPts, BlendFloat) : new BezierCurve<float>(XPts, BlendFloat),
            Y = weightedCrv != null ? new WeightedlBezierCurve<float>(YPts, WPts, BlendFloat) : new BezierCurve<float>(YPts, BlendFloat),
            Z = weightedCrv != null ? new WeightedlBezierCurve<float>(ZPts, WPts, BlendFloat) : new BezierCurve<float>(ZPts, BlendFloat)
        };
    }

    public static BezierCurve<float> BezierLineIntersectionXZExpr(BezierCurve<Vector3> crv, Vector3 linePt, Vector3 lineVec)
    {
        List<float>
            pts = new List<float>(crv.Order);
        WeightedlBezierCurve<Vector3> weightedCrv = crv as WeightedlBezierCurve<Vector3>;
        IEnumerator<float> weightIter = weightedCrv != null ? weightedCrv.Weights.GetEnumerator() : null;

        // Formula is: (crv_x(t) - crv_w(t) * pt_x * vec_z) / crv_w(t) - (crv_z(t) - crv_w(t) * pt_z * vec_x) / crv_w(t) = 0
        // Ignore denominator.
        foreach (Vector3 crvPt in crv.ControlPoints)
        {
            if (weightIter != null)
            {
                weightIter.MoveNext();
                float crv_w = weightIter.Current;
                pts.Add(((crvPt.x - crv_w * linePt.x) * lineVec.z) - ((crvPt.z - crv_w * linePt.z) * lineVec.x));
            }
            else
            {
                pts.Add(((crvPt.x - linePt.x) * lineVec.z) - ((crvPt.z - linePt.z) * lineVec.x));
            }
        }

        return new BezierCurve<float>(pts, BlendFloat);
    }

    public static float BlendFloat(float x, float a, float y, float b) => x * a + y * b;
}
