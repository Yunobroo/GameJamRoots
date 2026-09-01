using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ProceduralRoot : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private int radialSegments = 10;
    [SerializeField] private float baseThickness = 0.4f;
    [SerializeField] private float tipThickness = 0.08f;

    [Header("Taper")]
    [SerializeField] private float taperPower = 1.5f;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private readonly List<Vector3> points =
        new List<Vector3>();

    public int PointCount => points.Count;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = new Mesh();
        mesh.name = "Procedural Root";

        meshFilter.sharedMesh = mesh;

        meshCollider.convex = false;
        meshCollider.isTrigger = false;
        meshCollider.sharedMesh = null;
    }

    public void AddPoint(Vector3 worldPoint)
    {
        Vector3 localPoint =
            transform.InverseTransformPoint(worldPoint);

        points.Add(localPoint);

        RebuildMesh();
    }

    public void SetThickness(
        float newBaseThickness,
        float newTipThickness)
    {
        baseThickness = newBaseThickness;
        tipThickness = newTipThickness;

        RebuildMesh();
    }

    public Vector3 GetWorldPoint(int index)
    {
        if (points.Count == 0)
        {
            return transform.position;
        }

        index = Mathf.Clamp(
            index,
            0,
            points.Count - 1
        );

        return transform.TransformPoint(
            points[index]
        );
    }

    public Vector3 GetClosestPathPoint(
        Vector3 worldPosition)
    {
        if (points.Count == 0)
        {
            return transform.position;
        }

        if (points.Count == 1)
        {
            return transform.TransformPoint(
                points[0]
            );
        }

        Vector3 bestPoint =
            GetWorldPoint(0);

        float bestDistance =
            Mathf.Infinity;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start =
                GetWorldPoint(i);

            Vector3 end =
                GetWorldPoint(i + 1);

            Vector3 closestPoint =
                ClosestPointOnSegment(
                    worldPosition,
                    start,
                    end
                );

            float distance =
                (worldPosition - closestPoint)
                .sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = closestPoint;
            }
        }

        return bestPoint;
    }

    private Vector3 ClosestPointOnSegment(
        Vector3 point,
        Vector3 start,
        Vector3 end)
    {
        Vector3 segment =
            end - start;

        float lengthSquared =
            segment.sqrMagnitude;

        if (lengthSquared <= Mathf.Epsilon)
        {
            return start;
        }

        float t =
            Vector3.Dot(
                point - start,
                segment
            ) /
            lengthSquared;

        t = Mathf.Clamp01(t);

        return start +
            segment * t;
    }

    private void RebuildMesh()
    {
        if (points.Count < 2)
            return;

        List<Vector3> vertices =
            new List<Vector3>();

        List<int> triangles =
            new List<int>();

        List<Vector2> uvs =
            new List<Vector2>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward;

            if (i == 0)
            {
                forward =
                    points[i + 1] -
                    points[i];
            }
            else if (i == points.Count - 1)
            {
                forward =
                    points[i] -
                    points[i - 1];
            }
            else
            {
                forward =
                    points[i + 1] -
                    points[i - 1];
            }

            forward.Normalize();

            Vector3 referenceUp =
                Vector3.up;

            if (
                Mathf.Abs(
                    Vector3.Dot(
                        forward,
                        referenceUp
                    )
                ) > 0.95f
            )
            {
                referenceUp =
                    Vector3.right;
            }

            Vector3 right =
                Vector3.Cross(
                    forward,
                    referenceUp
                ).normalized;

            Vector3 up =
                Vector3.Cross(
                    right,
                    forward
                ).normalized;

            float progress =
                (float)i /
                (points.Count - 1);

            float taperedProgress =
                Mathf.Pow(
                    progress,
                    taperPower
                );

            float thickness =
                Mathf.Lerp(
                    baseThickness,
                    tipThickness,
                    taperedProgress
                );

            for (
                int j = 0;
                j < radialSegments;
                j++
            )
            {
                float angle =
                    (float)j /
                    radialSegments *
                    Mathf.PI *
                    2f;

                Vector3 offset =
                    right *
                    Mathf.Cos(angle) *
                    thickness +
                    up *
                    Mathf.Sin(angle) *
                    thickness;

                vertices.Add(
                    points[i] + offset
                );

                uvs.Add(
                    new Vector2(
                        (float)j /
                        radialSegments,
                        progress
                    )
                );
            }
        }

        for (
            int i = 0;
            i < points.Count - 1;
            i++
        )
        {
            for (
                int j = 0;
                j < radialSegments;
                j++
            )
            {
                int current =
                    i * radialSegments + j;

                int next =
                    i * radialSegments +
                    (j + 1) %
                    radialSegments;

                int currentAbove =
                    (i + 1) *
                    radialSegments + j;

                int nextAbove =
                    (i + 1) *
                    radialSegments +
                    (j + 1) %
                    radialSegments;

                triangles.Add(current);
                triangles.Add(currentAbove);
                triangles.Add(next);

                triangles.Add(next);
                triangles.Add(currentAbove);
                triangles.Add(nextAbove);
            }
        }

        mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.SetUVs(
            0,
            uvs
        );

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}