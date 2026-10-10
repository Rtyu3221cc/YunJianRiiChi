using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Video skin adapter: broad, opaque flame column with rotating orange bands.
    // APK flame textures supply the actual detail; no synthetic neon line texture.
    public sealed class FlameSpiral : MonoBehaviour
    {
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        readonly List<Transform> stones=new List<Transform>();
        MeshRenderer[] ribbons;Mesh[] strips;
        public void Initialize()
        {
            ribbons=new MeshRenderer[6];strips=new Mesh[6];
            for(int j=0;j<6;j++){
                var go=new GameObject(j<4?"Broad flame band "+j:"Filled flame core "+j);go.transform.SetParent(transform,false);
                strips[j]=new Mesh{name=go.name};strips[j].MarkDynamic();meshes.Add(strips[j]);go.AddComponent<MeshFilter>().sharedMesh=strips[j];
                var mat=new Material(Shader.Find("YunJian/WinParticle")){mainTexture=Resources.Load<Texture2D>(j<4?"Majsoul/Settlement/flame-detail":"Majsoul/Actions/fire-fire_bai_add")};
                mat.SetFloat("_DstBlend",j<4?10:1);mat.SetColor("_TintColor",j<4?new Color(.65f,.65f,.65f,1):new Color(1,1.3f,1.2f,1));materials.Add(mat);
                ribbons[j]=go.AddComponent<MeshRenderer>();ribbons[j].sharedMaterial=mat;ribbons[j].shadowCastingMode=ShadowCastingMode.Off;
            }
            var rockMat=new Material(Shader.Find("Unlit/Color")){color=new Color(.13f,.09f,.075f)};materials.Add(rockMat);
            for(int i=0;i<20;i++){
                var go=new GameObject("Orbiting rubble "+i);go.transform.SetParent(transform,false);
                var mesh=new Mesh{name=go.name,vertices=new[]{new Vector3(-.6f,0,-.5f),new Vector3(.5f,0,-.4f),new Vector3(.4f,0,.6f),new Vector3(-.45f,0,.4f),new Vector3(0,.8f,0),new Vector3(0,-.5f,0)},triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0,1,5,0,2,5,1,3,5,2,0,5,3}};
                mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=rockMat;stones.Add(go.transform);
            }
            Sample(-1);
        }
        public void Sample(float age)
        {
            float appear=Mathf.SmoothStep(0,1,age/.22f),fade=Mathf.Clamp01((1.72f-age)/.32f);
            float intensity=age<0?0:appear*fade;
            // The reference column widens toward its crown, about four tile widths
            // across at full height. Keep the broad filled center between its bands.
            for(int j=0;j<6;j++){
                ribbons[j].enabled=intensity>0;if(intensity<=0)continue;
                const int segments=120;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];var indices=new int[segments*6];
                for(int i=0;i<=segments;i++){
                    float t=i/(float)segments,height=3.35f*t*appear;
                    float theta=t*Mathf.PI*6.3f-age*10+j*Mathf.PI*.5f;
                    float radius=Mathf.Lerp(.18f,.43f,t)+Mathf.Sin(t*31-age*13)*.025f;
                    float width=((j<4?.17f:.24f)+t*(j<4?.12f:.14f))*(j<4?1:1+Mathf.Sin(t*45-age*17)*.13f);
                    var center=j<4?new Vector3(Mathf.Cos(theta)*radius,height,Mathf.Sin(theta)*radius):new Vector3(Mathf.Sin(age*7+t*14)*.035f,height,0);
                    var direction=j<4?new Vector3(Mathf.Cos(theta),.65f,Mathf.Sin(theta)):j==4?Vector3.right:Vector3.forward;
                    var offset=direction*width;
                    vertices[i*2]=center-offset;vertices[i*2+1]=center+offset;
                    // Repeat the original flame image along the rising column,
                    // scroll its turbulent edge, and retain its colored fire detail.
                    float v=t*(j<4?5.5f:3.5f)-age*(j<4?1.7f:1.15f)+j*.27f;
                    if(j<4){uv[i*2]=new Vector2(.05f,v);uv[i*2+1]=new Vector2(.95f,v);}
                    else {
                        // Use an opaque scanline of the native 4x4 fire sheet for
                        // the center: a ring-shaped texture left large holes here.
                        int frame=((int)(age*18)+j)%16;float tx=(frame%4)*.25f,ty=1-(frame/4+1)*.25f;
                        uv[i*2]=new Vector2(tx+.025f,ty+.125f);uv[i*2+1]=new Vector2(tx+.225f,ty+.125f);
                    }
                    float edge=Mathf.Clamp01(t/.025f)*Mathf.Clamp01((1-t)/.06f);
                    var c=j<4?new Color(1,j%2==0?.65f:.85f,.27f,intensity*edge):new Color(1,.82f,.32f,intensity*edge*.65f);
                    colors[i*2]=colors[i*2+1]=c;
                    if(i<segments){int k=i*6,a=i*2;indices[k]=a;indices[k+1]=a+1;indices[k+2]=a+2;indices[k+3]=a+1;indices[k+4]=a+3;indices[k+5]=a+2;}
                }
                strips[j].vertices=vertices;strips[j].uv=uv;strips[j].colors=colors;strips[j].triangles=indices;strips[j].RecalculateBounds();
            }
            for(int i=0;i<stones.Count;i++){
                var s=stones[i];s.gameObject.SetActive(age>=0 && age<1.65f);
                float theta=i*Mathf.PI*2/stones.Count+age*6,radius=.38f+.1f*Mathf.Sin(i*2.13f);
                s.localPosition=new Vector3(Mathf.Cos(theta)*radius,.05f+Mathf.Abs(Mathf.Sin(i*1.3f+age*3))*.24f,Mathf.Sin(theta)*radius);
                s.localRotation=Quaternion.Euler(i*34+age*160,i*67+age*260,i*39);
                s.localScale=Vector3.one*(.06f+(i%3)*.022f)*Mathf.Clamp01(age/.075f)*Mathf.Clamp01((1.65f-age)/.25f);
            }
        }
        void OnDestroy(){foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
