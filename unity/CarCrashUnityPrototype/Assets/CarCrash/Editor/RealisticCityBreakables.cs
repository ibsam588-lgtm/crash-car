using System.Collections.Generic;
using CarCrash;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static partial class RealisticCityBuilder
    {
        private static Transform BeginBreakable(string name,Vector3 origin,float mass)
        {
            var previous=_city;
            var root=new GameObject(name).transform;root.SetParent(previous,false);root.position=origin;
            var body=root.gameObject.AddComponent<Rigidbody>();body.mass=mass;body.isKinematic=true;
            body.useGravity=true;body.linearDamping=.20f;body.angularDamping=.45f;body.maxAngularVelocity=5f;
            root.gameObject.AddComponent<BreakableCityProp>();_city=root;return previous;
        }
        private static void EndBreakable(Transform previous)
        {
            var prop=_city.GetComponent<BreakableCityProp>();
            prop.visibleParts=_city.GetComponentsInChildren<Renderer>();
            foreach(var child in _city.GetComponentsInChildren<Transform>())child.gameObject.isStatic=false;
            _city=previous;
        }
        private static void PrepareBreakableMeshes()
        {
            var prototypes=new Dictionary<(string,Material),Mesh>();
            var groups=new Dictionary<(Mesh,Material,int,int),List<BreakableCityProp>>();
            foreach(var prop in _city.GetComponentsInChildren<BreakableCityProp>())
            {
                var byMaterial=new Dictionary<Material,List<CombineInstance>>();
                var originals=new List<GameObject>();
                foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;
                    var material=renderer.sharedMaterial;
                    if(!byMaterial.TryGetValue(material,out var list)){list=new List<CombineInstance>();byMaterial[material]=list;}
                    list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=prop.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                    originals.Add(filter.gameObject);
                }
                foreach(var entry in byMaterial)
                {
                    var key=(prop.name,entry.Key);
                    if(!prototypes.TryGetValue(key,out var mesh))
                    {
                        mesh=new Mesh{name="Breakaway_"+prop.name.Replace(' ','_')+"_"+entry.Key.name,indexFormat=IndexFormat.UInt32};
                        mesh.CombineMeshes(entry.Value.ToArray(),true,true);mesh.RecalculateBounds();mesh=SaveMesh(mesh.name,mesh);prototypes[key]=mesh;
                    }
                    var obj=new GameObject("Breakaway render",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(prop.transform,false);
                    obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=entry.Key;
                    entry.Key.enableInstancing=true;EditorUtility.SetDirty(entry.Key);
                    var batchKey=(mesh,entry.Key,Mathf.FloorToInt(prop.transform.position.x/160),Mathf.FloorToInt(prop.transform.position.z/160));
                    if(!groups.TryGetValue(batchKey,out var items)){items=new List<BreakableCityProp>();groups[batchKey]=items;}items.Add(prop);
                }
                foreach(var original in originals)
                    if(original.GetComponent<Collider>()!=null){Object.DestroyImmediate(original.GetComponent<MeshFilter>());Object.DestroyImmediate(original.GetComponent<MeshRenderer>());}
                    else Object.DestroyImmediate(original);
                prop.visibleParts=prop.GetComponentsInChildren<Renderer>();
            }
            var batches=new List<CityBreakableRenderer.Batch>();
            foreach(var group in groups)
            {
                var point=group.Value[0].transform.position;var bounds=new Bounds(point,Vector3.one*16);
                foreach(var prop in group.Value)bounds.Encapsulate(new Bounds(prop.transform.position,Vector3.one*16));
                batches.Add(new CityBreakableRenderer.Batch{mesh=group.Key.Item1,material=group.Key.Item2,props=group.Value.ToArray(),bounds=bounds});
            }
            _city.gameObject.AddComponent<CityBreakableRenderer>().batches=batches.ToArray();
        }
    }
}
