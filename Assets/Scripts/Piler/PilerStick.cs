using UnityEngine;
using UnityEngine.InputSystem;

namespace SantaPlay.Piler
{
    [RequireComponent(typeof(RectTransform))]
    public class PilerStick : MonoBehaviour
    {
        [SerializeField] private PilerConfig _config;

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private Camera _uiCamera;
        private float _targetX;
        private float _initialY;
        private float _currentY;
        private bool _canMove;

        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();

            if (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _parentCanvas.worldCamera;
            }

            _targetX = _rectTransform.anchoredPosition.x;
            _initialY = _rectTransform.anchoredPosition.y;
            _currentY = _initialY;
        }

        public void Initialize(PilerConfig config)
        {
            _config = config;
        }

        public void SetActiveMovement(bool active)
        {
            _canMove = active;
        }

        private void Update()
        {
            if (!_canMove || _config == null) return;

            HandlePointerInput();
            HandleKeyboardInput();
            UpdateSinking();
            ApplyMovement();
        }

        private void UpdateSinking()
        {
            var highestY = GetHighestStackY();
            if (highestY >= _config.SinkTriggerHeight && _currentY > _config.MinStickY)
            {
                _currentY = Mathf.Max(_config.MinStickY, _currentY - (_config.SinkSpeed * Time.deltaTime));
            }
        }

        public float GetHighestStackY()
        {
            var highest = 0f;
            var count = transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeSelf && child.localPosition.y > highest)
                {
                    highest = child.localPosition.y;
                }
            }
            return highest;
        }

        private void HandlePointerInput()
        {
            Vector2 screenPos = Vector2.zero;
            bool hasPointer = false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                hasPointer = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                screenPos = Mouse.current.position.ReadValue();
                hasPointer = true;
            }
            else if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                screenPos = Pointer.current.position.ReadValue();
                hasPointer = true;
            }

            if (hasPointer && _parentCanvas != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentCanvas.transform as RectTransform, screenPos, _uiCamera, out var localPoint))
                {
                    _targetX = Mathf.Clamp(localPoint.x, _config.MinStickX, _config.MaxStickX);
                }
            }
        }

        private void HandleKeyboardInput()
        {
            float horizontal = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            }

            if (Mathf.Abs(horizontal) > 0.01f)
            {
                _targetX += horizontal * _config.StickKeyboardSpeed * Time.deltaTime;
                _targetX = Mathf.Clamp(_targetX, _config.MinStickX, _config.MaxStickX);
            }
        }

        private void ApplyMovement()
        {
            var currentPos = _rectTransform.anchoredPosition;
            var newX = Mathf.Lerp(currentPos.x, _targetX, _config.StickSmoothSpeed * Time.deltaTime);
            _rectTransform.anchoredPosition = new Vector2(newX, _currentY);
        }

        public void ResetPosition()
        {
            _targetX = 0f;
            _currentY = _initialY;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = new Vector2(0f, _initialY);
            }
        }
    }
}
