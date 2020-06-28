using Biglab.Displays;
using Biglab.Tracking;
using UnityEditor;
using UnityEngine;

namespace Biglab.Editor
{
    internal static class Hierarchy
    {
        [MenuItem("GameObject/Biglab/Viewers/Perspective Viewer (Primary)", false, 0)]
        public static GameObject CreatePrimaryPerspectiveViewer() 
            => CreatePerspectiveViewerWithRole(ViewerRole.Primary);

        [MenuItem("GameObject/Biglab/Viewers/Perspective Viewer (Secondary)", false, 0)]
        public static GameObject CreateSecondaryPerspectiveViewer() 
            => CreatePerspectiveViewerWithRole(ViewerRole.Secondary);

        public static GameObject CreatePerspectiveViewerWithRole(ViewerRole role)
        {
            var go = new GameObject($"Viewer: {ObjectNames.NicifyVariableName($"{role}")}");

            var pv = go.AddComponent<Viewer>();
            pv.Role = role;

            var to = go.AddComponent<TrackedObject>();
        
            switch (role)
            {
                case ViewerRole.Primary:
                    to.ObjectIndex = 0;
                    break;

                case ViewerRole.Secondary:
                    to.ObjectIndex = 1;
                    break;
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            return go;
        }

        [MenuItem("GameObject/Biglab/Viewers/Perspective Viewer (Untracked)", false, 0)]
        public static GameObject CreatePerspectiveViewer()
        {
            var go = new GameObject("Viewer", typeof(Viewer));

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            return go;
        }

        [MenuItem("GameObject/Biglab/Tracked Object", false, 0)]
        public static GameObject CreateTrackedObject()
        {
            // 
            var go = new GameObject("Tracked Object");

            var to = go.AddComponent<TrackedObject>();
            to.ObjectKind = TrackedObjectKind.Object;
            to.ObjectIndex = 0;

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            return go;
        }

        [MenuItem("GameObject/Biglab/Volumetric Camera", false, 0)]
        public static GameObject CreateVolumetricCamera()
        {
            var go = new GameObject("Volumetric Camera", typeof(VolumetricCamera));

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            return go;
        }
    }
}
