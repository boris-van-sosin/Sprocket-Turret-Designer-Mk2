using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        _origCenterPt = _centerPt = Vector3.zero;
        _cameraVec = UserCamera.transform.position - _centerPt;
        _origCameraDist = _cameraDist = _cameraVec.magnitude;
        _origCameraVec = _cameraVec = _cameraVec.normalized;
        _ratio = _cameraDist / UserCamera.pixelWidth;
        _viewCenter = new Vector3(UserCamera.pixelRect.xMin + UserCamera.pixelRect.xMax,
                                  0f,
                                  UserCamera.pixelRect.yMin + UserCamera.pixelRect.yMax);
        UserCamera.transform.rotation = Quaternion.LookRotation(-_cameraVec, Vector3.Cross(-_cameraVec, UserCamera.transform.right));
        ScrollSensitivity = 0.2f;
        CameraSensitivity = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            StartMove(SwapYZ(Input.mousePosition));
            //if (Physics.Raycast(ray, out hit, 2000))
            //{
            //    StartMove(hit.point);
            //}
        }
        else if (Input.GetMouseButton(1) &&
                 (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            Move(SwapYZ(Input.mousePosition));
            UpdateCamera();
        }
        else if (Input.GetMouseButtonDown(1) &&
                 (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            StartOrbit(SwapYZ(Input.mousePosition));
        }
        else if (Input.GetMouseButton(1) &&
                 (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            Orbit(SwapYZ(Input.mousePosition));
            UpdateCamera();
        }
        else if (Input.mouseScrollDelta.y != 0.0f)
        {
            Zoom(-Input.mouseScrollDelta.y);
            UpdateCamera();
        }
    }

    private void StartMove(Vector3 origin)
    {
        _prevCenterPt = _centerPt;
        _mouseStartPt = origin;
    }

    private void Move(Vector3 moveTarget)
    {
        Vector3 moveVec = (moveTarget - _mouseStartPt) * _ratio * CameraSensitivity;
        Tuple<Vector3, Vector3> forwardUp = RectifiedCameraForwardUp();
        Vector3 rectifiedRight = UserCamera.transform.right;
        Vector3 rectifiedMoveVec = moveVec.x * rectifiedRight + moveVec.z * forwardUp.Item2;
        _centerPt = _prevCenterPt - rectifiedMoveVec;
    }

    private void StartOrbit(Vector3 origin)
    {
        _prevCameraVec = _cameraVec;
        _mouseStartPt = origin;
    }

    private void Orbit(Vector3 moveTarget)
    {
        Vector3 startHVec = Vector3.Project(moveTarget, Vector3.right) - _viewCenter;
        Vector3 endHVec = Vector3.Project(_mouseStartPt, Vector3.right) - _viewCenter;
        Vector3 startVVec = Vector3.Project(moveTarget, Vector3.forward) - _viewCenter;
        Vector3 endVVec = Vector3.Project(_mouseStartPt, Vector3.forward) - _viewCenter;
        float hAngle = Vector3.SignedAngle(startHVec, endHVec, Vector3.up) * CameraSensitivity;
        float vAngle = Vector3.SignedAngle(startVVec, endVVec, Vector3.up) * CameraSensitivity;
        _cameraVec = Quaternion.AngleAxis(hAngle, Vector3.up) * Quaternion.AngleAxis(vAngle, UserCamera.transform.right) * _prevCameraVec;
    }

    private void Zoom(float amount)
    {
        _cameraDist = Mathf.Max(_cameraDist + ScrollSensitivity * amount, 0f);
    }

    private void UpdateCamera()
    {
        UserCamera.transform.position = _centerPt + _cameraVec * _cameraDist;
        UserCamera.transform.rotation = Quaternion.LookRotation(-_cameraVec);
    }

    private static Vector3 SwapYZ(Vector3 v)
    {
        return new Vector3(v.x, v.z, v.y);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(_centerPt, 0.1f);
        Gizmos.DrawLine(_centerPt, UserCamera.transform.position);
        Gizmos.DrawLine(_centerPt, _centerPt + Vector3.up * (UserCamera.transform.position.y - _centerPt.y));

        Tuple<Vector3, Vector3> forwardUp = RectifiedCameraForwardUp();
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(UserCamera.transform.position, UserCamera.transform.position + 0.5f * forwardUp.Item2);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(UserCamera.transform.position, UserCamera.transform.position + 0.5f * forwardUp.Item1);
    }

    private Tuple<Vector3, Vector3> RectifiedCameraForwardUp()
    {
        Vector3 rectifiedUp = Vector3.ProjectOnPlane(UserCamera.transform.up, Vector3.up).normalized;
        Vector3 rectifiedForward = Vector3.Cross(rectifiedUp, UserCamera.transform.right);
        return new Tuple<Vector3, Vector3>(rectifiedForward, rectifiedUp);
    }

    public void ResetCamera()
    {
        _centerPt = _origCenterPt;
        _cameraVec = _origCameraVec;
        _cameraDist = _origCameraDist;
        UpdateCamera();
    }

    public Camera UserCamera;
    private Vector3 _viewCenter;
    private Vector3 _prevCenterPt;
    private Vector3 _centerPt;
    private Vector3 _prevCameraVec;
    private Vector3 _cameraVec;
    private float _cameraDist;
    private float _ratio;
    private Vector3 _mouseStartPt;
    public float ScrollSensitivity { get; set; }
    public float CameraSensitivity { get; set; }

    private Vector3 _origCenterPt;
    private Vector3 _origCameraVec;
    private float _origCameraDist;
}
