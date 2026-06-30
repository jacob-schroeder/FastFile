using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FastFile.Render.Export;

namespace FastFile.Render.Glb;

internal enum GlbPrimitiveMode
{
    Lines = 1,
    Triangles = 4
}

internal sealed class GlbSceneBuilder
{
    private readonly string _generator;
    private readonly List<byte> _bin = [];
    private readonly List<Dictionary<string, object>> _bufferViews = [];
    private readonly List<Dictionary<string, object>> _accessors = [];
    private readonly List<Dictionary<string, object>> _materials = [];
    private readonly List<Dictionary<string, object>> _images = [];
    private readonly List<Dictionary<string, object>> _textures = [];
    private readonly List<GltfMesh> _meshes = [];
    private readonly List<Dictionary<string, object>> _nodes = [];

    public GlbSceneBuilder(string generator)
    {
        _generator = generator;
    }

    public int AddMaterial(string name, Rgba color, int? baseColorTexture = null)
    {
        int index = _materials.Count;
        var pbr = new Dictionary<string, object>
        {
            ["baseColorFactor"] = new[] { color.R, color.G, color.B, color.A },
            ["metallicFactor"] = 0.0f,
            ["roughnessFactor"] = 0.85f
        };
        if (baseColorTexture.HasValue)
            pbr["baseColorTexture"] = new Dictionary<string, object> { ["index"] = baseColorTexture.Value };

        _materials.Add(new Dictionary<string, object>
        {
            ["name"] = name,
            ["pbrMetallicRoughness"] = pbr,
            ["alphaMode"] = color.A < 1.0f ? "BLEND" : "OPAQUE",
            ["doubleSided"] = true
        });
        return index;
    }

    public int AddPngTexture(string name, byte[] pngBytes)
    {
        AlignBin(4);
        int byteOffset = _bin.Count;
        _bin.AddRange(pngBytes);
        int bufferView = AddBufferView(byteOffset, pngBytes.Length, target: null);

        int image = _images.Count;
        _images.Add(new Dictionary<string, object>
        {
            ["name"] = name,
            ["mimeType"] = "image/png",
            ["bufferView"] = bufferView
        });

        int texture = _textures.Count;
        _textures.Add(new Dictionary<string, object> { ["source"] = image });
        return texture;
    }

    public int AddMesh(string name)
    {
        int index = _meshes.Count;
        _meshes.Add(new GltfMesh(name));
        return index;
    }

    public void AddPrimitive(
        int meshIndex,
        int positionAccessor,
        IReadOnlyList<uint> indices,
        GlbPrimitiveMode mode,
        int materialIndex,
        int? texCoordAccessor = null)
    {
        int indexAccessor = AddIndices(indices);
        var attributes = new Dictionary<string, object> { ["POSITION"] = positionAccessor };
        if (texCoordAccessor.HasValue)
            attributes["TEXCOORD_0"] = texCoordAccessor.Value;

        _meshes[meshIndex].Primitives.Add(new Dictionary<string, object>
        {
            ["attributes"] = attributes,
            ["indices"] = indexAccessor,
            ["mode"] = (int)mode,
            ["material"] = materialIndex
        });
    }

    public void AddNode(string name, int meshIndex)
    {
        _nodes.Add(new Dictionary<string, object>
        {
            ["name"] = name,
            ["mesh"] = meshIndex
        });
    }

    public int AddPositions(IReadOnlyList<Vec3f> positions)
    {
        if (positions.Count == 0)
            throw new ArgumentException("Position accessor cannot be empty.", nameof(positions));

        AlignBin(4);
        int byteOffset = _bin.Count;
        Vec3f min = positions[0];
        Vec3f max = positions[0];
        foreach (Vec3f position in positions)
        {
            min = Vec3f.Min(min, position);
            max = Vec3f.Max(max, position);
            WriteSingle(position.X);
            WriteSingle(position.Y);
            WriteSingle(position.Z);
        }

        int byteLength = _bin.Count - byteOffset;
        int bufferView = AddBufferView(byteOffset, byteLength, target: 34962);
        int accessor = _accessors.Count;
        _accessors.Add(new Dictionary<string, object>
        {
            ["bufferView"] = bufferView,
            ["byteOffset"] = 0,
            ["componentType"] = 5126,
            ["count"] = positions.Count,
            ["type"] = "VEC3",
            ["min"] = new[] { min.X, min.Y, min.Z },
            ["max"] = new[] { max.X, max.Y, max.Z }
        });
        return accessor;
    }

