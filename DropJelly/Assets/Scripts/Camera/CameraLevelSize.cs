using System;
using Grid;
using UnityEngine;
using UnityEngine.Serialization;

namespace Camera
{
    public class CameraLevelSize : MonoBehaviour
    {
        [Header("~~~~~~~~~ CAMERA LEVEL SETTINGS ~~~~~~~~~~")]
        [SerializeField] private int borderSize;
        [SerializeField] private float padding = 0.5f;

        private void Start()
        {
            SetupCamera();
        }

        private void SetupCamera()
        {
            UnityEngine.Camera.main.transform.position = new Vector3((GridManager.instance.columns - 1) / 2f, -(GridManager.instance.rows - 1) / 2f,
                UnityEngine.Camera.main.transform.position.z);

            var aspectRatio = UnityEngine.Camera.main.aspect;

            var verticalSize = (GridManager.instance.rows * borderSize) / 2f + padding;
            var horizontalSize = (GridManager.instance.columns * borderSize) / (2f * aspectRatio) + padding;

            UnityEngine.Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
        }
    }
}