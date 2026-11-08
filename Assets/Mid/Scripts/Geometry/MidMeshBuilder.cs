using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilentPavilion
{
    /// <summary>직접 작성한 표면 생성기. Primitive/외부 모델로 크리처를 대체하지 않는다.</summary>
    public sealed class MidMeshBuilder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();

        int Vertex(Vector3 p, Vector2 uv) { vertices.Add(p); uvs.Add(uv); return vertices.Count - 1; }
        void Triangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }

        // Catmull–Rom 보간: 제어점을 통과하는 곡선. 끝점은 반복해 끝에서의 외삽을 막는다.
        public static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            return .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t
                + (-a + 3 * b - 3 * c + d) * t * t * t);
        }

        // 필수 ①: 경로 방향 반복 × 단면 각도 반복으로 새로운 유기적 표면의 정점을 생성한다.
        // P(i,j) = C(t) + r(t)[cos(theta)N(t) + flatten*sin(theta)B(t)].
        // 이전 단면의 N을 새 접평면에 투영해 프레임이 갑자기 뒤집히는 것을 줄인다.
        public void AddTube(Vector3[] path, float[] radii, int steps = 28, int sides = 12,
            float flatten = 1f, float irregularity = .025f)
        {
            if (path == null || path.Length < 2 || radii.Length != path.Length)
                throw new System.ArgumentException("경로와 반지름 개수가 맞아야 합니다.");
            int first = vertices.Count;
            Vector3 previousNormal = Vector3.zero;
            for (int i = 0; i <= steps; i++)
            {
                float f = (float)i / steps * (path.Length - 1);
                int k = Mathf.Min(Mathf.FloorToInt(f), path.Length - 2);
                float t = f - k;
                Vector3 a = path[Mathf.Max(0, k - 1)], b = path[k];
                Vector3 c = path[k + 1], d = path[Mathf.Min(path.Length - 1, k + 2)];
                Vector3 center = CatmullRom(a, b, c, d, t);
                Vector3 tangent = (.5f * ((-a + c) + 2 * (2*a - 5*b + 4*c - d)*t
                    + 3 * (-a + 3*b - 3*c + d)*t*t)).normalized;
                if (tangent.sqrMagnitude < .01f) tangent = (c - b).normalized;
                Vector3 n = Vector3.ProjectOnPlane(previousNormal, tangent).normalized;
                if (n.sqrMagnitude < .01f)
                    n = Vector3.Cross(tangent, Mathf.Abs(tangent.y) < .9f ? Vector3.up : Vector3.right).normalized;
                Vector3 binormal = Vector3.Cross(tangent, n).normalized;
                previousNormal = n;
                float radius = Mathf.Lerp(radii[k], radii[k + 1], Mathf.SmoothStep(0, 1, t));
                for (int j = 0; j <= sides; j++)
                {
                    float angle = j * 2 * Mathf.PI / sides;
                    float organic = 1 + irregularity * Mathf.Sin(angle * 3 + f * 4);
                    Vertex(center + radius * organic * (Mathf.Cos(angle)*n + Mathf.Sin(angle)*flatten*binormal),
                        new Vector2((float)j / sides, (float)i / steps * 3));
                }
            }
            for (int i = 0; i < steps; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = first + i * (sides + 1) + j, b = a + 1, c = a + sides + 1;
                    Triangle(a, b, c); Triangle(b, c + 1, c);
                }
            int start = Vertex(path[0], Vector2.zero), end = Vertex(path[path.Length - 1], Vector2.one);
            for (int j = 0; j < sides; j++)
            {
                Triangle(start, first + j + 1, first + j);
                int last = first + steps * (sides + 1);
                Triangle(end, last + j, last + j + 1);
            }
        }

        // 머리와 관절도 구 Primitive 대신 위도·경도에서 생성. 극점은 삼각형 부채꼴로 닫는다.
        public void AddEllipsoid(Vector3 center, Vector3 scale, int rings = 14, int sides = 20, float asymmetry = 0)
        {
            int top = Vertex(center + Vector3.up * scale.y, new Vector2(.5f, 1));
            int first = vertices.Count;
            for (int i = 1; i < rings; i++)
            {
                float v = (float)i / rings, phi = Mathf.PI * v;
                for (int j = 0; j <= sides; j++)
                {
                    float u = (float)j / sides, theta = 2 * Mathf.PI * u;
                    Vector3 p = new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi)*Mathf.Sin(theta));
                    p.x += asymmetry * Mathf.Sin(phi) * Mathf.Sin(phi * 3);
                    // 얼굴 앞쪽(-Z)은 둥근 공 대신 살짝 눌린 비대칭 표면.
                    if (p.z < -.35f) p.z = -.35f + (p.z + .35f) * .68f;
                    Vertex(center + Vector3.Scale(p, scale), new Vector2(u, 1-v));
                }
            }
            int bottom = Vertex(center - Vector3.up * scale.y, new Vector2(.5f, 0));
            for (int j = 0; j < sides; j++)
            {
                Triangle(top, first + j + 1, first + j);
                int last = first + (rings - 2) * (sides + 1);
                Triangle(bottom, last + j, last + j + 1);
            }
            for (int i = 0; i < rings - 2; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = first + i*(sides+1)+j, b = a+1, c = a+sides+1;
                    Triangle(a, b, c); Triangle(b, c+1, c);
                }
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
