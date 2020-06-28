using System.IO;
using UnityEditor;
using UnityEngine;

namespace Biglab.Editor
{
    public class BlenderAssetProcessor : AssetPostprocessor
    {
        public void OnPostprocessModel(GameObject obj)
        {
            // Only perform corrections with blender files
            var importer = assetImporter as ModelImporter;
            if (importer != null && Path.GetExtension(importer.assetPath) == ".blend")
            {
                RotateObject(obj.transform);
            }

            // Don't know why we need this...
            // Fixes wrong parent rotation
            obj.transform.rotation = Quaternion.identity;
        }

        // Recursively rotate a object tree individualy
        private void RotateObject(Transform obj)
        {
            var objRotation = obj.eulerAngles;
            objRotation.x += 90f;
            obj.eulerAngles = objRotation;

            //if a meshFilter is attached, we rotate the vertex mesh data
            var meshFilter = obj.GetComponent(typeof(MeshFilter)) as MeshFilter;
            if (meshFilter)
            {
                RotateMesh(meshFilter.sharedMesh);
            }

            // Do this too for all our children
            // Casting is done to get rid of implicit downcast errors
            foreach (Transform child in obj)
            {
                RotateObject(child);
            }
        }

        // "rotate" the mesh data
        private void RotateMesh(Mesh mesh)
        {
            // Switch all vertex z values with y values
            var vertices = mesh.vertices;
            for (var index = 0; index < vertices.Length; index++)
            {
                vertices[index] = new Vector3(vertices[index].x, vertices[index].z, vertices[index].y);
            }

            mesh.vertices = vertices;

            // For each submesh, we invert the order of vertices for all triangles
            // For some reason changing the vertex positions flips all the normals???
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var triangles = mesh.GetTriangles(submesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    var intermediate = triangles[index];
                    triangles[index] = triangles[index + 2];
                    triangles[index + 2] = intermediate;
                }
                mesh.SetTriangles(triangles, submesh);
            }

            // Recalculate other relevant mesh data
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
