using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class MeshGenerator
{
    public static QuadMesh GenerateMesh(List<PlaneLayer> layers)
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

        /*
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
        */

        SplitStructure splitStruct = new SplitStructure() { FrontCurves = new List<SectionCurves>(), SideCurves = new List<SectionCurves>(), RearCurves = new List<SectionCurves>() };
        int samplesPerCurvedSection = 5;
        int layerIdx = 0;
        foreach (RawCurveChain currChain in rawChains)
        {
            splitStruct.FrontCurves.Add(new SectionCurves(Section.Front, layerIdx, currChain.Elevation));
            splitStruct.SideCurves.Add(new SectionCurves(Section.Side, layerIdx, currChain.Elevation));
            splitStruct.RearCurves.Add(new SectionCurves(Section.Rear, layerIdx, currChain.Elevation));
            for (int i = 0; i < currChain.Curves.Count; ++i)
            {
                AssignCurve(currChain.Curves[i], currChain.Elevation, samplesPerCurvedSection, splitStruct.FrontCurves[layerIdx].Curves, splitStruct.SideCurves[layerIdx].Curves, splitStruct.RearCurves[layerIdx].Curves);
            }

            ++layerIdx;
        }

        List<float> frontSamplePts = ReparamSectionCurves(splitStruct.FrontCurves, true, false);
        List<float> sideSamplePts = ReparamSectionCurves(splitStruct.SideCurves, true, true);
        List<float> rearSamplePts = ReparamSectionCurves(splitStruct.RearCurves, false, true);

        // Generate the mesh:
        List<(int, int)> edges = new List<(int, int)>();
        QuadMesh quads = new QuadMesh() { Vertices = new List<Vector3>(), Quads = new List<(int, int, int, int)>() };
        int vertexIdx = 0, currlayerBase = 0, lastLayerBase = 0;
        for (int i = 0; i < rawChains.Count; ++i)
        {
            currlayerBase = vertexIdx;
            int crvIdx = 0, sampleIdx = 0, layerVertexIdx = 0;
            while (sampleIdx < frontSamplePts.Count)
            {
                float t = frontSamplePts[sampleIdx];
                ICurve<Vector3> currCrv = splitStruct.FrontCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    quads.Vertices.Add(currCrv.Eval(t));

                    if (layerVertexIdx > 0)
                    {
                        edges.Add((vertexIdx - 1, vertexIdx));
                    }
                    if (i > 0)
                    {
                        edges.Add((lastLayerBase + layerVertexIdx, vertexIdx));
                        if (layerVertexIdx > 0)
                        {
                            quads.Quads.Add((lastLayerBase + layerVertexIdx - 1, lastLayerBase + layerVertexIdx, vertexIdx, vertexIdx - 1));
                        }
                    }

                    ++layerVertexIdx;
                    ++vertexIdx;

                    ++sampleIdx;
                }
                else if (t > currCrv.Domain.Item2)
                {
                    ++crvIdx;
                }
                else
                {
                    throw new Exception("Sample out of range of curve");
                }
            }

            crvIdx = 0;
            sampleIdx = 0;
            while (sampleIdx < sideSamplePts.Count)
            {
                float t = sideSamplePts[sampleIdx];
                ICurve<Vector3> currCrv = splitStruct.SideCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    quads.Vertices.Add(currCrv.Eval(t));

                    if (layerVertexIdx > 0)
                    {
                        edges.Add((vertexIdx - 1, vertexIdx));
                    }
                    if (i > 0)
                    {
                        edges.Add((lastLayerBase + layerVertexIdx, vertexIdx));
                        if (layerVertexIdx > 0)
                        {
                            quads.Quads.Add((lastLayerBase + layerVertexIdx - 1, lastLayerBase + layerVertexIdx, vertexIdx, vertexIdx - 1));
                        }
                    }

                    ++layerVertexIdx;
                    ++vertexIdx;

                    ++sampleIdx;
                }
                else if (t > currCrv.Domain.Item2)
                {
                    ++crvIdx;
                }
                else
                {
                    throw new Exception("Sample out of range of curve");
                }
            }

            crvIdx = 0;
            sampleIdx = 0;
            while (sampleIdx < rearSamplePts.Count)
            {
                float t = rearSamplePts[sampleIdx];
                ICurve<Vector3> currCrv = splitStruct.RearCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    quads.Vertices.Add(currCrv.Eval(t));

                    if (layerVertexIdx > 0)
                    {
                        edges.Add((vertexIdx - 1, vertexIdx));
                    }
                    if (i > 0)
                    {
                        edges.Add((lastLayerBase + layerVertexIdx, vertexIdx));
                        if (layerVertexIdx > 0)
                        {
                            quads.Quads.Add((lastLayerBase + layerVertexIdx - 1, lastLayerBase + layerVertexIdx, vertexIdx, vertexIdx - 1));
                        }
                    }

                    ++layerVertexIdx;
                    ++vertexIdx;

                    ++sampleIdx;
                }
                else if (t > currCrv.Domain.Item2)
                {
                    ++crvIdx;
                }
                else
                {
                    throw new Exception("Sample out of range of curve");
                }
            }

            lastLayerBase = currlayerBase;
        }

        // Mirror the mesh:
        int numVertices = quads.Vertices.Count;
        Vector3[] mirroredVertices = quads.Vertices.Select(v => new Vector3(-v.x, v.y, v.z)).ToArray();
        (int, int, int, int)[] mirroredQuads = quads.Quads.Select(q => (q.Item4 + numVertices, q.Item3 + numVertices, q.Item2 + numVertices, q.Item1 + numVertices)).ToArray();
        (int, int)[] mirroredEdges = edges.Select(e => (e.Item2 + numVertices, e.Item1 + numVertices)).ToArray();
        quads.Vertices.AddRange(mirroredVertices);
        quads.Quads.AddRange(mirroredQuads);
        edges.AddRange(mirroredEdges);

        GeomObjectFactory.GetGeometryManager().AssignGizmos(edges.Select(e => (quads.Vertices[e.Item1], quads.Vertices[e.Item2], UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))), quads.Vertices.Select(v => (v, UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))));

        return quads;
    }

    private static void AssignCurve(BezierCurve<Vector3> crv, float layerElevation, int samplesPerCurvedSection, List<(ICurve<Vector3>, int)> front, List<(ICurve<Vector3>, int)> side, List<(ICurve<Vector3>, int)> rear)
    {
        BezierCurve<float> f1 = SolverUtils.BezierLineIntersectionXZExpr(crv, new Vector3(0f, layerElevation, 0f), new Vector3(1f, 0f, 1f).normalized);
        ZeroSolver zr1 = new ZeroSolver(f1);
        (List<float>, SolverUtils.SolverResult) frontSideRes = zr1.SolveSubdiv(1e-6f);

        int numSamples = crv.Order == 2 ? 2 : samplesPerCurvedSection;

        switch (frontSideRes.Item2)
        {
            case SolverUtils.SolverResult.AllNegative:
                {
                    front.Add((crv, numSamples));
                    return;
                }
            case SolverUtils.SolverResult.HasZeroCrossing:
                {
                    Vector3 pt1 = crv.Eval(frontSideRes.Item1[0]);
                    Vector3 pt2 = new Vector3(pt1.x * 1.1f, pt1.y, pt1.z * 1.1f);
                    //Debug.DrawLine(pt1, pt2, Color.red, 10f);
                    (BezierCurve<Vector3>, BezierCurve<Vector3>) splitCrvs = crv.Subdivide(frontSideRes.Item1[0]);
                    AssignCurve(splitCrvs.Item1, layerElevation, samplesPerCurvedSection, front, side, rear);
                    AssignCurve(splitCrvs.Item2, layerElevation, samplesPerCurvedSection, front, side, rear);
                    return;
                }
            default:
                break;
        }

        BezierCurve<float> f2 = SolverUtils.BezierLineIntersectionXZExpr(crv, new Vector3(0f, layerElevation, 0f), new Vector3(1f, 0f, -1f).normalized);
        ZeroSolver zr2 = new ZeroSolver(f2);
        (List<float>, SolverUtils.SolverResult) sideRearRes = zr2.SolveSubdiv(1e-6f);

        switch (sideRearRes.Item2)
        {
            case SolverUtils.SolverResult.AllPositive:
                {
                    rear.Add((crv, numSamples));
                    return;
                }
            case SolverUtils.SolverResult.AllNegative:
                {
                    side.Add((crv, numSamples));
                    return;
                }
            case SolverUtils.SolverResult.HasZeroCrossing:
                {
                    Vector3 pt1 = crv.Eval(sideRearRes.Item1[0]);
                    Vector3 pt2 = new Vector3(pt1.x * 1.1f, pt1.y, pt1.z * 1.1f);
                    //Debug.DrawLine(pt1, pt2, Color.red, 10f);
                    (BezierCurve<Vector3>, BezierCurve<Vector3>) splitCrvs = crv.Subdivide(sideRearRes.Item1[0]);
                    AssignCurve(splitCrvs.Item1, layerElevation, samplesPerCurvedSection, front, side, rear);
                    AssignCurve(splitCrvs.Item2, layerElevation, samplesPerCurvedSection, front, side, rear);
                    return;
                }
            default:
                break;
        }

        throw new Exception("Failed to assign curve to either front, side, or rear sections.");
    }

    private static List<float> ReparamSectionCurves(List<SectionCurves> secCrvs, bool includeStart, bool includeEnd)
    {
        SortedSet<float> samplePts = new SortedSet<float>(_floatEpsComp);

        int maxNumCrvs = 0;
        foreach (SectionCurves sec in secCrvs)
        {
            if (sec.Curves.Count > maxNumCrvs)
            {
                maxNumCrvs = sec.Curves.Count;
            }
        }

        // Reparameterize the curves and add the sample points:
        foreach (SectionCurves sec in secCrvs)
        {
            float totalDomain = (float) maxNumCrvs,
                segDomainSize = totalDomain / sec.Curves.Count,
                currTMin = 0f,
                currTMax = segDomainSize;

            for (int i = 0; i < sec.Curves.Count; ++i)
            {
                sec.Curves[i] = (new CurveAffineReparameterization<Vector3>(sec.Curves[i].Item1, currTMin, currTMax), sec.Curves[i].Item2);

                float t = currTMin;
                for (int j = 0; j < sec.Curves[i].Item2; ++j)
                {
                    if ((includeStart && i == 0 && j == 0) ||
                        (includeEnd && i == sec.Curves.Count - 1 && j == sec.Curves[i].Item2 - 1) ||
                        ((i > 0 && i < sec.Curves.Count - 1) || (j > 0 && j < sec.Curves[i].Item2 - 1)))
                    {
                        samplePts.Add(t);
                    }

                    t += segDomainSize / (sec.Curves[i].Item2 - 1);
                }

                currTMin = currTMax;
                currTMax += segDomainSize;
            }
        }

        return new List<float>(samplePts);
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

    private class FloatEpsComparer : IComparer<float>
    {
        public FloatEpsComparer(float eps)
        {
            _eps = eps;
        }

        private readonly float _eps;

        public int Compare(float x, float y)
        {
            if (Mathf.Abs(x - y) < _eps)
            {
                return 0;
            }
            return y > x ? -1 : 1;
        }
    }

    private struct RawCurveChain
    {
        public float Elevation { get; set; }
        public List<BezierCurve<Vector3>> Curves { get; set; }
    }

    private struct SectionCurves
    {
        public SectionCurves(Section sec, int layer, float elevation)
        {
            ContainingSection = sec;
            LayerIndex = layer;
            Elevation = elevation;
            Curves = new List<(ICurve<Vector3>, int)>();
        }

        public Section ContainingSection { get; set; }
        public int LayerIndex { get; set; }
        public float Elevation { get; set; }
        public List<(ICurve<Vector3>, int)> Curves { get; set; }
    }

    private struct SplitStructure
    {
        public List<SectionCurves> FrontCurves { get; set; }
        public List<SectionCurves> SideCurves { get; set; }
        public List<SectionCurves> RearCurves { get; set; }
    }

    private enum Section
    {
        Front, Side, Rear
    }

    private static readonly LayerComparer _comp = new LayerComparer();
    private static readonly FloatEpsComparer _floatEpsComp = new FloatEpsComparer(1e-5f);
}

public class CurveAffineReparameterization<T> : ICurve<T>
{
    public CurveAffineReparameterization(ICurve<T> innerCrv, float tMin, float tMax)
    {
        _innerCurve = innerCrv;
        _tMin = tMin;
        _tMax = tMax;
    }

    public (float, float) Domain => (_tMin, _tMax);

    public IEnumerable<T> ControlPoints => _innerCurve.ControlPoints;

    public T Eval(float t)
    {
        float t01 = Mathf.InverseLerp(_tMin, _tMax, t);
        return _innerCurve.Eval(Mathf.Lerp(_innerCurve.Domain.Item1, _innerCurve.Domain.Item2, t01));
    }

    public void UpdateControlPoint(int idx, T newPt)
    {
        _innerCurve.UpdateControlPoint(idx, newPt);
    }

    private ICurve<T> _innerCurve;
    private float _tMin, _tMax;
}
