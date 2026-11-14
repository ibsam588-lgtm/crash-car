using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrash
{
    public sealed class CityFoliageRenderer : MonoBehaviour
    {
        [Serializable] public struct Frond { public Vector3 position;public Quaternion rotation;public float scale; }
        [Serializable] public sealed class Batch { public Bounds bounds;public Frond[] fronds; }
        public Mesh mesh;
        public Material material;
        public Batch[] batches=Array.Empty<Batch>();
        private Matrix4x4[][] _matrices;
        private Mesh[] _fallback;
        private Transform _player;
        public int InstanceCount {get {var count=0;foreach(var batch in batches)count+=batch.fronds.Length;return count;}}
        private void Start()
        {
            var player=FindFirstObjectByType<ArcadeVehicleController>();if(player!=null)_player=player.transform;
            _matrices=new Matrix4x4[batches.Length][];
            if(!SystemInfo.supportsInstancing)_fallback=new Mesh[batches.Length];
            for(var i=0;i<batches.Length;i++)
            {
                _matrices[i]=new Matrix4x4[batches[i].fronds.Length];
                for(var j=0;j<_matrices[i].Length;j++)
                {
                    var frond=batches[i].fronds[j];_matrices[i][j]=Matrix4x4.TRS(frond.position,frond.rotation,Vector3.one*frond.scale);
                }
                if(_fallback==null)continue;
                var combine=new CombineInstance[_matrices[i].Length];
                for(var j=0;j<combine.Length;j++)combine[j]=new CombineInstance{mesh=mesh,transform=_matrices[i][j]};
                var fallback=new Mesh{indexFormat=IndexFormat.UInt32,hideFlags=HideFlags.HideAndDontSave};
                fallback.CombineMeshes(combine,true,true);_fallback[i]=fallback;
            }
        }
        private void LateUpdate()
        {
            if(_matrices==null||mesh==null||material==null)return;
            var observer=_player!=null ? _player.position : transform.position;
            for(var i=0;i<batches.Length;i++)
            {
                var distance=(batches[i].bounds.ClosestPoint(observer)-observer).sqrMagnitude;
                if(distance>360*360)continue;
                var parameters=new RenderParams(material){worldBounds=batches[i].bounds,receiveShadows=true,
                    shadowCastingMode=distance<115*115 ? ShadowCastingMode.On : ShadowCastingMode.Off};
                if(_fallback!=null)Graphics.RenderMesh(parameters,_fallback[i],0,Matrix4x4.identity);
                else Graphics.RenderMeshInstanced(parameters,mesh,0,_matrices[i]);
            }
        }
        private void OnDestroy(){if(_fallback!=null)foreach(var mesh in _fallback)if(mesh!=null)Destroy(mesh);}
    }
}
