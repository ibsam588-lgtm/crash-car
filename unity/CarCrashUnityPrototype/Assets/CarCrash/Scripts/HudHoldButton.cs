using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed class HudHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld => _pointers.Count > 0;
        public Color activeColor = new(0.32f, 0.46f, 0.08f, 0.95f);
        private readonly HashSet<int> _pointers = new();
        private Graphic _graphic;
        private Color _restColor;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _restColor = _graphic.color;
        }
        public void OnPointerDown(PointerEventData data)
        {
            _pointers.Add(data.pointerId);
            _graphic.color = activeColor;
        }
        public void OnPointerUp(PointerEventData data)
        {
            _pointers.Remove(data.pointerId);
            if (!IsHeld) _graphic.color = _restColor;
        }
        public void OnPointerExit(PointerEventData data) => OnPointerUp(data);
        public void Release()
        {
            _pointers.Clear();
            if (_graphic != null) _graphic.color = _restColor;
        }
        private void OnDisable() => Release();
    }
}
