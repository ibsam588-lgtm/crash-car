using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    public sealed class CityBreakableRenderer : MonoBehaviour
    {
        [Serializable] public sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public BreakableCityProp[] props;
            public Bounds bounds;
        }
        public Batch[] batches=Array.Empty<Batch>();
        private Transform _player;
        private Matrix4x4[] _matrices=new Matrix4x4[1023];
        public int InstanceCount { get; private set; }
        private void Start()
        {
            var vehicle=FindFirstObjectByType<ArcadeVehicleController>();if(vehicle!=null)_player=vehicle.transform;
            var unique=new HashSet<BreakableCityProp>();
            foreach(var batch in batches)foreach(var prop in batch.props)
            {
                if(prop==null)continue;unique.Add(prop);prop.Instanced=SystemInfo.supportsInstancing;
                foreach(var renderer in prop.visibleParts)if(renderer!=null)renderer.forceRenderingOff=prop.Instanced&&renderer.GetComponent<TextMesh>()==null;
            }
            InstanceCount=unique.Count;
        }
        private void LateUpdate()
        {
            if(!SystemInfo.supportsInstancing)return;
            var observer=_player!=null ? _player.position : Vector3.zero;
            foreach(var batch in batches)
            {
                if(batch.mesh==null||batch.material==null)continue;
                var distance=(batch.bounds.ClosestPoint(observer)-observer).sqrMagnitude;if(distance>360*360)continue;
                var count=0;
                foreach(var prop in batch.props)
                {
                    if(prop==null||prop.Broken||!prop.gameObject.activeInHierarchy)continue;
                    _matrices[count++]=prop.transform.localToWorldMatrix;
                    if(count==_matrices.Length){Draw(batch,count,distance);count=0;}
                }
                if(count>0)Draw(batch,count,distance);
            }
        }
        private void Draw(Batch batch,int count,float distance)
        {
            var parameters=new RenderParams(batch.material){worldBounds=batch.bounds,receiveShadows=true,
                shadowCastingMode=distance<115*115 ? ShadowCastingMode.On : ShadowCastingMode.Off};
            Graphics.RenderMeshInstanced(parameters,batch.mesh,0,_matrices,count);
        }
    }
}
