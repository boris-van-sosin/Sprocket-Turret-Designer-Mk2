using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class MeshGenerator
{
    public static QuadMesh GenerateQuadMesh(List<PlaneLayer> layers, int samplesPerCurvedSeg)
    {
        ProcessedCurves processed = ProcessAndSampleCurves(layers, samplesPerCurvedSeg);

        // Generate the mesh:
        List<(int, int)> edges = new List<(int, int)>();
        QuadMesh quads = new QuadMesh()
        {
            Vertices = new List<Vector3>(processed.FrontSamplePoints.Count + processed.SideSamplePoints.Count + processed.RearSamplePoints.Count),
            Quads = new List<(int, int, int, int)>((processed.FrontSamplePoints.Count + processed.SideSamplePoints.Count + processed.RearSamplePoints.Count) * 4)
        };
        int vertexIdx = 0, currlayerBase = 0, lastLayerBase = 0;
        for (int i = 0; i < processed.NumLayers; ++i)
        {
            currlayerBase = vertexIdx;
            int crvIdx = 0, sampleIdx = 0, layerVertexIdx = 0;
            while (sampleIdx < processed.FrontSamplePoints.Count)
            {
                float t = processed.FrontSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.FrontCurves[i].Curves[crvIdx].Item1;
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
            while (sampleIdx < processed.SideSamplePoints.Count)
            {
                float t = processed.SideSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.SideCurves[i].Curves[crvIdx].Item1;
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
            while (sampleIdx < processed.RearSamplePoints.Count)
            {
                float t = processed.RearSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.RearCurves[i].Curves[crvIdx].Item1;
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

        quads.MeshSize = (quads.Vertices.Count / processed.NumLayers, processed.NumLayers);

        GeomObjectFactory.GetGeometryManager().AssignGizmos(edges.Select(e => (quads.Vertices[e.Item1], quads.Vertices[e.Item2], UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))), quads.Vertices.Select(v => (v, UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))));

        return quads;
    }

    public static HexMesh GenerateHexMesh(List<PlaneLayer> layers, int samplesPerCurvedSeg)
    {
        ProcessedCurves processed = ProcessAndSampleCurves(layers, samplesPerCurvedSeg);

        // Generate the mesh:
        List<(int, int)> edges = new List<(int, int)>();
        HexMesh hexes = new HexMesh()
        {
            BoundaryVertices = new List<Vector3>(processed.FrontSamplePoints.Count + processed.SideSamplePoints.Count + processed.RearSamplePoints.Count),
            CoreVertices = new List<Vector3>(processed.FrontSamplePoints.Count + processed.SideSamplePoints.Count + processed.RearSamplePoints.Count),
            Hexes = new List<Hex>(processed.FrontSamplePoints.Count + processed.SideSamplePoints.Count + processed.RearSamplePoints.Count)
        };
        int vertexIdx = 0, currlayerBase = 0, lastLayerBase = 0;
        float frontRearMinZ = -1f, sideMinX = -1f;
        for (int i = 0; i < processed.NumLayers; ++i)
        {
            currlayerBase = vertexIdx;
            int crvIdx = 0, sampleIdx = 0, layerVertexIdx = 0;
            while (sampleIdx < processed.FrontSamplePoints.Count)
            {
                float t = processed.FrontSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.FrontCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    Vector3 pt = currCrv.Eval(t);
                    hexes.BoundaryVertices.Add(pt);
                    if (sampleIdx == 0 || Mathf.Abs(pt.z) < frontRearMinZ) { frontRearMinZ = Mathf.Abs(pt.z); }

                    if (layerVertexIdx > 0)
                    {
                        edges.Add((vertexIdx - 1, vertexIdx));
                    }
                    if (i > 0)
                    {
                        edges.Add((lastLayerBase + layerVertexIdx, vertexIdx));
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
            while (sampleIdx < processed.SideSamplePoints.Count)
            {
                float t = processed.SideSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.SideCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    Vector3 pt = currCrv.Eval(t);
                    hexes.BoundaryVertices.Add(pt);
                    if (sampleIdx == 0 || Mathf.Abs(pt.z) < sideMinX) { sideMinX = Mathf.Abs(pt.x); }

                    if (layerVertexIdx > 0)
                    {
                        edges.Add((vertexIdx - 1, vertexIdx));
                    }
                    if (i > 0)
                    {
                        edges.Add((lastLayerBase + layerVertexIdx, vertexIdx));
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
            while (sampleIdx < processed.RearSamplePoints.Count)
            {
                float t = processed.RearSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.RearCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    Vector3 pt = currCrv.Eval(t);
                    hexes.BoundaryVertices.Add(pt);
                    if (sampleIdx == 0 || Mathf.Abs(pt.z) < frontRearMinZ) { frontRearMinZ = Mathf.Abs(pt.z); }

                    if (layerVertexIdx > 0)
                    {
                        edges.Add((vertexIdx - 1, vertexIdx));
                    }
                    if (i > 0)
                    {
                        edges.Add((lastLayerBase + layerVertexIdx, vertexIdx));
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

        float minR = new Vector2(hexes.BoundaryVertices[0].x, hexes.BoundaryVertices[0].z).sqrMagnitude;
        float minH = hexes.BoundaryVertices[0].y;
        float maxH = hexes.BoundaryVertices[0].y;
        foreach (Vector3 v in hexes.BoundaryVertices)
        {
            if (v.sqrMagnitude < minR)
            {
                minR = new Vector2(v.x, v.z).sqrMagnitude;
            }
            if (v.y < minH)
            {
                minH = v.y;
            }
            if (v.y > maxH)
            {
                maxH = v.y;
            }
        }
        minR = Mathf.Min(Mathf.Sqrt(minR), frontRearMinZ, sideMinX);
        hexes.CoreSize = new Vector3(minR * 2f / Sqrt2, maxH - minH, minR * 2f / Sqrt2);
        hexes.CoreElevation = minH + hexes.CoreSize.y * 0.5f;

        vertexIdx = 0;
        currlayerBase = 0;
        lastLayerBase = 0;
        for (int i = 0; i < processed.NumLayers; ++i)
        {
            currlayerBase = vertexIdx;
            int crvIdx = 0, sampleIdx = 0, layerVertexIdx = 0;
            while (sampleIdx < processed.FrontSamplePoints.Count)
            {
                float t = processed.FrontSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.FrontCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    float x = processed.FrontSamplePoints.Count > 1 ? ((hexes.CoreSize.x / 2f) * sampleIdx) / (processed.FrontSamplePoints.Count - 1f) : 0f;
                    float y = processed.SplitStruct.FrontCurves[i].Elevation;
                    float z = hexes.CoreSize.z * 0.5f;
                    hexes.CoreVertices.Add(new Vector3(x, y, z));

                    if (i > 0 && layerVertexIdx > 0)
                    {
                        //quads.Quads.Add((lastLayerBase + layerVertexIdx - 1, lastLayerBase + layerVertexIdx, vertexIdx, vertexIdx - 1));
                        hexes.Hexes.Add(new Hex()
                        {
                            A = hexes.BoundaryVertices[lastLayerBase + layerVertexIdx - 1],
                            B = hexes.BoundaryVertices[lastLayerBase + layerVertexIdx],
                            C = hexes.BoundaryVertices[vertexIdx],
                            D = hexes.BoundaryVertices[vertexIdx - 1],
                            E = hexes.CoreVertices[lastLayerBase + layerVertexIdx - 1],
                            F = hexes.CoreVertices[lastLayerBase + layerVertexIdx],
                            G = hexes.CoreVertices[vertexIdx],
                            H = hexes.CoreVertices[vertexIdx - 1]
                        });
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
            while (sampleIdx < processed.SideSamplePoints.Count)
            {
                float t = processed.SideSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.SideCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    float x = hexes.CoreSize.x / 2f;
                    float y = processed.SplitStruct.FrontCurves[i].Elevation;
                    float z = -hexes.CoreSize.z * (sampleIdx / (processed.SideSamplePoints.Count - 1f)) + (hexes.CoreSize.z * 0.5f);
                    hexes.CoreVertices.Add(new Vector3(x, y, z));

                    if (i > 0 && layerVertexIdx > 0)
                    {
                        //quads.Quads.Add((lastLayerBase + layerVertexIdx - 1, lastLayerBase + layerVertexIdx, vertexIdx, vertexIdx - 1));
                        hexes.Hexes.Add(new Hex()
                        {
                            A = hexes.BoundaryVertices[lastLayerBase + layerVertexIdx - 1],
                            B = hexes.BoundaryVertices[lastLayerBase + layerVertexIdx],
                            C = hexes.BoundaryVertices[vertexIdx],
                            D = hexes.BoundaryVertices[vertexIdx - 1],
                            E = hexes.CoreVertices[lastLayerBase + layerVertexIdx - 1],
                            F = hexes.CoreVertices[lastLayerBase + layerVertexIdx],
                            G = hexes.CoreVertices[vertexIdx],
                            H = hexes.CoreVertices[vertexIdx - 1]
                        });
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
            while (sampleIdx < processed.RearSamplePoints.Count)
            {
                float t = processed.RearSamplePoints[sampleIdx];
                ICurve<Vector3> currCrv = processed.SplitStruct.RearCurves[i].Curves[crvIdx].Item1;
                if (currCrv.Domain.Item1 <= t && t <= currCrv.Domain.Item2)
                {
                    float x = processed.RearSamplePoints.Count > 1 ? ((hexes.CoreSize.x / 2f) - ((hexes.CoreSize.x / 2f) * sampleIdx) / (processed.RearSamplePoints.Count - 1f)) : 0f;
                    float y = processed.SplitStruct.FrontCurves[i].Elevation;
                    float z = -hexes.CoreSize.z * 0.5f;
                    hexes.CoreVertices.Add(new Vector3(x, y, z));

                    if (i > 0 && layerVertexIdx > 0)
                    {
                        //quads.Quads.Add((lastLayerBase + layerVertexIdx - 1, lastLayerBase + layerVertexIdx, vertexIdx, vertexIdx - 1));
                        hexes.Hexes.Add(new Hex()
                        {
                            A = hexes.BoundaryVertices[lastLayerBase + layerVertexIdx - 1],
                            B = hexes.BoundaryVertices[lastLayerBase + layerVertexIdx],
                            C = hexes.BoundaryVertices[vertexIdx],
                            D = hexes.BoundaryVertices[vertexIdx - 1],
                            E = hexes.CoreVertices[lastLayerBase + layerVertexIdx - 1],
                            F = hexes.CoreVertices[lastLayerBase + layerVertexIdx],
                            G = hexes.CoreVertices[vertexIdx],
                            H = hexes.CoreVertices[vertexIdx - 1]
                        });
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
        int numVertices = hexes.BoundaryVertices.Count;
        Vector3[] mirroredBndryVertices = hexes.BoundaryVertices.Select(v => new Vector3(-v.x, v.y, v.z)).ToArray();
        Vector3[] mirroredCoreVertices = hexes.CoreVertices.Select(v => new Vector3(-v.x, v.y, v.z)).ToArray();
        Hex[] mirroredHexes = hexes.Hexes.Select(
            h => new Hex()
            {
                A = new Vector3(-h.D.x, h.D.y, h.D.z),
                B = new Vector3(-h.C.x, h.C.y, h.C.z),
                C = new Vector3(-h.B.x, h.B.y, h.B.z),
                D = new Vector3(-h.A.x, h.A.y, h.A.z),
                E = new Vector3(-h.H.x, h.H.y, h.H.z),
                F = new Vector3(-h.G.x, h.G.y, h.G.z),
                G = new Vector3(-h.F.x, h.F.y, h.F.z),
                H = new Vector3(-h.E.x, h.E.y, h.E.z)
            }
            ).ToArray();
        (int, int)[] mirroredEdges = edges.Select(e => (e.Item2 + numVertices, e.Item1 + numVertices)).ToArray();
        hexes.BoundaryVertices.AddRange(mirroredBndryVertices);
        hexes.CoreVertices.AddRange(mirroredCoreVertices);
        hexes.Hexes.AddRange(mirroredHexes);
        edges.AddRange(mirroredEdges);

        GeomObjectFactory.GetGeometryManager().AssignGizmos(edges.Select(e => (hexes.BoundaryVertices[e.Item1], hexes.BoundaryVertices[e.Item2], UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))), hexes.BoundaryVertices.Select(v => (v, UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))));

        return hexes;
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
                    if (!((!includeStart && i == 0 && j == 0) || (!includeEnd && i == sec.Curves.Count - 1 && j == sec.Curves[i].Item2 - 1)))
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

    private static ProcessedCurves ProcessAndSampleCurves(List<PlaneLayer> layers, int samplesPerCurvedSeg)
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
                    BezierCurve<Vector3> currCrv = (BezierCurve<Vector3>)bzrCrv.GetCurve();
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
        int layerIdx = 0;
        foreach (RawCurveChain currChain in rawChains)
        {
            splitStruct.FrontCurves.Add(new SectionCurves(Section.Front, layerIdx, currChain.Elevation));
            splitStruct.SideCurves.Add(new SectionCurves(Section.Side, layerIdx, currChain.Elevation));
            splitStruct.RearCurves.Add(new SectionCurves(Section.Rear, layerIdx, currChain.Elevation));
            for (int i = 0; i < currChain.Curves.Count; ++i)
            {
                AssignCurve(currChain.Curves[i], currChain.Elevation, samplesPerCurvedSeg, splitStruct.FrontCurves[layerIdx].Curves, splitStruct.SideCurves[layerIdx].Curves, splitStruct.RearCurves[layerIdx].Curves);
            }

            ++layerIdx;
        }

        List<float> frontSamplePts = ReparamSectionCurves(splitStruct.FrontCurves, true, false);
        List<float> sideSamplePts = ReparamSectionCurves(splitStruct.SideCurves, true, true);
        List<float> rearSamplePts = ReparamSectionCurves(splitStruct.RearCurves, false, true);

        return new ProcessedCurves() { NumLayers = rawChains.Count, SplitStruct = splitStruct, FrontSamplePoints = frontSamplePts, SideSamplePoints = sideSamplePts, RearSamplePoints = rearSamplePts };
    }

    public static Mesh AssignToMesh(QuadMesh quads)
    {
        Mesh m = new Mesh();
        m.Clear();
        m.SetVertices(quads.Vertices);
        List<int> triangles = new List<int>(quads.Quads.Count * 6);

        foreach (var q in quads.Quads)
        {
            triangles.Add(q.Item1);
            triangles.Add(q.Item2);
            triangles.Add(q.Item3);

            triangles.Add(q.Item1);
            triangles.Add(q.Item3);
            triangles.Add(q.Item4);
        }
        m.SetTriangles(triangles, 0, true);

        Vector2[] uvs = new Vector2[quads.Vertices.Count];
        int idx = 0;
        for (int i = 0; i < quads.MeshSize.Item1; ++i)
        {
            for (int j = 0; j < quads.MeshSize.Item2; ++j)
            {
                uvs[idx] = new Vector2(((float)i) / quads.MeshSize.Item1, ((float)j) / quads.MeshSize.Item2);
            }
        }
        m.uv = uvs;

        m.UploadMeshData(true);

        return m;
    }

    public static Mesh AssignToMesh(HexMesh hexes, float explode)
    {
        Mesh m = new Mesh();
        m.Clear();

        List<Vector3> vertices = new List<Vector3>(hexes.Hexes.Count * 8);
        List<int> triagnles = new List<int>(hexes.Hexes.Count * 6 * 2 * 3);
        List<Vector2> uvs = new List<Vector2>(hexes.Hexes.Count * 8);

        int idx = 0;
        foreach (Hex h in hexes.Hexes)
        {
            if (explode > 1e-5f)
            {
                Vector3 explodeVec = ((h.A + h.B + h.C + h.D + h.E + h.F + h.G + h.H) / 8f).normalized * explode;
                vertices.Add(h.A + explodeVec);
                vertices.Add(h.B + explodeVec);
                vertices.Add(h.C + explodeVec);
                vertices.Add(h.D + explodeVec);
                vertices.Add(h.E + explodeVec);
                vertices.Add(h.F + explodeVec);
                vertices.Add(h.G + explodeVec);
                vertices.Add(h.H + explodeVec);
            }
            else
            {
                vertices.Add(h.A);
                vertices.Add(h.B);
                vertices.Add(h.C);
                vertices.Add(h.D);
                vertices.Add(h.E);
                vertices.Add(h.F);
                vertices.Add(h.G);
                vertices.Add(h.H);
            }

            // Front quad:
            triagnles.Add(idx + 0);
            triagnles.Add(idx + 1);
            triagnles.Add(idx + 2);

            triagnles.Add(idx + 0);
            triagnles.Add(idx + 2);
            triagnles.Add(idx + 3);

            // Side quad 1:
            triagnles.Add(idx + 0);
            triagnles.Add(idx + 3);
            triagnles.Add(idx + 7);

            triagnles.Add(idx + 4);
            triagnles.Add(idx + 0);
            triagnles.Add(idx + 7);

            // Side quad 2:
            triagnles.Add(idx + 1);
            triagnles.Add(idx + 5);
            triagnles.Add(idx + 2);

            triagnles.Add(idx + 2);
            triagnles.Add(idx + 5);
            triagnles.Add(idx + 6);

            // Top quad:
            triagnles.Add(idx + 3);
            triagnles.Add(idx + 2);
            triagnles.Add(idx + 6);

            triagnles.Add(idx + 3);
            triagnles.Add(idx + 6);
            triagnles.Add(idx + 7);

            // Bottom quad:
            triagnles.Add(idx + 1);
            triagnles.Add(idx + 0);
            triagnles.Add(idx + 5);

            triagnles.Add(idx + 0);
            triagnles.Add(idx + 4);
            triagnles.Add(idx + 5);

            // Rear quad:
            triagnles.Add(idx + 6);
            triagnles.Add(idx + 5);
            triagnles.Add(idx + 4);

            triagnles.Add(idx + 7);
            triagnles.Add(idx + 6);
            triagnles.Add(idx + 4);

            for (int i = 0; i < 8; ++i)
            {
                uvs.Add(new Vector2((i / 3)  / 3f, (i % 3) / 3f));
            }

            idx += 8;
        }

        m.vertices = vertices.ToArray();
        m.triangles = triagnles.ToArray();

        m.UploadMeshData(true);

        GeomObjectFactory.GetGeometryManager().AssignGizmos(null, vertices.Select(v => (v, UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f))));

        return m;
    }

    public class QuadMesh
    {
        public (int, int) MeshSize { get; set; }
        public List<Vector3> Vertices { get; set; }
        public List<ValueTuple<int, int, int, int>> Quads { get; set; }
    }

    public struct Hex
    {
        public Vector3 A, B, C, D, E, F, G, H;
    }

    public class HexMesh
    {
        public List<Vector3> BoundaryVertices { get; set; }
        public List<Vector3> CoreVertices { get; set; }
        public Vector3 CoreSize { get; set; }
        public float CoreElevation { get; set; }
        public List<Hex> Hexes { get; set; }
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

    private struct ProcessedCurves
    {
        public int NumLayers { get; set; }
        public SplitStructure SplitStruct { get; set; }
        public List<float> FrontSamplePoints { get; set; }
        public List<float> SideSamplePoints { get; set; }
        public List<float> RearSamplePoints { get; set; }
    }

    private static readonly LayerComparer _comp = new LayerComparer();
    private static readonly FloatEpsComparer _floatEpsComp = new FloatEpsComparer(1e-5f);
    private static readonly float Sqrt2 = Mathf.Sqrt(2);
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