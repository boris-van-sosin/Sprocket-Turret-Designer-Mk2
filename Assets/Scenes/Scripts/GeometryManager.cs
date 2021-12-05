using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class GeometryManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private BezierCurveGeom _currTmpBzrCurve;
    private CircularArcGeom _currTmpCircArc;

    private ControlPoint _selecedCtlPt;
    private Axis _dragAxis;
    private Vector3 _dragOrigin;
    private float _dragTime = 0f;
    private static readonly float _dragDelay = 0.5f;
}
