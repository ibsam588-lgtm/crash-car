using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CarCrash
{
    public sealed class HudHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld => _pointers.Count > 0;
        public Color activeColor = new(.13f,.42f,.55f,.98f);
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
            if (data.button == PointerEventData.InputButton.Left)
                _pointers.Add(data.pointerId);
        }
        public void OnPointerUp(PointerEventData data)
        {
            _pointers.Remove(data.pointerId);
        }
        public void OnPointerExit(PointerEventData data)
        {
            // Touches retain their press target until lift, even if a thumb drifts.
            // The event system sends PointerUp to that target outside its bounds.
            if (data.pointerId < 0) OnPointerUp(data);
        }
        public void Release()
        {
            _pointers.Clear();
            transform.localScale=Vector3.one;
            if (_graphic != null) _graphic.color = _restColor;
        }
        private void OnDisable() => Release();
        private void Update()
        {
            if(_graphic==null)return;
            var blend=1-Mathf.Exp(-20*Time.unscaledDeltaTime);
            _graphic.color=Color.Lerp(_graphic.color,IsHeld ? activeColor : _restColor,blend);
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(IsHeld ? .96f : 1),blend);
        }
    }
}
