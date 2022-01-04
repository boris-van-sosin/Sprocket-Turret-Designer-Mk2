using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    private enum CameraMode
    {
        Free,
        TopDown
    }
    // Start is called before the first frame update
    void Start()
    {
        _origCenterPt = _centerPt = Vector3.zero;
        _cameraVec = UserCamera.transform.position - _centerPt;
        _origCameraDist = _cameraDist = _cameraVec.magnitude;
        _origCameraVec = _cameraVec = _cameraVec.normalized;
        _origCameraRight = UserCamera.transform.right;
        _ratio = _cameraDist / UserCamera.pixelWidth;
        _viewCenter = new Vector3(UserCamera.pixelRect.xMin + UserCamera.pixelRect.xMax,
                                  0f,
                                  UserCamera.pixelRect.yMin + UserCamera.pixelRect.yMax);
        UserCamera.transform.rotation = Quaternion.LookRotation(-_cameraVec, Vector3.Cross(-_cameraVec, UserCamera.transform.right));
        ScrollSensitivity = 0.2f;
        CameraSensitivity = 1f;
        GeomObjectFactory.GetCameraModeToggle().Changed += CameraModeChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_inAnimMove && _mode == CameraMode.Free &&
            Input.GetMouseButtonDown(2) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            StartMove(SwapYZ(Input.mousePosition));
            //if (Physics.Raycast(ray, out hit, 2000))
            //{
            //    StartMove(hit.point);
            //}
        }
        else if (!_inAnimMove && _mode == CameraMode.Free &&
                 Input.GetMouseButton(2) &&
                 (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            Move(SwapYZ(Input.mousePosition));
            UpdateCamera();
        }
        else if (!_inAnimMove && Input.GetMouseButtonDown(2))
        {
            StartOrbit(SwapYZ(Input.mousePosition));
        }
        else if (!_inAnimMove && Input.GetMouseButton(2))
        {
            Orbit(SwapYZ(Input.mousePosition));
            UpdateCamera();
        }
        else if (!_inAnimMove && Input.mouseScrollDelta.y != 0.0f)
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
        if (_mode == CameraMode.Free)
        {
            Vector3 startHVec = Vector3.Project(moveTarget, Vector3.right) - _viewCenter;
            Vector3 endHVec = Vector3.Project(_mouseStartPt, Vector3.right) - _viewCenter;
            Vector3 startVVec = Vector3.Project(moveTarget, Vector3.forward) - _viewCenter;
            Vector3 endVVec = Vector3.Project(_mouseStartPt, Vector3.forward) - _viewCenter;
            float hAngle = Vector3.SignedAngle(startHVec, endHVec, Vector3.up) * CameraSensitivity;
            float vAngle = Vector3.SignedAngle(startVVec, endVVec, Vector3.up) * CameraSensitivity;
            _cameraVec = Quaternion.AngleAxis(hAngle, Vector3.up) * Quaternion.AngleAxis(vAngle, UserCamera.transform.right) * _prevCameraVec;
        }
        else
        {
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(_mouseStartPt - 0.5f*_viewCenter, Vector3.up), Vector3.ProjectOnPlane(moveTarget - 0.5f*_viewCenter, Vector3.up), Vector3.up) * CameraSensitivity;
            UserCamera.transform.right = Quaternion.AngleAxis(-angle, Vector3.up) * UserCamera.transform.right;
            _mouseStartPt = moveTarget;
        }
    }

    private void Zoom(float amount)
    {
        _cameraDist = Mathf.Max(_cameraDist + ScrollSensitivity * amount, 0f);
    }

    private void UpdateCamera()
    {
        UserCamera.transform.position = _centerPt + _cameraVec * _cameraDist;
        UserCamera.transform.rotation = Quaternion.LookRotation(-_cameraVec, Vector3.Cross(-_cameraVec, UserCamera.transform.right));
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
        if (_mode == CameraMode.Free)
        {
            AnimMoveCamera(_origCenterPt, -_origCameraVec, _origCameraRight, _origCameraDist, 0.25f);
        }
        else
        {
            AnimMoveCamera(Vector3.zero, Vector3.down, Vector3.forward, _origCameraDist, 0.25f);
        }
    }

    private void AnimMoveCamera(Vector3 endCenter, Vector3 endForward, Vector3 endRight, float endDist, float time)
    {
        StartCoroutine(AnimMoveCameraCoRoutine(_centerPt, -_cameraVec, UserCamera.transform.right, _cameraDist, endCenter, endForward, endRight,  endDist, time));
    }

    private void CameraModeChanged(int mode)
    {
        if (mode == 0)
        {
            AnimMoveCamera(_prevCenterPt, -_prevCameraVec, _prevCameraRight, _cameraDist, 0.25f);
            _mode = CameraMode.Free;
        }
        else
        {
            _prevCenterPt = _centerPt;
            _prevCameraVec = _cameraVec;
            _prevCameraRight = UserCamera.transform.right;
            AnimMoveCamera(Vector3.zero, Vector3.down, Vector3.forward, _cameraDist, 0.25f);
            _mode = CameraMode.TopDown;
        }
    }

    private IEnumerator AnimMoveCameraCoRoutine(Vector3 startCenter, Vector3 startForward, Vector3 startRight, float startDist, Vector3 endCenter, Vector3 endForward, Vector3 endRight, float endDist, float time)
    {
        _inAnimMove = true;

        for (float t = 0f; t <= 1f; t += _animStepTime / time)
        {
            Vector3 center = Vector3.Lerp(startCenter, endCenter, t);
            Vector3 forwardVec = Vector3.Slerp(startForward.normalized, endForward.normalized, t);
            Vector3 rightVec = Vector3.Slerp(startRight, endRight, t);

            _cameraDist = Mathf.Lerp(startDist, endDist, t);
            _centerPt = center;
            _cameraVec = -forwardVec;
            UserCamera.transform.right = rightVec;
            UpdateCamera();

            yield return _wait;
        }

        _inAnimMove = false;
    }

    public Camera UserCamera;
    private Vector3 _viewCenter;
    private Vector3 _prevCenterPt;
    private Vector3 _centerPt;
    private Vector3 _prevCameraVec;
    private Vector3 _prevCameraRight;
    private Vector3 _cameraVec;
    private float _cameraDist;
    private float _ratio;
    private Vector3 _mouseStartPt;
    private bool _inAnimMove = false;
    private CameraMode _mode = CameraMode.Free;
    private static readonly float _animStepTime = 1f / 60f;
    private WaitForSeconds _wait = new WaitForSeconds(_animStepTime);
    public float ScrollSensitivity { get; set; }
    public float CameraSensitivity { get; set; }

    private Vector3 _origCenterPt;
    private Vector3 _origCameraVec;
    private Vector3 _origCameraRight;
    private float _origCameraDist;
}
