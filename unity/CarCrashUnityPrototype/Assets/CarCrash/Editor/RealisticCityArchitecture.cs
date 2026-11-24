using CarCrash;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    public static partial class RealisticCityBuilder
    {
        private static readonly string[] ArchitectureNames={"Art Deco hotel","Brick warehouse","Glass courtyard apartments","Mediterranean apartments","Neighborhood corner store","Brick townhouses","Contemporary offices","Motor service workshop"};
        private static readonly string[] ShopNames={"SUNDOWN COFFEE","GOLDEN CRUST BAKERY","PACIFIC FRESH MARKET","PALOMA PHARMACY","VINYL & CO.","ELYSIAN MOTOR WORKS","WESTSIDE DINER","COMMON THREAD","HOTEL PALOMA"};
        private static readonly Color[] FacadeColors={new(.83f,.79f,.67f),new(.55f,.31f,.22f),new(.65f,.70f,.71f),new(.80f,.70f,.52f),new(.45f,.61f,.54f),new(.42f,.29f,.23f),new(.68f,.70f,.70f),new(.56f,.57f,.53f)};
        private static readonly Color[] ShopColors={new(.12f,.29f,.24f),new(.43f,.24f,.12f),new(.22f,.36f,.18f),new(.10f,.33f,.29f),new(.22f,.18f,.27f),new(.13f,.23f,.32f),new(.43f,.13f,.09f),new(.25f,.28f,.31f),new(.25f,.32f,.27f)};
        private static Material ArchitectureWall(int family,int variation)
        {
            var brick=family is 1 or 5;
            var tint=FacadeColors[family]*(variation==0 ? 1f : .89f);
            return RealismMaterialLibrary.WorldSurface("M_Architecture_"+family+"_"+variation,brick ? "brick_wall_003" : "plastered_wall_02",tint,brick ? .44f : .48f,.6f,.24f,.63f);
        }
        private static Material ShopAccent(int kind)=>RealismMaterialLibrary.Standard("M_Shop_Accent_"+kind,ShopColors[kind],.08f,.28f);
        private static Material RetailGlass()
        {
            var material=RealismMaterialLibrary.Standard("M_Retail_Clear_Glazing",new Color(.58f,.64f,.66f,.13f),0,.91f);
            material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
            return material;
        }
        private static void BuildArchitecturalCore(int side,float center,float facade,float z,float width,float depth,float height,Material wall,bool interior)
        {
            if(!interior)
            {
                var hull=Box("Masonry building",new Vector3(center,height*.5f,z),new Vector3(width,height,depth),wall,true);
                Object.DestroyImmediate(hull.GetComponent<MeshFilter>());Object.DestroyImmediate(hull.GetComponent<MeshRenderer>());
            }
            var recess=interior ? 7.15f : .48f;
            Box("Building ground floor core",new Vector3(facade+side*(recess+(width-recess)*.5f),1.85f,z),new Vector3(width-recess,3.7f,depth),wall,interior);
            Box("Building upper storeys",new Vector3(center+side*.11f,(height+3.7f)*.5f,z),new Vector3(width-.22f,height-3.7f,depth),wall,interior);
            if(!interior)return;
            foreach(var end in new[]{-1f,1f})
                Box("Interior side wall",new Vector3(facade+side*recess*.5f,1.85f,z+end*(depth*.5f-.08f)),new Vector3(recess,3.7f,.16f),_stone,true);
            Box("Shop interior floor",new Vector3(facade+side*(recess-.5f)*.5f,.112f,z),new Vector3(recess+.5f,.09f,depth-.3f),_bark,true);
            Box("Retail ceiling",new Vector3(facade+side*recess*.5f,3.18f,z),new Vector3(recess,.10f,depth-.3f),_trim);
            var strip=RealismMaterialLibrary.Standard("M_Retail_Ceiling_Light",new Color(.90f,.78f,.58f),0,.45f,.7f);
            foreach(var offset in new[]{-4.3f,0,4.3f})
                Box("Shop ceiling light",new Vector3(facade+side*2.2f,3.10f,z+offset),new Vector3(1.4f,.035f,.22f),strip);
        }

        private static void AddStorefrontPane(CityBuildingProfile shop,float facade,float bayZ,bool besideExit)
        {
            var z=bayZ+(besideExit?.67f:0f);
            var center=new Vector3(facade+shop.side*.28f,1.51f,z);
            var assembly=BeginBreakable("Shatterable storefront glass",center,8f);
            var prop=_city.GetComponent<BreakableCityProp>();
            prop.breakSpeedKmh=12f;
            prop.shatterGlass=true;
            prop.shop=shop;
            _city.gameObject.AddComponent<VehicleImpactMaterial>().surface=VehicleImpactSurface.Glass;
            var pane=Box("Shop glazing",center,new Vector3(.07f,2.68f,besideExit?2.25f:3.60f),RetailGlass(),true);
            pane.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            EndBreakable(assembly);
        }
        private static void DecorateArchitecture(int family,int side,float facade,float center,float z,float width,float depth,float height,Material wall,Material accent)
        {
            if(family==0)
            {
                for(var step=0;step<3;step++)
                    Box("Stepped Deco crown",new Vector3(center,height+.4f+step*.52f,z),new Vector3(width-1.3f-step*1.8f,.55f,depth-1-step*1.6f),_trim);
                foreach(var offset in new[]{-5f,0,5f})
                    Box("Deco vertical limestone fin",new Vector3(facade-side*.27f,height*.58f,z+offset),new Vector3(.55f,height*.72f,.19f),_trim);
                Box("Hotel entrance marquee",new Vector3(facade-side*.9f,3.05f,z),new Vector3(1.9f,.36f,4.8f),accent);
            }
            else if(family==1)
            {
                for(var bay=-2;bay<=2;bay++)
                    Box("Warehouse brick buttress",new Vector3(facade-side*.14f,height*.5f,z+bay*3.4f),new Vector3(.48f,height,.36f),wall);
                Box("Warehouse roof monitor",new Vector3(center,height+.9f,z),new Vector3(width*.53f,1.4f,depth*.72f),_metal);
                Box("Roof monitor glazing",new Vector3(center-side*width*.27f,height+.95f,z),new Vector3(.05f,.7f,depth*.65f),_glass);
            }
            else if(family is 2 or 6)
            {
                for(var y=4.4f;y<height-1;y+=3.55f)
                {
                    foreach(var end in new[]{-1f,1f})
                        Box("Office side ribbon glazing",new Vector3(center,y,z+end*(depth*.5f+.05f)),new Vector3(width*.88f,1.9f,.06f),_glass);
                    Box("Projecting floor slab",new Vector3(facade-side*.35f,y-1.02f,z),new Vector3(.85f,.16f,depth+.28f),_trim);
                }
                foreach(var offset in new[]{-6.4f,-2.15f,2.15f,6.4f})
                    Box("Facade aluminum sun fin",new Vector3(facade-side*.42f,height*.57f,z+offset),new Vector3(.85f,height*.78f,.075f),_metal);
                Box("Setback glazed penthouse",new Vector3(center+side*1.1f,height+1.05f,z),new Vector3(width*.66f,2.1f,depth*.72f),_glass);
                Box("Penthouse roof overhang",new Vector3(center+side*1.1f,height+2.15f,z),new Vector3(width*.71f,.18f,depth*.76f),_trim);
            }
            else if(family==3)
            {
                var tiles=RealismMaterialLibrary.WorldSurface("M_Terracotta_Roof","plastered_wall_02",new Color(.47f,.23f,.13f),1.5f,.35f,.18f);
                foreach(var direction in new[]{-1f,1f})
                {
                    var roof=Box("Terracotta pitched roof",new Vector3(center+direction*width*.25f,height+.65f,z),new Vector3(width*.55f,.13f,depth+.65f),tiles);
                    roof.transform.rotation=Quaternion.Euler(0,0,-direction*14);
                }
                for(var bay=-1;bay<=1;bay++)
                {
                    for(var n=0;n<10;n++)
                    {
                        var a=n*Mathf.PI/10;var b=(n+1)*Mathf.PI/10;
                        Tube("Stucco arcade arch",new Vector3(facade-side*.25f,1.8f+Mathf.Sin(a)*.8f,z+bay*4.3f+Mathf.Cos(a)*1.75f),new Vector3(facade-side*.25f,1.8f+Mathf.Sin(b)*.8f,z+bay*4.3f+Mathf.Cos(b)*1.75f),.12f,_trim);
                    }
                }
            }
            else if(family==4)
            {
                Box("Corner shop roof billboard",new Vector3(facade+side*.4f,height+.9f,z),new Vector3(.22f,1.65f,depth*.74f),accent);
                Sign("LOCAL GOODS  /  OPEN LATE",new Vector3(facade+side*.26f,height+.9f,z),Quaternion.Euler(0,side*90,0),.17f,new Color(.92f,.89f,.80f),depth*.64f,1.1f);
            }
            else if(family==5)
            {
                foreach(var offset in new[]{-5.4f,0,5.4f})
                {
                    Box("Townhouse bay projection",new Vector3(facade-side*.35f,5.45f,z+offset),new Vector3(.75f,3.8f,1.7f),wall);
                    Box("Bay picture window",new Vector3(facade-side*.75f,5.5f,z+offset),new Vector3(.04f,2.55f,1.27f),_glass);
                    Box("Bay window cap",new Vector3(facade-side*.4f,7.4f,z+offset),new Vector3(.95f,.14f,1.95f),_trim);
                }
                for(var i=0;i<8;i++)Box("Townhouse cornice corbel",new Vector3(facade-side*.22f,height-.5f,z-6.5f+i*1.85f),new Vector3(.65f,.48f,.22f),_trim);
            }
            else
            {
                Box("Workshop parapet fascia",new Vector3(facade-side*.15f,height-.3f,z),new Vector3(.38f,1.1f,depth+.2f),accent);
                for(var offset=-2;offset<=2;offset++)
                    Box("Service bay canopy rib",new Vector3(facade-side*.8f,3.45f,z+offset*3f),new Vector3(1.8f,.12f,.065f),_metal);
            }
        }
        private static void BuildShopDetails(int kind,int side,float facade,float z,float depth,bool interior,float avenue,int plot)
        {
            var accent=ShopAccent(kind);
            var cream=RealismMaterialLibrary.Standard("M_Retail_Enamel",new Color(.76f,.73f,.64f),.08f,.38f);
            var red=RealismMaterialLibrary.Standard("M_Retail_Red",new Color(.43f,.075f,.035f),0,.34f);
            var green=RealismMaterialLibrary.Standard("M_Retail_Produce_Green",new Color(.24f,.34f,.075f),0,.3f);
            var point=new Vector3(facade-side*.18f,1.35f,z-2.2f);
            Sign(kind is 0 or 1 or 6 ? "COFFEE  /  FRESH DAILY" : kind==5 ? "SERVICE  /  TIRES  /  REPAIR" : "OPEN  /  WELCOME",point,Quaternion.Euler(0,side*90,0),.075f,new Color(.90f,.86f,.73f),3.0f,.34f);
            if(!interior)return;
            var counterX=facade+side*5.65f;
            if(kind!=5)
            {
                Box("Shop service counter",new Vector3(counterX,.67f,z),new Vector3(.72f,1.0f,3.0f),kind==6 ? red : _bark,true);
                Box("Counter worktop",new Vector3(counterX,1.20f,z),new Vector3(.86f,.08f,3.18f),cream);
                Box("Point of sale terminal",new Vector3(counterX-side*.08f,1.39f,z+.95f),new Vector3(.18f,.29f,.27f),_metal);
            }
            for(var shelf=0;shelf<3;shelf++)
            {
                var y=.58f+shelf*.56f;
                Box("Retail back wall shelf",new Vector3(facade+side*6.83f,y,z),new Vector3(.5f,.06f,7.5f),_bark);
                for(var item=0;item<10;item++)
                    Box("Stocked retail shelving",new Vector3(facade+side*6.78f,y+.16f,z-3.4f+item*.74f),new Vector3(.29f,.25f,.28f+(item%3)*.055f),item%3==0 ? accent : item%3==1 ? cream : red);
            }
            if(kind is 0 or 1)
            {
                Box("Espresso machine",new Vector3(counterX,1.42f,z-.55f),new Vector3(.48f,.38f,.70f),_metal);
                for(var cup=0;cup<5;cup++)Cylinder("Cafe ceramic cup",new Vector3(counterX-side*.17f,1.31f,z+.25f+cup*.17f),.055f,.15f,cream);
                if(kind==1)
                    for(var loaf=0;loaf<6;loaf++)Sphere("Fresh bakery display",new Vector3(counterX-side*.1f,1.35f,z-1.1f+loaf*.32f),new Vector3(.15f,.09f,.10f),_bark);
            }
            else if(kind==2)
            {
                for(var crate=0;crate<2;crate++)
                {
                    var location=new Vector3(facade+side*.9f,.65f,z-4.5f+crate*1.5f);
                    Box("Produce display crate",location,new Vector3(.7f,.52f,1.15f),_bark);
                    for(var fruit=0;fruit<12;fruit++)Sphere("Fresh produce",location+new Vector3((fruit%3-1)*.19f,.31f,(fruit/3-1.5f)*.22f),Vector3.one*.085f,crate==0 ? red : green);
                }
            }
            else if(kind==3)
            {
                Box("Pharmacy medicine cabinet",new Vector3(facade+side*3.8f,1.3f,z-4.5f),new Vector3(.7f,2.15f,2.0f),cream);
                var location=new Vector3(facade-side*.18f,2.0f,z+4.3f);
                Box("Pharmacy cross vertical",location,new Vector3(.05f,.85f,.25f),green);
                Box("Pharmacy cross horizontal",location,new Vector3(.05f,.25f,.85f),green);
            }
            else if(kind==4)
            {
                for(var rack=0;rack<3;rack++)
                {
                    var location=new Vector3(facade+side*1.0f,.67f,z-4.5f+rack*1.4f);
                    Box("Record browsing rack",location,new Vector3(.74f,.85f,1.1f),_bark);
                    for(var record=0;record<6;record++)Box("Record sleeve display",location+new Vector3(-side*.08f,.49f,-.42f+record*.15f),new Vector3(.38f,.33f,.026f),record%2==0 ? accent : cream);
                }
            }
            else if(kind==5)
            {
                for(var tire=0;tire<4;tire++)Cylinder("Workshop tire stack",new Vector3(facade+side*1.4f,.35f+tire*.22f,z-4.2f),.34f,.19f,_metal);
                Box("Workshop tool chest",new Vector3(facade+side*3.7f,.75f,z+3.5f),new Vector3(.65f,1.2f,1.7f),red);
                for(var drawer=0;drawer<5;drawer++)Box("Tool chest drawer pull",new Vector3(facade+side*3.35f,.36f+drawer*.2f,z+3.5f),new Vector3(.045f,.035f,1.35f),_trim);
                if(avenue==0&&plot>=14&&plot<=25)
                {
                    var car=DetailedRoadCarLibrary.Instantiate("bmw-m3-e30");car.name="Workshop display car";car.transform.SetParent(_city,false);
                    car.transform.position=new Vector3(facade+side*2.5f,.16f,z);
                    car.transform.rotation=Quaternion.Euler(0,-side*90,0)*car.transform.rotation;
                }
            }
            else if(kind is 6 or 8)
            {
                for(var seat=0;seat<3;seat++)
                {
                    var location=new Vector3(facade+side*1.1f,.52f,z-4.5f+seat*1.6f);
                    Box("Upholstered diner booth",location,new Vector3(.85f,.30f,1.25f),red);
                    Box("Booth padded back",location+new Vector3(-side*.35f,.42f,0),new Vector3(.18f,.75f,1.25f),red);
                    Box("Diner table top",location+new Vector3(side*.7f,.22f,0),new Vector3(.72f,.075f,1.1f),cream);
                }
            }
            else
            {
                Tube("Clothing rack rail",new Vector3(facade+side*1.05f,1.65f,z-5.5f),new Vector3(facade+side*1.05f,1.65f,z-2.5f),.025f,_metal);
                for(var item=0;item<7;item++)Box("Hanging shop garments",new Vector3(facade+side*1.05f,1.25f,z-5.3f+item*.4f),new Vector3(.48f,.70f,.085f),item%2==0 ? accent : cream);
            }
            if(kind is 0 or 6)
            {
                var location=new Vector3(side*10.1f,.15f,z+4.5f);
                var parent=BeginBreakable("Breakaway cafe furniture",location,22);
                Tube("Cafe table pedestal",location,location+Vector3.up*.70f,.04f,_metal,true);
                Cylinder("Cafe table top",location+Vector3.up*.73f,.44f,.055f,_bark,true);
                foreach(var direction in new[]{-1f,1f})
                {
                    Box("Cafe chair seat",location+new Vector3(0,.38f,direction*.74f),new Vector3(.42f,.08f,.42f),_bark,true);
                    Box("Cafe chair back",location+new Vector3(0,.70f,direction*.92f),new Vector3(.42f,.55f,.065f),_bark);
                }
                EndBreakable(parent);
            }
        }
        private static void Sphere(string name,Vector3 point,Vector3 radius,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(_city,false);obj.transform.position=point;obj.transform.localScale=radius*2;
            obj.GetComponent<MeshRenderer>().sharedMaterial=material;Object.DestroyImmediate(obj.GetComponent<Collider>());
        }
    }
}
