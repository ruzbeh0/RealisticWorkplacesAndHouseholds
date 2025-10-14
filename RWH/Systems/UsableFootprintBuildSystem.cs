using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Components;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Game.Tools.ValidationSystem;

#nullable enable
namespace RWH.Systems
{
    /// <summary>
    /// Builds a per-PREFAB UsableFootprintFactor (UFF) by comparing:
    ///   - union of sub-area polygon areas (XZ)  vs
    ///   - XZ area of the prefab's aggregated mesh bounds (bbox)
    ///
    /// Notes:
    /// - SubArea polygons are referenced by SubArea.m_NodeRange into the *prefab's* SubAreaNode buffer.
    /// - If no non-surface areas exist, we include surfaces as a fallback.
    /// - If no geometry is available, factor defaults to 1.
    /// - Run this once during PrefabUpdate; later jobs can read the factor.
    /// </summary>
    //[BurstCompile]
    public partial class UsableFootprintBuildSystem : GameSystemBase
    {
        private EntityQuery _prefabQuery;

        private BufferLookup<SubMesh> _subMeshes;
        private ComponentLookup<MeshData> _meshData;

        private BufferLookup<Game.Prefabs.SubArea> _prefabSubAreas;
        private BufferLookup<SubAreaNode> _prefabSubAreaNodes;
        private ComponentLookup<AreaGeometryData> _prefabAreaGeometry;
        private bool _builtOnce;
        private PrefabSystem _prefabSystem;

        // Setting.cs (suggested defaults)
        public int footprint_min_vertices = 12;   // geometry must have at least this many points
        public float footprint_hull_fill_gate = 0.92f;// hull must cover >=92% of the bbox
        public float footprint_concavity_strength = 0.80f;// how hard to pull toward rHull for concave shapes
        public float footprint_concavity_gate = 0.80f;// only penalize when rHull < 0.90
        public float footprint_concavity_gamma = 1.8f; // nonlinearity; >1 spares mild concavity
        public float footprint_strength = 0.20f;// base soften from rRaw (keep this mild)
        public float footprint_deadzone = 0.10f;// ignore tiny deficits in rRaw
        public float footprint_min = 0.35f;// never go below this (global floor)
        public float footprint_points_bbox_coverage_gate = 0.85f; // pointsBBox / assetBBox must be >= 0.85
        public float footprint_hull_fill_points_gate = 0.8f; // hullArea / pointsBBox must be >= 0.92
        public float footprint_bbox_similarity_gate = 0.50f; // 0..1, higher = stricter
        public bool shape_penalty_enabled = true;
        public float shape_min_hull_gate = 0.70f;   // only apply when rHull < gate (clearly concave)
        public float shape_penalty_strength = 0.35f;   // 0..1, how much to reduce when triggered
        public float shape_penalty_gamma = 0.65f;   // <1 = stronger; >1 = gentler ramp
        public float shape_rect_norm = 0.785398163f; // π/4; square’s IQ → 1.0 after normalization




