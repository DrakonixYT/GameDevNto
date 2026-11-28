using UnityEngine;

namespace HangarGame.Core
{
    public static class ModelUtils
    {
        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            return comp != null ? comp : go.AddComponent<T>();
        }

        public static void DestroySafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        public static Bounds CalcBounds(GameObject go, bool includeInactive = true)
        {
            if (go == null) return new Bounds(Vector3.zero, Vector3.zero);
            var rends = go.GetComponentsInChildren<Renderer>(includeInactive);
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            return b;
        }

        public static Color GetColor(Renderer r)
        {
            if (r == null || r.sharedMaterial == null) return Color.white;
            return r.sharedMaterial.HasProperty("_BaseColor")
                ? r.sharedMaterial.GetColor("_BaseColor")
                : r.sharedMaterial.color;
        }

        public static void SetColor(Renderer r, Color color)
        {
            if (r == null) return;
            Material m = Application.isPlaying ? r.material : r.sharedMaterial;
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
        }

        public static void ApplyPrimaryMaterial(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                if (mats.Length > 0)
                {
                    mats[0] = mat;
                    r.sharedMaterials = mats;
                }
            }
        }

        public static Material CreateLitMaterial(Color color, bool emissive = false, float emissionMult = 0.8f)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Unlit/Color");
            var m = new Material(s);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emissionMult);
            }
            return m;
        }
    }
}
