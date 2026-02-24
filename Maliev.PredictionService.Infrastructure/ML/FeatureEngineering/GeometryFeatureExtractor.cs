using System.Numerics;

namespace Maliev.PredictionService.Infrastructure.ML.FeatureEngineering;

/// <summary>
/// Extracts geometric features from 3D models in STL binary format for ML prediction.
/// Features include volume, surface area, layer count, support structure percentage,
/// complexity score, and bounding box dimensions.
/// </summary>
public class GeometryFeatureExtractor
{
    private const float LayerHeight = 0.2f; // Default layer height in mm

    /// <summary>
    /// Represents extracted geometric features from a 3D model.
    /// </summary>
    public record GeometryFeatures
    {
        public required float Volume { get; init; } // mm³
        public required float SurfaceArea { get; init; } // mm²
        public required int LayerCount { get; init; }
        public required float SupportPercentage { get; init; } // 0-100
        public required float ComplexityScore { get; init; } // 0-100
        public required Vector3 BoundingBoxMin { get; init; }
        public required Vector3 BoundingBoxMax { get; init; }
        public required float BoundingBoxWidth { get; init; } // mm
        public required float BoundingBoxDepth { get; init; } // mm
        public required float BoundingBoxHeight { get; init; } // mm
    }

    /// <summary>
    /// Extracts geometric features from STL binary file.
    /// </summary>
    /// <param name="stlFileStream">Stream containing STL binary data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted geometry features.</returns>
    /// <exception cref="InvalidDataException">Thrown when STL file format is invalid.</exception>
    public async Task<GeometryFeatures> ExtractFeaturesAsync(Stream stlFileStream, CancellationToken cancellationToken = default)
    {
        var triangles = await ParseStlBinaryAsync(stlFileStream, cancellationToken);

        if (triangles.Count == 0)
        {
            throw new InvalidDataException("STL file contains no triangles");
        }

        var volume = CalculateVolume(triangles);
        var surfaceArea = CalculateSurfaceArea(triangles);
        var (boundingBoxMin, boundingBoxMax) = CalculateBoundingBox(triangles);
        var layerCount = CalculateLayerCount(boundingBoxMin.Z, boundingBoxMax.Z, LayerHeight);
        var supportPercentage = EstimateSupportPercentage(triangles);
        var complexityScore = CalculateComplexityScore(triangles, surfaceArea, volume);

        var boundingBoxWidth = boundingBoxMax.X - boundingBoxMin.X;
        var boundingBoxDepth = boundingBoxMax.Y - boundingBoxMin.Y;
        var boundingBoxHeight = boundingBoxMax.Z - boundingBoxMin.Z;

        return new GeometryFeatures
        {
            Volume = volume,
            SurfaceArea = surfaceArea,
            LayerCount = layerCount,
            SupportPercentage = supportPercentage,
            ComplexityScore = complexityScore,
            BoundingBoxMin = boundingBoxMin,
            BoundingBoxMax = boundingBoxMax,
            BoundingBoxWidth = boundingBoxWidth,
            BoundingBoxDepth = boundingBoxDepth,
            BoundingBoxHeight = boundingBoxHeight
        };
    }