        protected override void OnCreate()
        {
            base.OnCreate();

            // Prefab-scoped buildings (works for spawnables & services)
            _prefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<BuildingData>(),
                    ComponentType.ReadOnly<SubMesh>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                }
            });

            RequireForUpdate(_prefabQuery);

            _prefabSystem = this.World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnUpdate()
        {
            if (_builtOnce)
                return;

            // Defer until prefabs are actually available in this phase
            int count = _prefabQuery.CalculateEntityCount();
            if (count == 0)
                return;

            // Refresh lookups each frame
            _subMeshes = GetBufferLookup<SubMesh>(true);
            _meshData = GetComponentLookup<MeshData>(true);

            _prefabSubAreas = GetBufferLookup<Game.Prefabs.SubArea>(true);
            _prefabSubAreaNodes = GetBufferLookup<SubAreaNode>(true);
            _prefabAreaGeometry = GetComponentLookup<AreaGeometryData>(true);

            var prefabs = _prefabQuery.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Mod.log.Info($"UsableFootprintBuildSystem: Computing UFF for {prefabs.Length} prefabs...");
            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];

                float uff = ComputeUsableFootprintFactor(prefab);

                if (uff < 1f)
                {
                    if (EntityManager.HasComponent<UsableFootprintFactor>(prefab))
                        ecb.SetComponent(prefab, new UsableFootprintFactor { Value = uff });
                    else
                        ecb.AddComponent(prefab, new UsableFootprintFactor { Value = uff });
                }
                
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            prefabs.Dispose();

            _builtOnce = true;
            // Stop running forever after the one-time build
            Enabled = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ComputeUsableFootprintFactor(Entity prefab)
        {
            // ---------- 0) Asset bbox area from meshes ----------
            float bboxArea = 0f;
            if (_subMeshes.HasBuffer(prefab))
            {
                var dims = BuildingUtils.GetBuildingDimensions(_subMeshes[prefab], _meshData);
                float3 size = ObjectUtils.GetSize(dims);
                bboxArea = math.max(0.0001f, size.x * size.z);
            }
            if (bboxArea <= 0f)
                return 1f; // no geometry → neutral

            // ---------- 1) Footprint polygon area (union of subareas) ----------
            float polyArea = SumSubAreaPolygons(prefab, includeSurface: false);
            if (polyArea <= 0.0001f)
                polyArea = SumSubAreaPolygons(prefab, includeSurface: true);
            if (polyArea <= 0.0001f)
                return 1f;

            float rRaw = math.saturate(polyArea / bboxArea);

            // ---------- 2) Collect points, their bbox, and convex hull ----------
            var pts = CollectAllRingPoints(prefab, includeSurface: true, Allocator.Temp);
            float pointsBBoxArea = BoundsAreaFromPoints(pts);

            float hullArea = 0f;
            float rHull = rRaw;
            float hullFillPoints = 0f;
            NativeList<int> hullIdx = default;

            if (pts.Length >= 3 && pointsBBoxArea > 0f)
            {
                hullIdx = ConvexHull(pts, Allocator.Temp);
                hullArea = PolygonArea(pts, hullIdx);
                if (hullArea > 0f)
                {
                    rHull = math.saturate(polyArea / hullArea);                 // solidity (poly vs hull)
                    hullFillPoints = math.saturate(hullArea / pointsBBoxArea);  // hull vs its own points-bbox
                }
            }

            // ---------- 3) Bbox similarity (asset vs points) ----------
            float bboxSimilarity = 0f;
            if (bboxArea > 0f && pointsBBoxArea > 0f)
            {
                float a = math.min(bboxArea, pointsBBoxArea);
                float b = math.max(bboxArea, pointsBBoxArea);
                bboxSimilarity = a / b; // 0..1
            }

            // ---------- 4) Trust gate ----------
            int minVertices = math.max(10, footprint_min_vertices);
            bool trusted = pts.Length >= minVertices;
            //Mod.log?.Info($"Prefab {prefab.Index}: pts={pts.Length}, minReq={minVertices}, prelimTrusted={trusted}");
            trusted &= hullFillPoints >= math.saturate(footprint_hull_fill_points_gate);
            //Mod.log.Info($"Prefab {prefab.Index}: hullFillPts={hullFillPoints:F3}, hullFillGate={footprint_hull_fill_points_gate}, trusted={trusted}");
            trusted &= bboxSimilarity >= math.saturate(footprint_bbox_similarity_gate);
            //Mod.log.Info($"Prefab {prefab.Index}: bboxSim={bboxSimilarity:F3}, bboxSimGate={footprint_bbox_similarity_gate}, trusted={trusted}");
            if (rRaw < 0.35f) // stricter when the raw rect-fill is tiny (common for metadata misses)
                trusted &= (bboxSimilarity >= footprint_bbox_similarity_gate);
            //Mod.log?.Info(
            //    $"Prefab {prefab.Index}: polyArea={polyArea:F2}, bboxArea={bboxArea:F2}, rRaw={rRaw:F3}, pts={pts.Length}, hullFillPts={hullFillPoints:F3}, bboxSim={bboxSimilarity:F3}, trusted={trusted}"
            //);
            if (!trusted)
            {
                if (hullIdx.IsCreated) hullIdx.Dispose();
                pts.Dispose();
                return 1f;
            }

            // ---------- 5) Concavity blend toward rHull ----------
            float concGate = math.saturate(footprint_concavity_gate);          // e.g., 0.80
            float concStrength = math.clamp(footprint_concavity_strength, 0f, 1f); // e.g., 0.80
            float concGamma = math.max(1f, footprint_concavity_gamma);          // e.g., 1.8
            float t = (rHull < concGate) ? math.saturate((concGate - rHull) / concGate) : 0f;
            float k = math.saturate(concStrength * math.pow(t, concGamma));
            float hullPenalty = math.lerp(1f, rHull, k); // lerp(1 → rHull)

            // ---------- 6) Gentle soften based on raw fill ----------
            float strength = math.clamp(footprint_strength, 0f, 1f);   // e.g., 0.20
            float deadzone = math.saturate(footprint_deadzone);        // e.g., 0.10
            float deficit = math.max(0f, 1f - rRaw - deadzone);
            float softened = 1f - (deficit * strength);

            // ---------- 7) Courtyard/U-shape detector (grid sampling in the hull) ----------
            // Detect large "empty" region inside the convex hull that the footprint doesn't occupy.
            // This catches open courtyards (U/C shapes) even when rings don't outline the void.
            float uMult = 1f;
            if (hullIdx.IsCreated && hullIdx.Length >= 3)
            {
                // Build a small axis-aligned bbox of the hull for sampling.
                float2 minXZ = new float2(float.MaxValue, float.MaxValue);
                float2 maxXZ = new float2(float.MinValue, float.MinValue);
                for (int i = 0; i < hullIdx.Length; i++)
                {
                    float2 p = pts[hullIdx[i]];
                    minXZ = math.min(minXZ, p);
                    maxXZ = math.max(maxXZ, p);
                }

                // Local helpers (no allocations)
                static bool PointInConvexHull(float2 p, NativeArray<float2> allPts, NativeList<int> idx)
                {
                    int n = idx.Length;
                    float2 prev = allPts[idx[n - 1]];
                    for (int i = 0; i < n; i++)
                    {
                        float2 cur = allPts[idx[i]];
                        float2 e = cur - prev;
                        float2 vp = p - prev;
                        // Left-of test for CCW hull; if any edge has negative cross, point is outside.
                        if ((e.x * vp.y - e.y * vp.x) < 0f)
                            return false;
                        prev = cur;
                    }
                    return true;
                }

                bool PointInAnyFootprint(float2 p)
                {
                    if (!_prefabSubAreas.HasBuffer(prefab) || !_prefabSubAreaNodes.HasBuffer(prefab))
                        return false;

                    var subs = _prefabSubAreas[prefab];
                    var nodes = _prefabSubAreaNodes[prefab];

                    // Classic ray-cast inside test per ring.
                    bool insideAny = false;
                    for (int si = 0; si < subs.Length; si++)
                    {
                        var sa = subs[si];
                        // We include all subarea types (surfaces + non-surfaces) to approximate the footprint union.
                        var range = sa.m_NodeRange;
                        int count = range.y - range.x + 1;
                        if (count < 3) continue;

                        int first = ObjectToolBaseSystem.GetFirstNodeIndex(nodes, range);

                        bool inside = false;
                        float2 pj = new float2(nodes[first + (count - 1)].m_Position.x,
                                               nodes[first + (count - 1)].m_Position.z);
                        for (int k2 = 0; k2 < count; k2++)
                        {
                            float2 pi = new float2(nodes[first + k2].m_Position.x,
                                                   nodes[first + k2].m_Position.z);

                            bool intersect = ((pi.y > p.y) != (pj.y > p.y)) &&
                                             (p.x < (pj.x - pi.x) * (p.y - pi.y) / math.max(1e-6f, (pj.y - pi.y)) + pi.x);
                            if (intersect) inside = !inside;
                            pj = pi;
                        }
                        if (inside) { insideAny = true; break; }
                    }
                    return insideAny;
                }

                // Sample a coarse grid in the hull bbox.
                const int N = 16; // 16×16 ~ 256 tests: cheap in PrefabUpdate
                int hullSamples = 0;
                int emptyInHull = 0;

                float2 span = maxXZ - minXZ;
                float2 step = span / N;

                // If the hull bbox is degenerate, skip.
                if (step.x > 1e-4f && step.y > 1e-4f)
                {
                    for (int iy = 0; iy < N; iy++)
                        for (int ix = 0; ix < N; ix++)
                        {
                            float2 p = minXZ + (new float2(ix + 0.5f, iy + 0.5f)) * step;
                            if (!PointInConvexHull(p, pts, hullIdx)) continue;

                            hullSamples++;
                            if (!PointInAnyFootprint(p))
                                emptyInHull++;
                        }
                }

                if (hullSamples > 0)
                {
                    float voidRatio = math.saturate((float)emptyInHull / hullSamples); // 0..1
                                                                                       // Gate to avoid false positives on tiny/skinny assets.
                    bool looksLikeCourtyard =
                        bboxSimilarity >= 0.95f &&
                        hullFillPoints >= 0.95f &&
                        rHull < 0.85f &&
                        voidRatio >= 0.25f; // at least a quarter of hull area is empty

                    if (looksLikeCourtyard)
                    {
                        // Strong penalty for obvious U/C shapes.
                        // Tuneable constants; aggressive but bounded.
                        const float courStrength = 0.85f;  // 0..1
                        const float courGamma = 1.10f;  // shape aggressiveness
                        float uScore = math.pow(voidRatio, courGamma);
                        uMult = 1f - courStrength * uScore; // multiplicative reducer
                        uMult = math.clamp(uMult, 0.1f, 1f); // never below 0.1 just in case
                    }
                }
            }

            float uff = softened * hullPenalty * uMult;

            // ---------- cache for log BEFORE disposing natives ----------
            int ptsCountCached = pts.IsCreated ? pts.Length : 0;
            float hullPenaltyCached = hullPenalty;
            float hullFillPointsCached = hullFillPoints;
            float bboxSimilarityCached = bboxSimilarity;
            float rRawCached = rRaw;
            float rHullCached = rHull;
            float uMultCached = uMult;

            if (hullIdx.IsCreated) hullIdx.Dispose();
            pts.Dispose();

            //Mod.log?.Info(
            //    $"  Prefab {prefab.Index}: polyArea={polyArea:F2}, rRaw={rRawCached:F3}, rHull={rHullCached:F3}, " +
            //    $"hullFillPts={hullFillPointsCached:F3}, bboxSim={bboxSimilarityCached:F3}, pts={ptsCountCached}, uMult={uMultCached:F3} -> UFF={math.clamp(uff, math.clamp(footprint_min, 0f, 1f), 1f):F3}"
            //);

            //var pName = GetPrefabNameSafe(prefab);
            //Mod.log?.Info(
            //    $"  Prefab {prefab.Index} ({pName}): UFF={math.clamp(uff, math.clamp(footprint_min, 0f, 1f), 1f):F3}"
            //);


            return math.clamp(uff, math.clamp(footprint_min, 0f, 1f), 1f);
        }

        private string GetPrefabNameSafe(Entity prefab)
        {
            try
            {
                if (EntityManager.Exists(prefab))
                {
                    // 1) PrefabSystem name (works in player builds)
                    if (_prefabSystem != null)
                    {
                        FixedString64Bytes fs = _prefabSystem.GetPrefabName(prefab);
                        if (fs.Length > 0)
                            return fs.ToString();
                    }

                    // 2) DOTS debug name (editor/dev builds)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var dbg = EntityManager.GetName(prefab);
            if (!string.IsNullOrEmpty(dbg))
                return dbg;
#endif
                }
            }
            catch { /* ignore */ }

            // 3) Fallback
            return $"Prefab {prefab.Index}";
        }

        private static float BoundsAreaFromPoints(NativeList<float2> pts)
        {
            if (pts.Length < 3) return 0f;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < pts.Length; i++)
            {
                var p = pts[i];
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
            float dx = math.max(0f, maxX - minX);
            float dy = math.max(0f, maxY - minY);
            return dx * dy;
        }


        // Collect all included ring points into a single list (XZ). We include surface rings here;
        // penalty blending will protect regular rectangles.
        private NativeList<float2> CollectAllRingPoints(Entity prefab, bool includeSurface, Allocator alloc)
        {
            var pts = new NativeList<float2>(128, alloc);
            if (!_prefabSubAreas.HasBuffer(prefab) || !_prefabSubAreaNodes.HasBuffer(prefab))
                return pts;

            var subs = _prefabSubAreas[prefab];
            var nodes = _prefabSubAreaNodes[prefab];

            for (int i = 0; i < subs.Length; i++)
            {
                var sa = subs[i];
                if (_prefabAreaGeometry.TryGetComponent(sa.m_Prefab, out var ag))
                {
                    if (!includeSurface && ag.m_Type == Game.Areas.AreaType.Surface)
                        continue;
                }
                var range = sa.m_NodeRange;
                if (range.y - range.x + 1 < 3) continue;
                for (int idx = range.x; idx <= range.y; idx++)
                {
                    var p = nodes[idx].m_Position;
                    pts.Add(new float2(p.x, p.z));
                }
            }
            return pts;
        }

        // Monotone chain convex hull (returns indices into 'points' in CCW order, no duplicate last point)
        private NativeList<int> ConvexHull(NativeList<float2> points, Allocator alloc)
        {
            var n = points.Length;
            var idx = new NativeArray<int>(n, alloc);
            for (int i = 0; i < n; i++) idx[i] = i;

            // sort by x, then y
            idx.Sort(new IndexComparer(points));

            var H = new NativeList<int>(2 * n, alloc);
            // lower hull
            for (int t = 0; t < n; t++)
            {
                int i = idx[t];
                while (H.Length >= 2)
                {
                    var a = points[H[H.Length - 2]];
                    var b = points[H[H.Length - 1]];
                    var c = points[i];
                    if (Cross(a, b, c) <= 0) H.RemoveAt(H.Length - 1);
                    else break;
                }
                H.Add(i);
            }
            // upper hull
            int lowerCount = H.Length;
            for (int t = n - 1; t >= 0; t--)
            {
                int i = idx[t];
                while (H.Length > lowerCount)
                {
                    var a = points[H[H.Length - 2]];
                    var b = points[H[H.Length - 1]];
                    var c = points[i];
                    if (Cross(a, b, c) <= 0) H.RemoveAt(H.Length - 1);
                    else break;
                }
                H.Add(i);
            }
            if (H.Length > 0) H.RemoveAt(H.Length - 1); // remove duplicate start
            idx.Dispose();
            return H;
        }

        static float Cross(float2 a, float2 b, float2 c)
        {
            float2 ab = b - a, ac = c - a;
            return ab.x * ac.y - ab.y * ac.x;
        }

        struct IndexComparer : IComparer<int>
        {
            NativeList<float2> pts;
            public IndexComparer(NativeList<float2> p) { pts = p; }
            public int Compare(int i, int j)
            {
                var a = pts[i]; var b = pts[j];
                if (a.x < b.x) return -1; if (a.x > b.x) return 1;
                if (a.y < b.y) return -1; if (a.y > b.y) return 1;
                return i - j;
            }
        }

        // Shoelace area for a polygon given vertex indices into 'points'
        private static float PolygonArea(NativeList<float2> points, NativeList<int> poly)
        {
            int n = poly.Length; if (n < 3) return 0f;
            double acc = 0d;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var a = points[poly[j]];
                var b = points[poly[i]];
                acc += (double)a.x * b.y - (double)b.x * a.y;
            }
            return math.abs((float)(acc * 0.5));
        }


        private int CountIncludedRings(Entity prefab, bool includeSurface)
        {
            if (!_prefabSubAreas.HasBuffer(prefab) || !_prefabSubAreaNodes.HasBuffer(prefab))
                return 0;

            var subAreas = _prefabSubAreas[prefab];
            int rings = 0;
            for (int i = 0; i < subAreas.Length; i++)
            {
                var sa = subAreas[i];
                if (_prefabAreaGeometry.TryGetComponent(sa.m_Prefab, out var ag))
                {
                    if (!includeSurface && ag.m_Type == Game.Areas.AreaType.Surface)
                        continue;
                }
                var range = sa.m_NodeRange;
                if (range.y - range.x + 1 >= 3) rings++;
            }
            return rings;
        }

        // Confidence rises when (1 - rRaw) is large (clear concavity), and when we have more rings.
        // Keeps it low (≈0.4) for ambiguous data, high (≈1) for obvious U/L/O shapes.
        private float ComputeFootprintConfidence(float rRaw, int ringCount)
        {
            float concavity = math.saturate(1f - rRaw);           // 0..1
            float ringFactor = math.saturate(ringCount / 3f);     // 0 rings -> 0, 3+ rings -> 1
            float baseConf = 0.4f + 0.6f * ringFactor;            // 0.4..1.0
            return math.saturate(baseConf * (0.5f + 0.5f * concavity));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float SumSubAreaPolygons(Entity prefab, bool includeSurface)
        {
            if (!_prefabSubAreas.HasBuffer(prefab) || !_prefabSubAreaNodes.HasBuffer(prefab))
                return 0f;

            var subAreas = _prefabSubAreas[prefab];
            var nodes = _prefabSubAreaNodes[prefab];

            if (subAreas.Length == 0 || nodes.Length == 0)
                return 0f;

            float areaSum = 0f;

            for (int i = 0; i < subAreas.Length; i++)
            {
                var sa = subAreas[i];

                // Filter by area type if requested (skip surfaces unless allowed)
                if (_prefabAreaGeometry.TryGetComponent(sa.m_Prefab, out var ag))
                {
                    if (!includeSurface && ag.m_Type == AreaType.Surface)
                        continue;
                }

                var range = sa.m_NodeRange; // inclusive indices into 'nodes' buffer
                if (range.y < range.x) continue;

                // Use the same entry ordering helper the game uses
                int first = ObjectToolBaseSystem.GetFirstNodeIndex(nodes, range);
                int count = range.y - range.x + 1;
                if (count < 3) continue;

                // Build ring in local XZ order
                var ring = new NativeList<float2>(count, Allocator.Temp);
                int idx = first;
                for (int k = 0; k < count; k++)
                {
                    var p = nodes[idx].m_Position;
                    ring.Add(new float2(p.x, p.z));

                    idx++;
                    if (idx > range.y) idx = range.x; // wrap
                }

                float a = ShoelaceAreaXZ(ring);
                ring.Dispose();

                if (a > 0.0001f)
                    areaSum += a;
            }

            return areaSum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ShoelaceAreaXZ(NativeList<float2> ring)
        {
            int n = ring.Length;
            if (n < 3) return 0f;

            double acc = 0d;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float2 a = ring[j];
                float2 b = ring[i];
                acc += (double)a.x * b.y - (double)b.x * a.y;
            }
            return math.abs((float)(acc * 0.5));
        }
    }
}
