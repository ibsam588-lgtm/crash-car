using UnityEngine;
using UnityEngine.EventSystems;

namespace CarCrash
{
    public sealed class HudButtonMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        private bool _hover,_pressed;
        private void Update()=>transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(_pressed ? .96f : _hover ? 1.015f : 1f),1-Mathf.Exp(-20*Time.unscaledDeltaTime));
        public void OnPointerEnter(PointerEventData data)=>_hover=true;
        public void OnPointerExit(PointerEventData data){_hover=false;_pressed=false;}
        public void OnPointerDown(PointerEventData data)=>_pressed=true;
        public void OnPointerUp(PointerEventData data)=>_pressed=false;
        private void OnDisable(){_hover=_pressed=false;transform.localScale=Vector3.one;}
    }
}
