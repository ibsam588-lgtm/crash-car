using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static class DetailedRoadCarLibrary
    {
        public const string Root="Assets/CarCrash/";
        public const string ModelRoot=Root+"Models/RoadCars/";
        private const string Textures=Root+"Textures/RoadCars/";
        [Serializable] public class ModelData { public MaterialData[] materials; public float wheelRadius; }
        [Serializable] public class MaterialData
        {
            public string name,role,baseTexture,normalTexture,maskTexture;
            public float[] color;
            public float metallic,roughness;
        }
        public static ModelData Read(string id)=>JsonUtility.FromJson<ModelData>(File.ReadAllText(ModelRoot+id+".json"));
        public static void Prepare()
        {
            Directory.CreateDirectory(Root+"Materials/RoadCars");AssetDatabase.Refresh();
            foreach(var id in new[]{"bmw-m3-e30","lotus-elise"})
            {
                var importer=AssetImporter.GetAtPath(ModelRoot+id+".fbx") as ModelImporter;
                if(importer==null)throw new FileNotFoundException("Prepare the detailed road car: "+id);
                importer.importNormals=ModelImporterNormals.Import;
                importer.importTangents=ModelImporterTangents.CalculateMikk;
                importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                foreach(var material in Read(id).materials)
                {
                    ConfigureTexture(material.baseTexture,false,false);
                    ConfigureTexture(material.normalTexture,true,true);
                    ConfigureTexture(material.maskTexture,false,true);
                }
            }
        }
        private static void ConfigureTexture(string file,bool normal,bool linear)
        {
            if(string.IsNullOrEmpty(file))return;
            if(AssetImporter.GetAtPath(Textures+file) is not TextureImporter importer)return;
            importer.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=!linear;importer.mipmapEnabled=true;
            importer.maxTextureSize=file.Contains("mask") ? 1024 : 2048;
            importer.anisoLevel=8;importer.filterMode=FilterMode.Trilinear;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        public static GameObject Instantiate(string id)
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot+id+".fbx"));
            Apply(model,id);return model;
        }
        public static void Apply(GameObject model,string id)
        {
            var materials=new Dictionary<string,Material>();
            foreach(var info in Read(id).materials)
            {
                var color=new Color(info.color[0],info.color[1],info.color[2],info.color[3]);
                if(info.role=="Paint_1")color=new Color(.25f,.27f,.30f);
                if(info.role=="Glass")color=new Color(.16f,.20f,.23f,.35f);
                if(info.role=="Lens")color=new Color(.82f,.86f,.90f,1f);
                if(info.role=="Tire"&&string.IsNullOrEmpty(info.baseTexture))color=new Color(.035f,.038f,.041f);
                if(info.role=="Alloy"&&string.IsNullOrEmpty(info.baseTexture))color=new Color(.62f,.64f,.66f);
                var path=Root+"Materials/RoadCars/"+info.name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
                material.shader=Shader.Find("Standard");material.color=color;
                material.SetFloat("_Mode",0);material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.Zero);
                material.SetInt("_ZWrite",1);material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=-1;
                material.SetFloat("_Metallic",info.metallic);
                material.SetFloat("_Glossiness",1-Mathf.Clamp(info.roughness,.12f,.94f));
                material.mainTexture=string.IsNullOrEmpty(info.baseTexture) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+info.baseTexture);
                if(!string.IsNullOrEmpty(info.normalTexture))
                {
                    material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+info.normalTexture));
                    material.SetFloat("_BumpScale",.65f);material.EnableKeyword("_NORMALMAP");
                }
                if(!string.IsNullOrEmpty(info.maskTexture)&&info.role!="Paint_1"&&info.role!="Glass"&&info.role!="Lens")
                {
                    material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+info.maskTexture));
                    material.SetFloat("_GlossMapScale",.85f);material.EnableKeyword("_METALLICGLOSSMAP");
                }
                if(info.role=="Paint_1"){RealismMaterialLibrary.AutomotivePaint(material);material.SetFloat("_Glossiness",.73f);}
                if(info.role is "Glass" or "Lens")
                {
                    material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.93f);
                    material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
                }
                if(info.role is "Headlamp" or "Taillamp" or "Amber")
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor",info.role=="Taillamp" ? new Color(.13f,.008f,.003f) : info.role=="Amber" ? new Color(.15f,.06f,.003f) : new Color(.09f,.09f,.08f));
                }
                materials[info.name]=material;EditorUtility.SetDirty(material);
            }
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var slots=renderer.sharedMaterials;
                for(var i=0;i<slots.Length;i++)if(slots[i]!=null&&materials.TryGetValue(slots[i].name,out var replacement))slots[i]=replacement;
                renderer.sharedMaterials=slots;
            }
        }
    }
}