    /// <summary>
    /// Parses STL binary file format and extracts triangle meshes.
    /// STL Binary Format:
    /// - 80 bytes: Header (ignored)
    /// - 4 bytes: Number of triangles (uint32, little-endian)
    /// - For each triangle (50 bytes each):
    ///   - 12 bytes: Normal vector (3 floats, 4 bytes each)
    ///   - 12 bytes: Vertex 1 (3 floats)
    ///   - 12 bytes: Vertex 2 (3 floats)
    ///   - 12 bytes: Vertex 3 (3 floats)
    ///   - 2 bytes: Attribute byte count (ignored)
    /// </summary>
    private static async Task<List<Triangle>> ParseStlBinaryAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);

        // Skip 80-byte header
        var header = reader.ReadBytes(80);
        if (header.Length < 80)
        {
            throw new InvalidDataException("Invalid STL file: header too short");
        }

        // Read triangle count
        var triangleCount = reader.ReadUInt32();
        if (triangleCount == 0 || triangleCount > 10_000_000) // Sanity check
        {
            throw new InvalidDataException($"Invalid triangle count: {triangleCount}");
        }

        var triangles = new List<Triangle>((int)triangleCount);

        for (uint i = 0; i < triangleCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read normal vector (12 bytes) - not used in current implementation
            var normalX = reader.ReadSingle();
            var normalY = reader.ReadSingle();
            var normalZ = reader.ReadSingle();
            var normal = new Vector3(normalX, normalY, normalZ);

            // Read three vertices (36 bytes total)
            var v1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var v2 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var v3 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            // Skip attribute byte count (2 bytes)
            reader.ReadUInt16();

            triangles.Add(new Triangle(v1, v2, v3, normal));
        }

        return triangles;
    }

    /// <summary>
    /// Calculates the volume of the 3D mesh using divergence theorem (signed tetrahedron method).
    /// </summary>
    private static float CalculateVolume(List<Triangle> triangles)
    {
        float signedVolume = 0;

        foreach (var triangle in triangles)
        {
            // Signed volume of tetrahedron formed by origin and triangle
            // V = (1/6) * dot(v1, cross(v2, v3))
            var cross = Vector3.Cross(triangle.V2, triangle.V3);
            var dot = Vector3.Dot(triangle.V1, cross);
            signedVolume += dot;
        }

        return Math.Abs(signedVolume / 6.0f);
    }

    /// <summary>
    /// Calculates the total surface area of the mesh.
    /// </summary>
    private static float CalculateSurfaceArea(List<Triangle> triangles)
    {
        float totalArea = 0;

        foreach (var triangle in triangles)
        {
            // Area = 0.5 * |cross(v2-v1, v3-v1)|
            var edge1 = triangle.V2 - triangle.V1;
            var edge2 = triangle.V3 - triangle.V1;
            var cross = Vector3.Cross(edge1, edge2);
            var area = cross.Length() * 0.5f;
            totalArea += area;
        }

        return totalArea;
    }

    /// <summary>
    /// Calculates axis-aligned bounding box.
    /// </summary>
    private static (Vector3 Min, Vector3 Max) CalculateBoundingBox(List<Triangle> triangles)
    {
        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var triangle in triangles)
        {
            min = Vector3.Min(min, Vector3.Min(triangle.V1, Vector3.Min(triangle.V2, triangle.V3)));
            max = Vector3.Max(max, Vector3.Max(triangle.V1, Vector3.Max(triangle.V2, triangle.V3)));
        }

        return (min, max);
    }

    /// <summary>
    /// Calculates layer count based on Z-axis height and layer height.
    /// </summary>
    private static int CalculateLayerCount(float minZ, float maxZ, float layerHeight)
    {
        var height = maxZ - minZ;
        return (int)Math.Ceiling(height / layerHeight);
    }

    /// <summary>
    /// Estimates percentage of geometry requiring support structures.
    /// Uses heuristic: triangles with normal Z-component less than -0.5 need support.
    /// </summary>
    private static float EstimateSupportPercentage(List<Triangle> triangles)
    {
        var overhangsCount = triangles.Count(t => t.Normal.Z < -0.5f);
        return (overhangsCount / (float)triangles.Count) * 100f;
    }

    /// <summary>
    /// Calculates complexity score (0-100) based on surface-to-volume ratio and triangle density.
    /// Higher score = more complex geometry requiring longer print times.
    /// </summary>
    private static float CalculateComplexityScore(List<Triangle> triangles, float surfaceArea, float volume)
    {
        if (volume <= 0) return 0;

        // Surface-to-volume ratio (normalized)
        var svRatio = surfaceArea / (float)Math.Pow(volume, 2.0 / 3.0);

        // Triangle density (triangles per mm³)
        var triangleDensity = triangles.Count / volume;

        // Combine metrics (weighted average)
        var normalizedSvRatio = Math.Min(svRatio / 10.0f, 1.0f); // Normalize to 0-1
        var normalizedDensity = Math.Min(triangleDensity / 0.01f, 1.0f); // Normalize to 0-1

        var complexityScore = (normalizedSvRatio * 0.6f + normalizedDensity * 0.4f) * 100f;

        return Math.Clamp(complexityScore, 0, 100);
    }

    /// <summary>
    /// Represents a triangle in 3D space.
    /// </summary>
    private record Triangle(Vector3 V1, Vector3 V2, Vector3 V3, Vector3 Normal);
}
