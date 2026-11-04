using System.Collections.Generic;
using System.IO;
using Spine;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi {
    // The original effect prefab owns the transform and visibility of this renderer.
    public sealed class NativeSpineEffect : MonoBehaviour {
        public TextAsset atlas, skeletonData;
        public Texture2D[] textures;
        public float scale = .01f, speed = 1;
        public string animationName;
        public bool loop;

        sealed class Pages : Spine.TextureLoader {
            readonly Texture2D[] textures;
            int index;
            public Pages(Texture2D[] source) { textures = source; }
            public void Load(AtlasPage page, string path) { page.rendererObject = textures[index++]; }
            public void Unload(object texture) { }
        }

        Skeleton skeleton;
        Spine.Animation animation;
        Mesh mesh;
        MeshRenderer meshRenderer;
        float origin = -1;
        readonly Dictionary<(AtlasPage, Spine.BlendMode), Material> materials = new();
        readonly SkeletonClipping clipper = new();
        readonly float[] world = new float[65536];
        static readonly int[] Quad = {0, 1, 2, 2, 3, 0};

        void Initialize() {
            var source = new Atlas(new StringReader(atlas.text), "", new Pages(textures));
            source.FlipV();
            var loader = new SkeletonBinary(new AtlasAttachmentLoader(source)) { Scale = scale };
            var data = loader.ReadSkeletonData(new MemoryStream(skeletonData.bytes));
            skeleton = new Skeleton(data);
            animation = data.FindAnimation(animationName);
            mesh = new Mesh { name = "Original effect Spine mesh", indexFormat = IndexFormat.UInt32 };
            GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = GetComponent<MeshRenderer>();
        }

        Material MaterialFor(AtlasPage page, Spine.BlendMode blend) {
            if (materials.TryGetValue((page, blend), out var material)) return material;
            material = new Material(Shader.Find("YunJian/CharacterSpine")) { mainTexture = (Texture2D)page.rendererObject };
            material.SetFloat("_WorldSpace", 1);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_PMA", page.pma ? 1 : 0);
            material.SetFloat("_SrcBlend", (float)(blend == Spine.BlendMode.Multiply ? UnityEngine.Rendering.BlendMode.DstColor : blend == Spine.BlendMode.Screen ? UnityEngine.Rendering.BlendMode.OneMinusDstColor : UnityEngine.Rendering.BlendMode.One));
            material.SetFloat("_DstBlend", (float)(blend == Spine.BlendMode.Additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            materials[(page, blend)] = material;
            return material;
        }

        public void Sample(float age) {
            if (age < 0 || !gameObject.activeInHierarchy) { origin = -1; mesh?.Clear(); return; }
            if (skeleton == null) Initialize();
            if (origin < 0 || age < origin) origin = age;
            skeleton.SetToSetupPose();
            animation?.Apply(skeleton, 0, (age-origin)*speed, loop, null, 1, MixBlend.Setup, MixDirection.In);
            skeleton.UpdateWorldTransform(Skeleton.Physics.Pose);
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var dark = new List<Vector3>();
            var colors = new List<Color>();
            var layers = new List<Material>();
            var triangles = new List<List<int>>();
            clipper.ClipEnd();
            foreach (var slot in skeleton.DrawOrder) {
                if (slot.Attachment is ClippingAttachment clip) { clipper.ClipStart(slot, clip); continue; }
                float[] positions = world, coordinates;
                int[] indices;
                int length;
                AtlasRegion region;
                Color color;
                if (slot.Attachment is RegionAttachment quad) {
                    quad.ComputeWorldVertices(slot, world, 0, 2);
                    coordinates = quad.UVs; indices = Quad; length = 8; region = (AtlasRegion)quad.Region;
                    color = new Color(quad.R*slot.R, quad.G*slot.G, quad.B*slot.B, quad.A*slot.A);
                } else if (slot.Attachment is MeshAttachment part) {
                    length = part.WorldVerticesLength;
                    part.ComputeWorldVertices(slot, 0, length, world, 0, 2);
                    coordinates = part.UVs; indices = part.Triangles; region = (AtlasRegion)part.Region;
                    color = new Color(part.R*slot.R, part.G*slot.G, part.B*slot.B, part.A*slot.A);
                } else { clipper.ClipEnd(slot); continue; }
                if (clipper.IsClipping) {
                    clipper.ClipTriangles(world, indices, indices.Length, coordinates);
                    positions = clipper.ClippedVertices.Items; length = clipper.ClippedVertices.Count;
                    coordinates = clipper.ClippedUVs.Items; indices = clipper.ClippedTriangles.ToArray();
                }
                if (length > 0) {
                    var material = MaterialFor(region.page, slot.Data.BlendMode);
                    if (layers.Count == 0 || layers[layers.Count-1] != material) {
                        layers.Add(material); triangles.Add(new List<int>());
                    }
                    int start = vertices.Count;
                    for (int i = 0; i < length; i += 2) {
                        vertices.Add(new Vector3(positions[i], positions[i+1], 0));
                        uv.Add(new Vector2(coordinates[i], coordinates[i+1]));
                        colors.Add(color);
                        dark.Add(slot.HasSecondColor ? new Vector3(slot.R2, slot.G2, slot.B2) : Vector3.zero);
                    }
                    foreach (int index in indices) triangles[triangles.Count-1].Add(start+index);
                }
                clipper.ClipEnd(slot);
            }
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, dark); mesh.SetColors(colors);
            mesh.subMeshCount = triangles.Count;
            for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i, false);
            mesh.RecalculateBounds();
            meshRenderer.sharedMaterials = layers.ToArray();
        }

        void OnDestroy() {
            if (mesh != null) Destroy(mesh);
            foreach (var material in materials.Values) Destroy(material);
        }
    }
}
