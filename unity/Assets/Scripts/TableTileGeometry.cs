using UnityEngine;

namespace YunJian.Riichi
{
    // All gameplay tiles share one world-space size. Scene coordinates retain the
    // original import conversion; changing a tile's shape must not scale its anchors.
    public sealed class TableTileGeometry
    {
        public const float SourceWidth = .02947767f, SourceDepth = .01913448f;
        public const float SourceUnits = .33f / SourceWidth;
        public const float Width = .35f;
        public readonly float Length, Depth, Pitch, SidewaysPitch, RiverRowPitch;
        public float NorthPitch => Width + .001f;
        public float GroupGap => Width * .45f;
        public float StackRise => Depth + .005f;
        public Vector3 FlatSize => new Vector3(Width, Depth, Length);

        public TableTileGeometry(Bounds model, Bounds outline, float outlineScale)
        {
            Length = model.size.z * Width;
            Depth = model.size.y * Width;
            // Include the actual APK outline rather than inventing a second
            // sideways width. Packing is derived from the same shape as rendering.
            Pitch = Mathf.Max(model.size.x, outline.size.x * outlineScale) * Width + .001f;
            SidewaysPitch = Mathf.Max(model.size.z, outline.size.z * outlineScale) * Width + .001f;
            RiverRowPitch = SidewaysPitch + .012f;
        }

        public float Slot(bool sideways) => sideways ? SidewaysPitch : Pitch;

        public static float WorldWidth(Transform tile)
        {
            var model = tile.Find("Original tile");
            return model == null ? 0 : model.TransformVector(Vector3.right).magnitude;
        }

        // Carry the actual destination shape, including any parent scale. Collider
        // size alone is a local value and used to miss inherited presentation scale.
        public static void CopyShape(Transform source, Transform destination)
        {
            var from = source.Find("Original tile");
            var to = destination.Find("Original tile");
            destination.localScale = Vector3.one;
            to.localScale = from.lossyScale;
            to.localRotation = from.localRotation;
            to.localPosition = Vector3.Scale(from.localPosition, source.lossyScale);
            var sourceBox = source.GetComponent<BoxCollider>();
            var box = destination.GetComponent<BoxCollider>();
            box.size = Vector3.Scale(sourceBox.size, source.lossyScale);
            box.center = Vector3.Scale(sourceBox.center, source.lossyScale);
        }
    }
}
