using UnityEngine;

namespace CarCrash
{
    public sealed class TrafficLivery : MonoBehaviour
    {
        public Color paint=new(.25f,.28f,.31f);
        private void OnEnable()
        {
            var block=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(var i=0;i<materials.Length;i++)
                    if(materials[i]!=null&&materials[i].name.Contains("Paint"))
                    {
                        renderer.GetPropertyBlock(block,i);block.SetColor("_Color",paint);
                        renderer.SetPropertyBlock(block,i);block.Clear();
                    }
            }
        }
    }
}
