using UnityEngine;

namespace Tomu.Helpers
{
    public static class CameraHelper
    {
        public static Rect GetCameraWorldBounds(Camera cam = null)
        {
            if (cam == null)
                cam = Camera.main;

            if (!cam.orthographic)
            {
                Debug.LogError("GetCameraBounds only works with orthographic cameras.");
                return new Rect();
            }

            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;

            float left = cam.transform.position.x - (camWidth / 2f);
            float right = cam.transform.position.x + (camWidth / 2f);
            float top = cam.transform.position.y + (camHeight / 2f);
            float bottom = cam.transform.position.y - (camHeight / 2f);

            return Rect.MinMaxRect(left, bottom, right, top);
        }
    }
}
