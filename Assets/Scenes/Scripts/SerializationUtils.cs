using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class SerializationUtils
{
    public static string Serialize(IEnumerable<LayerPlane> layers)
    {
        StructureDef structure = new StructureDef()
        {
            Layers = layers.Select(l => PrepLayer(l)).ToArray()
        };
        return JsonUtility.ToJson(structure, true);
    }

    public static StructureDef LoadFromJson(string jsonData)
    {
        return JsonUtility.FromJson<StructureDef>(jsonData);
    }

    private static CurveDef PrepCurve(CurveGeomBase crv)
    {
        if (crv is BezierCurveGeom)
        {
            return new CurveDef()
            {
                CurveType = SplineCurveString,
                Points = crv.CtlPts.Select(pt => pt.position).ToArray()
            };
        }
        else if (crv is CircularArcGeom)
        {
            return new CurveDef()
            {
                CurveType = CircuarArcString,
                Points = crv.CtlPts.Select(pt => pt.position).ToArray()
            };
        }
        throw new NotSupportedException("Only Bezier and circular curves are supported.");
    }

    private static LayerDef PrepLayer(LayerPlane layer)
    {
        return new LayerDef()
        {
            Elevation = layer.Elevation,
            Curves = layer.Curves.Select(c => PrepCurve(c)).ToArray()
        };
    }

    public static readonly string SplineCurveString = "Spline";
    public static readonly string CircuarArcString = "CircularArc";
}

[System.Serializable]
public class CurveDef
{
    public string CurveType;
    public Vector3[] Points;
}

[System.Serializable]
public class LayerDef
{
    public float Elevation;
    public CurveDef[] Curves;
}

[System.Serializable]
public class StructureDef
{
    public LayerDef[] Layers;
}
