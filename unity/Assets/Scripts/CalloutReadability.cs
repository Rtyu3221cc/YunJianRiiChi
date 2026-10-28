using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi {
    // Keep the equipped indicator visible around the river tile while reducing
    // only its screen-space overlap with the actual tile face. Instance materials
    // ensure that winning/riichi effects and ordinary guide materials stay intact.
    static class CalloutReadability {
        static readonly int Style=Shader.PropertyToID("_CalloutStyle"),Center=Shader.PropertyToID("_CalloutFaceCenter"),Right=Shader.PropertyToID("_CalloutFaceRight"),Forward=Shader.PropertyToID("_CalloutFaceForward"),Normal=Shader.PropertyToID("_CalloutFaceNormal");
        public static void Apply(IEnumerable<Material> materials,Transform tile,float intensity,float faceOpacity,bool enabled){
            var box=tile.GetComponent<BoxCollider>();
            var center=tile.TransformPoint(box.center+Vector3.up*box.size.y*.5f);
            var right=tile.TransformVector(Vector3.right*box.size.x*.5f);
            var forward=tile.TransformVector(Vector3.forward*box.size.z*.5f);
            foreach(var material in materials){
                material.SetVector(Style,new Vector4(intensity,enabled?1:0,0,0));
                material.SetVector(Center,new Vector4(center.x,center.y,center.z,faceOpacity));
                material.SetVector(Right,new Vector4(right.normalized.x,right.normalized.y,right.normalized.z,right.magnitude));
                material.SetVector(Forward,new Vector4(forward.normalized.x,forward.normalized.y,forward.normalized.z,forward.magnitude));
                material.SetVector(Normal,tile.up);
            }
        }
    }
}