    public int AddTexCoords(IReadOnlyList<Vec2f> texCoords)
    {
        if (texCoords.Count == 0)
            throw new ArgumentException("Texcoord accessor cannot be empty.", nameof(texCoords));

        AlignBin(4);
        int byteOffset = _bin.Count;
        foreach (Vec2f texCoord in texCoords)
        {
            WriteSingle(texCoord.U);
            WriteSingle(texCoord.V);
        }

        int byteLength = _bin.Count - byteOffset;
        int bufferView = AddBufferView(byteOffset, byteLength, target: 34962);
        int accessor = _accessors.Count;
        _accessors.Add(new Dictionary<string, object>
        {
            ["bufferView"] = bufferView,
            ["byteOffset"] = 0,
            ["componentType"] = 5126,
            ["count"] = texCoords.Count,
            ["type"] = "VEC2"
        });
        return accessor;
    }

    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        byte[] jsonChunk = BuildJsonChunk();
        byte[] binChunk = PadChunk(_bin.ToArray(), 0);
        uint length = checked((uint)(12 + 8 + jsonChunk.Length + 8 + binChunk.Length));

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        writer.Write(0x46546C67u);
        writer.Write(2u);
        writer.Write(length);
        writer.Write((uint)jsonChunk.Length);
        writer.Write(0x4E4F534Au);
        writer.Write(jsonChunk);
        writer.Write((uint)binChunk.Length);
        writer.Write(0x004E4942u);
        writer.Write(binChunk);
    }

    private int AddIndices(IReadOnlyList<uint> indices)
    {
        if (indices.Count == 0)
            throw new ArgumentException("Index accessor cannot be empty.", nameof(indices));

        AlignBin(4);
        int byteOffset = _bin.Count;
        uint min = indices[0];
        uint max = indices[0];
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        foreach (uint index in indices)
        {
            min = Math.Min(min, index);
            max = Math.Max(max, index);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, index);
            _bin.AddRange(bytes);
        }

        int byteLength = _bin.Count - byteOffset;
        int bufferView = AddBufferView(byteOffset, byteLength, target: 34963);
        int accessor = _accessors.Count;
        _accessors.Add(new Dictionary<string, object>
        {
            ["bufferView"] = bufferView,
            ["byteOffset"] = 0,
            ["componentType"] = 5125,
            ["count"] = indices.Count,
            ["type"] = "SCALAR",
            ["min"] = new[] { min },
            ["max"] = new[] { max }
        });
        return accessor;
    }

    private int AddBufferView(int byteOffset, int byteLength, int? target)
    {
        int index = _bufferViews.Count;
        var bufferView = new Dictionary<string, object>
        {
            ["buffer"] = 0,
            ["byteOffset"] = byteOffset,
            ["byteLength"] = byteLength
        };
        if (target.HasValue)
            bufferView["target"] = target.Value;

        _bufferViews.Add(bufferView);
        return index;
    }

    private byte[] BuildJsonChunk()
    {
        var root = new Dictionary<string, object>
        {
            ["asset"] = new Dictionary<string, object>
            {
                ["version"] = "2.0",
                ["generator"] = _generator
            },
            ["scene"] = 0,
            ["scenes"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["nodes"] = Enumerable.Range(0, _nodes.Count).ToArray()
                }
            },
            ["nodes"] = _nodes,
            ["meshes"] = _meshes.Select(x => new Dictionary<string, object>
            {
                ["name"] = x.Name,
                ["primitives"] = x.Primitives
            }).ToList(),
            ["materials"] = _materials,
            ["buffers"] = new[]
            {
                new Dictionary<string, object> { ["byteLength"] = AlignTo4(_bin.Count) }
            },
            ["bufferViews"] = _bufferViews,
            ["accessors"] = _accessors
        };
        if (_images.Count > 0)
        {
            root["images"] = _images;
            root["textures"] = _textures;
        }

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(root, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        return PadChunk(json, 0x20);
    }

    private void WriteSingle(float value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
        _bin.AddRange(bytes);
    }

    private void AlignBin(int alignment)
    {
        int aligned = (_bin.Count + alignment - 1) / alignment * alignment;
        while (_bin.Count < aligned)
            _bin.Add(0);
    }

    private static int AlignTo4(int value)
    {
        return (value + 3) / 4 * 4;
    }

    private static byte[] PadChunk(byte[] bytes, byte padding)
    {
        int length = AlignTo4(bytes.Length);
        if (length == bytes.Length)
            return bytes;

        byte[] padded = new byte[length];
        bytes.CopyTo(padded, 0);
        padded.AsSpan(bytes.Length).Fill(padding);
        return padded;
    }

    private sealed class GltfMesh
    {
        public GltfMesh(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public List<Dictionary<string, object>> Primitives { get; } = [];
    }
}
