using System;
using System.Linq;
using UnityEngine;

namespace Utilities
{
    public class Tuple<T1, T2>
    {
        public Tuple ()
        {
        }

        public Tuple (T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public T1 Item1
        {
            get;

            set;
        }

        public T2 Item2
        {
            get;

            set;
        }
    }

    public static class OSD
    {
        public static void PostMessageLowerRightCorner (string text)
        {
            Debug.Log (text);
        }

        public static void PostMessageUpperCenter (string text)
        {
            Debug.Log (text);
        }
    }

    public static class UI
    {
        public static GameObject CreatePrimitive (PrimitiveType type, Color color, Vector3 scale, bool isActive, string name = "", string shader = "Diffuse")
        {
            GameObject obj = GameObject.CreatePrimitive (type);

            var renderer = obj.GetComponent<Renderer>();

            renderer.material.color = color;
            obj.transform.localScale = scale;

            obj.SetActive (isActive);

            obj.name = name;

            renderer.material.shader = Shader.Find (shader);

            obj.layer = 1;

            return obj;
        }

        public static Color GetColorFromRgb (byte r, byte g, byte b, byte a = 255)
        {
            var c = new Color (r / 255f, g / 255f, b / 255f, a / 255f);

            return c;
        }
    }
}
