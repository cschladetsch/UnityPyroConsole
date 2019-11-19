using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    public static class Utility
    {
        public static string GetFullName(MonoBehaviour go)
        {
            if (!go)
                return string.Empty;
            
            var full = $"/{go.name}";
            var parent = go.transform.parent;
            while (parent)
            {
                full = $"/{parent.name}" + full;
                parent = parent.parent;
            }

            return full;
        }
    }
}
