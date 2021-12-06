using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlaneLayer : MonoBehaviour
{
    public void AddCurve(CurveGeomBase crv)
    {
        _curves.Add(crv);
    }

    public void DeleteCurve(CurveGeomBase crv)
    {
        int idx = _curves.FindIndex(c => c == crv);
        if (idx >= 0)
        {
            Destroy(crv);
            _curves.RemoveAt(idx);
        }
    }

    public float Elevation
    {
        get
        {
            return transform.position.y;
        }
        set
        {
            transform.position = new Vector3(transform.position.z, value, transform.position.x);
        }
    }
    private List<CurveGeomBase> _curves = new List<CurveGeomBase>();
}

