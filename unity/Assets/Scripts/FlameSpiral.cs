using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Video skin adapter: animated flame ribbons, rising core and orbiting faceted rubble.
    // Original APK fire sheet supplies the flame texture; time is local to the win burst.
    public sealed class FlameSpiral : MonoBehaviour
    {
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        readonly List<Transform> stones=new List<Transform>();
        MeshRenderer[] ribbons;Mesh[] strips;
        public void Initialize()
        {
            ribbons=new MeshRenderer[3];strips=new Mesh[3];
            for(int j=0;j<3;j++){
                var go=new GameObject(j==2?"Flame core":"Rising flame spiral "+j);go.transform.SetParent(transform,false);
                strips[j]=new Mesh{name=go.name};strips[j].MarkDynamic();meshes.Add(strips[j]);go.AddComponent<MeshFilter>().sharedMesh=strips[j];
                var mat=new Material(Shader.Find("YunJian/WinParticle")){mainTexture=Resources.Load<Texture2D>("Majsoul/Actions/fire-fire_bai_add")};mat.SetFloat("_DstBlend",1);materials.Add(mat);
                ribbons[j]=go.AddComponent<MeshRenderer>();ribbons[j].sharedMaterial=mat;ribbons[j].shadowCastingMode=ShadowCastingMode.Off;
            }
            var rockMat=new Material(Shader.Find("Unlit/Color")){color=new Color(.13f,.09f,.075f)};materials.Add(rockMat);
            for(int i=0;i<14;i++){
                var go=new GameObject("Orbiting rubble "+i);go.transform.SetParent(transform,false);
                var mesh=new Mesh{name=go.name,vertices=new[]{new Vector3(-.6f,0,-.5f),new Vector3(.5f,0,-.4f),new Vector3(.4f,0,.6f),new Vector3(-.45f,0,.4f),new Vector3(0,.8f,0),new Vector3(0,-.5f,0)},triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0,1,5,0,2,5,1,3,5,2,0,5,3}};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=rockMat;stones.Add(go.transform);
            }
            Sample(-1);
        }
        public void Sample(float age)
        {
            float appear=Mathf.Clamp01(age/.12f),fade=Mathf.Clamp01((1.4f-age)/.18f);
            float intensity=age<0?0:appear*fade;
            for(int j=0;j<3;j++){
                ribbons[j].enabled=intensity>0;
                if(intensity<=0)continue;
                const int segments=96;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];var indices=new int[segments*6];
                for(int i=0;i<=segments;i++){
                    float t=i/(float)segments,height=(j==2?1.7f:1.8f)*t*appear;
                    float theta=t*Mathf.PI*7.4f-age*10+j*Mathf.PI,radius=Mathf.Lerp(.29f,.025f,t);
                    float width=j==2?.13f*(1-t*.72f):.115f*(1-t*.68f);
                    var center=j==2?new Vector3(Mathf.Sin(age*7+t*8)*.012f,height,0):new Vector3(Mathf.Cos(theta)*radius,height,Mathf.Sin(theta)*radius);
                    var offset=j==2?Vector3.right*width:new Vector3(Mathf.Cos(theta),.22f,Mathf.Sin(theta))*width;
                    vertices[i*2]=center-offset;vertices[i*2+1]=center+offset;
                    // Repeat one original sheet cell vertically, then animate its source frame.
                    int frame=((int)(age*25)+j*5)%16;float tx=(frame%4)*.25f,ty=1-(frame/4+1)*.25f;
                    uv[i*2]=new Vector2(tx,ty+t*.25f);uv[i*2+1]=new Vector2(tx+.25f,uv[i*2].y);
                    Color c=j==2?new Color(1,.85f,.18f,intensity*.82f):new Color(1,j==0?.35f:.65f,.045f,intensity*(1-t*.45f));colors[i*2]=colors[i*2+1]=c;
                    if(i<segments){int k=i*6,a=i*2;indices[k]=a;indices[k+1]=a+1;indices[k+2]=a+2;indices[k+3]=a+1;indices[k+4]=a+3;indices[k+5]=a+2;}
                }
                strips[j].vertices=vertices;strips[j].uv=uv;strips[j].colors=colors;strips[j].triangles=indices;strips[j].RecalculateBounds();
                ribbons[j].sharedMaterial.SetColor("_TintColor",new Color(j==2?.72f:.4f,j==2?.72f:.4f,j==2?.72f:.4f,1));
            }
            for(int i=0;i<stones.Count;i++){
                var s=stones[i];s.gameObject.SetActive(age>=0 && age<1.3f);
                float theta=i*Mathf.PI*2/stones.Count+age*6,radius=.26f+.055f*Mathf.Sin(i*2.13f);
                s.localPosition=new Vector3(Mathf.Cos(theta)*radius,.04f+Mathf.Abs(Mathf.Sin(i*1.3f+age*3))*.11f,Mathf.Sin(theta)*radius);
                s.localRotation=Quaternion.Euler(i*34+age*160,i*67+age*260,i*39);
                s.localScale=Vector3.one*(.045f+(i%3)*.017f)*Mathf.Clamp01(age/.075f)*Mathf.Clamp01((1.3f-age)/.2f);
            }
        }
        void OnDestroy(){foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
