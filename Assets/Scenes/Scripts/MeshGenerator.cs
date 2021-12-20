using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class MeshGenerator
{
    public static void GenerateMesh(List<PlaneLayer> layers)
    {
        int maxOrder = 0;
        List<RawCurveChain> rawChains = new List<RawCurveChain>();
        foreach (PlaneLayer l in layers.OrderBy(t => t.Elevation))
        {
            List<ValueTuple<CurveGeomBase, bool>> crvs = l.GetConnectedChain();
            List<BezierCurve<Vector3>> currChain = new List<BezierCurve<Vector3>>(crvs.Count);
            foreach (ValueTuple<CurveGeomBase, bool> crv in crvs)
            {
                BezierCurveGeom bzrCrv;
                if ((bzrCrv = crv.Item1 as BezierCurveGeom) != null)
                {
                    BezierCurve<Vector3> currCrv = (BezierCurve<Vector3>) bzrCrv.GetCurve();
                    if (crv.Item2)
                    {
                        currCrv = new BezierCurve<Vector3>(currCrv.ControlPoints.Reverse(), CurveGeomBase.Blend);
                    }

                    if (currCrv.Order > maxOrder)
                    {
                        maxOrder = currCrv.Order;
                    }

                    currChain.Add(currCrv);
                    continue;
                }

                CircularArcGeom circCrv;
                if ((circCrv = crv.Item1 as CircularArcGeom) != null)
                {
                    //TODO: implement this
                }
            }

            rawChains.Add(new RawCurveChain() { Curves = currChain, Elevation = l.Elevation });
        }

        foreach (RawCurveChain currChain in rawChains)
        {
            for (int i = 0; i < currChain.Curves.Count; ++i)
            {
                while (currChain.Curves[i].Order < maxOrder)
                {
                    currChain.Curves[i] = currChain.Curves[i].RaiseDegree();
                }
            }
        }

        foreach (RawCurveChain currChain in rawChains)
        {
            for (int i = 0; i < currChain.Curves.Count; ++i)
            {
                BezierCurve<float> f1 = SolverUtils.BezierLineIntersectionXZExpr(currChain.Curves[i], new Vector3(0f, currChain.Elevation, 0f), new Vector3(1f, 0f, 1f).normalized);
                BezierCurve<float> f2 = SolverUtils.BezierLineIntersectionXZExpr(currChain.Curves[i], new Vector3(0f, currChain.Elevation, 0f), new Vector3(1f, 0f, -1f).normalized);
                ZeroSolver zr1 = new ZeroSolver(f1);
                (List<float>, SolverUtils.SolverResult) frontSideRes = zr1.SolveSubdiv(1e-6f);

                switch (frontSideRes.Item2)
                {
                    case SolverUtils.SolverResult.AllNegative:
                        break;
                    case SolverUtils.SolverResult.AllPositive:
                        break;
                    case SolverUtils.SolverResult.HasZeroCrossing:
                        {
                            Vector3 pt1 = currChain.Curves[i].Eval(frontSideRes.Item1[0]);
                            Vector3 pt2 = new Vector3(pt1.x * 1.1f, pt1.y, pt1.z * 1.1f);
                            Debug.DrawLine(pt1, pt2, Color.red, 10f);
                        }
                        break;
                    default:
                        break;
                }


                ZeroSolver zr2 = new ZeroSolver(f2);
                (List<float>, SolverUtils.SolverResult) sideRearRes = zr2.SolveSubdiv(1e-6f);

                switch (sideRearRes.Item2)
                {
                    case SolverUtils.SolverResult.AllNegative:
                        break;
                    case SolverUtils.SolverResult.AllPositive:
                        break;
                    case SolverUtils.SolverResult.HasZeroCrossing:
                        {
                            Vector3 pt1 = currChain.Curves[i].Eval(sideRearRes.Item1[0]);
                            Vector3 pt2 = new Vector3(pt1.x * 1.1f, pt1.y, pt1.z * 1.1f);
                            Debug.DrawLine(pt1, pt2, Color.blue, 10f);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public class QuadMesh
    {
        public List<Vector3> Vertices { get; set; }
        public List<ValueTuple<int, int, int, int>> Quads { get; set; }
    }

    private class LayerComparer : IComparer<PlaneLayer>
    {
        public int Compare(PlaneLayer x, PlaneLayer y)
        {
            if (Mathf.Abs(x.Elevation - y.Elevation) < Mathf.Epsilon)
            {
                return 0;
            }
            return y.Elevation > x.Elevation ? 1 : -1;
        }
    }

    private struct RawCurveChain
    {
        public float Elevation { get; set; }
        public List<BezierCurve<Vector3>> Curves { get; set; }
    }

    private struct SplitCurveChain
    {
        public float Elevation { get; set; }
        public List<BezierCurve<Vector3>> CurvesFront { get; set; }
        public List<BezierCurve<Vector3>> CurvesSide { get; set; }
        public List<BezierCurve<Vector3>> CurvesRear { get; set; }
    }

    private static readonly LayerComparer _comp = new LayerComparer();
}
