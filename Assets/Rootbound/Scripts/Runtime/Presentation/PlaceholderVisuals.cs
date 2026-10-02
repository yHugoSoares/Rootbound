using UnityEngine;
using UnityEngine.Rendering;

namespace Rootbound.Unity
{
    public static class PlaceholderVisuals
    {
        public static GameObject CreateCapsule(string name, Color color)
        {
            return Create(PrimitiveType.Capsule, name, color);
        }

        public static GameObject CreateSphere(string name, Color color)
        {
            return Create(PrimitiveType.Sphere, name, color);
        }

        public static GameObject CreateCylinder(string name, Color color)
        {
            return Create(PrimitiveType.Cylinder, name, color);
        }

        private static GameObject Create(PrimitiveType type, string name, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material = CreateMaterial(color);
            return go;
        }

        public static Material CreateMaterial(Color color)
        {
            Shader shader = null;
            if (UsesUniversalPipeline()) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }

        private static bool UsesUniversalPipeline()
        {
            RenderPipelineAsset asset = GraphicsSettings.defaultRenderPipeline;
            if (asset == null) asset = QualitySettings.renderPipeline;
            return asset != null && asset.GetType().FullName.Contains("Universal");
        }

        public static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            Material material = renderer.material;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }
    }
}
