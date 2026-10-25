using System;
using UnityEngine;

namespace YunJian.Riichi {
    // A separate camera uses the same tile meshes and shaders as the playing table.
    public sealed class WardrobeTablePreview:IDisposable {
        GameObject root;Camera camera;RenderTexture target;OriginalTiles tiles;Material cloth;
        string current,requested;WardrobeItem face,back,mat;
        public Texture Image=>target;
        public void Request(WardrobeItem clothItem,WardrobeItem backItem,WardrobeItem faceItem){
            mat=clothItem;back=backItem;face=faceItem;requested=mat.id+"/"+back.id+"/"+face.id;
        }
        public void Tick(){
            if(requested==null)return;
            if(current!=requested){Build();current=requested;}
            var active=RenderTexture.active;camera.Render();RenderTexture.active=active;
        }
        void Build(){
            Release();root=new GameObject("Decoration table preview");root.transform.position=new Vector3(300,300,300);
            cloth=new Material(Shader.Find("YunJian/TableCloth")){mainTexture=WardrobeCatalog.Texture(mat.texture)};
            var plane=GameObject.CreatePrimitive(PrimitiveType.Quad);plane.name="Selected cloth";plane.transform.SetParent(root.transform,false);plane.transform.localRotation=Quaternion.Euler(90,0,0);plane.transform.localScale=Vector3.one*8;plane.GetComponent<Renderer>().sharedMaterial=cloth;UnityEngine.Object.Destroy(plane.GetComponent<Collider>());
            tiles=new OriginalTiles(face,back);
            string[] sample={"p1","s1","m1","z1","z7"};
            for(int row=0;row<2;row++)for(int i=0;i<sample.Length;i++)tiles.CreateTable(root.transform,sample[i],new Vector3(-2.9f+i*.62f,.015f,2.8f-row*1.1f),row==0);
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=27;
            camera=new GameObject("Decoration preview camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.transform.localPosition=new Vector3(0,12,-.01f);camera.transform.localRotation=Quaternion.Euler(90,0,0);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=4.12f;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.04f);camera.cullingMask=1<<27;camera.allowMSAA=true;
            target=new RenderTexture(900,900,24,RenderTextureFormat.ARGB32){name="Selected decoration table",antiAliasing=4};target.Create();camera.targetTexture=target;
        }
        void Release(){if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}if(cloth!=null)UnityEngine.Object.Destroy(cloth);if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}tiles?.Dispose();tiles=null;target=null;root=null;camera=null;}
        public void Hide(){requested=null;current=null;Release();}
        public void Dispose()=>Hide();
    }
}
