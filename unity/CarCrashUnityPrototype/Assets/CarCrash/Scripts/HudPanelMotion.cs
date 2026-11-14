using UnityEngine;

namespace CarCrash
{
    public sealed class HudPanelMotion : MonoBehaviour
    {
        private CanvasGroup _group;
        private float _age;
        private void Awake()=>_group=gameObject.AddComponent<CanvasGroup>();
        private void OnEnable(){_age=0;if(_group!=null)_group.alpha=0;}
        private void Update(){_age+=Time.unscaledDeltaTime;_group.alpha=Mathf.SmoothStep(0,1,_age/.2f);}
    }
}
