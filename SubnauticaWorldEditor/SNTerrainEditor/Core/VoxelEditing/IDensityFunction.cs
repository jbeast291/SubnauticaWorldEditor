using Unity.Burst;
using Unity.Mathematics;

namespace SNTerrainEditor.Core.Editing;

[BurstCompile]
internal struct SphereDensityFunction : IDensityFunction {
    internal required float sqrScale;
    internal required float invSqrScale;
    
    [BurstCompile]
    public float Evaluate(int3 shapeCenter, int3 block) {
        float3 delta = block - shapeCenter;
        float sqrDist = math.lengthsq(delta);

        if (sqrDist >= sqrScale) return 0.0f;
        return 1.0f - (sqrDist * invSqrScale);
    }
}

[BurstCompile]
internal struct PyramidDensityFunction : IDensityFunction {
    internal required float height;
    internal required float baseHalfWidth;
    
    const float smoothingWidth = 1;//1 voxel wide
    
    [BurstCompile]
    public float Evaluate(int3 shapeCenter, int3 block) {
        float3 localPos = block - shapeCenter;
        float distance = EvaluateSdf(localPos, height, baseHalfWidth);
        float normalizedDist = distance / smoothingWidth;
        return math.saturate(0.5f - normalizedDist);
    }

    private static float EvaluateSdf(float3 p, float h, float r) {
        float r2 = r * r;
        float m2 = h * h + r2;
        
        float ax = math.abs(p.x) - r;
        float ay = math.abs(p.z) - r;

        if (ay > ax) {
            (ax, ay) = (ay, ax);
        }
        
        float px = ax;
        float py = p.y;
        float pz = ay;

        float qx = pz;
        float qy = h * py - r * px;
        float qz = h * px + r * py;

        float s = math.max(-qx, 0.0f);
        float t = math.clamp((qy - r * pz) / (m2 + r2), 0.0f, 1.0f);

        float a = m2 * (qx + s) * (qx + s) + qy * qy;
        float b = m2 * (qx + r * t) * (qx + r * t) + (qy - m2 * t) * (qy - m2 * t);

        float d2 = math.min(qy, -qx * m2 - qy * r) > 0.0f ? 0.0f : math.min(a, b);

        return math.sqrt((d2 + qz * qz) / m2) * math.sign(math.max(qz, -py));
    }
}

internal interface IDensityFunction {
    /// <summary>
    /// Returns the signed distance/density at position p with the given scale
    /// </summary>
    /// <returns>[0.0-1.0] The value 0.0 being fully outside, the value 1.0 fully inside. 0.5 being at the bountry </returns>
    internal float Evaluate(int3 shapeCenter, int3 block);
}