using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    public static class Utility
    {
        public static string GetFullName(MonoBehaviour go)
        {
            var full = $"/{go.name}";
            var parent = go.transform.parent;
            while (parent != null)
                full = $"/{parent.name}" + full;

            return full;
        }
    }
}
