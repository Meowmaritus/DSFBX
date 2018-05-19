using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX.Solvers
{
    public class NormalSolver
    {
        private readonly DSFBXImporter Importer;
        public NormalSolver(DSFBXImporter Importer)
        {
            this.Importer = Importer;
        }

        public List<Vector3> SolveNormals(List<ushort> vertexIndices, List<FlverVertex> vertices,
            List<Vector3> hqVertPositions,
            Action<string> onOutput, Action<string> onError)
        {
            int triangleCount = vertexIndices.Count / 3;
            Vector3[] triangleNormals = new Vector3[triangleCount];

            List<int>[] adjacentTriangleIndices = new List<int>[vertices.Count];

            List<Vector3> normals = new List<Vector3>();

            for (int i = 0; i < triangleCount; i++)
            {
                Vector3 vertPos1 = hqVertPositions[vertexIndices[(i * 3) + 0]];
                Vector3 vertPos2 = hqVertPositions[vertexIndices[(i * 3) + 1]];
                Vector3 vertPos3 = hqVertPositions[vertexIndices[(i * 3) + 2]];

                triangleNormals[i] = GetTriangleNormal(vertPos1, vertPos2, vertPos3);

                RegisterAdjacentTriangleIndex(adjacentTriangleIndices, vertexIndices[(i * 3) + 0], i);
                RegisterAdjacentTriangleIndex(adjacentTriangleIndices, vertexIndices[(i * 3) + 1], i);
                RegisterAdjacentTriangleIndex(adjacentTriangleIndices, vertexIndices[(i * 3) + 2], i);
            }

            

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertNormal = Vector3.Zero;

                //Add each adjacent triangle's normals
                foreach (var ati in adjacentTriangleIndices[i])
                {
                    vertNormal += triangleNormals[ati];
                }

                //Divide by number of adjacent triangle normals to get average
                vertNormal /= adjacentTriangleIndices[i].Count;

                var norm = Vector3.Normalize(vertNormal);

                vertices[i].Normal = norm;
                normals.Add(norm);
            }

            return normals;
        }

        private void RegisterAdjacentTriangleIndex(
            List<int>[] adjacentTriangleIndices, int vertIndex, int triangleIndex)
        {
            if (adjacentTriangleIndices[vertIndex] == null)
            {
                adjacentTriangleIndices[vertIndex] = new List<int>();
                adjacentTriangleIndices[vertIndex].Add(triangleIndex);
            }
            else
            {
                if (!adjacentTriangleIndices[vertIndex].Contains(triangleIndex))
                {
                    adjacentTriangleIndices[vertIndex].Add(triangleIndex);
                }
            }
        }

        private Vector3 GetTriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Normalize(Vector3.Cross(b - a, c - a));
        }

    }
}
