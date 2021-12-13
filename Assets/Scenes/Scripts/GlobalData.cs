using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

static class GlobalData
{
    public static readonly int LayersLayerMask = LayerMask.GetMask("PlaneLayers");
    public static readonly int ControlPtsLayerMask = LayerMask.GetMask("ControlPts");
    public static readonly int CurvesLayerMask = LayerMask.GetMask("Curves");
    public static readonly int GizmosLayerMask = LayerMask.GetMask("Gizmos");

    public static readonly Color DefaultCrvColor = new Color(207f, 207f, 0f);
    public static readonly Color ConnectedCrvColor = new Color(0f, 128f, 0f);
    public static readonly Color SelectedCrvColor = new Color(207f, 207f, 102f);

    public static float CurveConnectionTolerance = 1e-4f;
}
