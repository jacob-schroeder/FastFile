# EBOOT Zone Load Model

This note summarizes how the IW4 PS3 `EBOOT.ELF` loads a fastfile zone at the level needed to safely read, modify, and rewrite assets without breaking pointers.

The important idea is that the zone is not loaded like a normal object graph by seeking around the file. The EBOOT reads one serialized byte stream in order, while separately advancing logical `g_streamBlocks` cursors that represent where each loaded object will live in memory.

Serialized read order and stream-block allocation order are related, but they are not the same thing. Treating them as the same thing is what causes edited files to blackscreen when a resized asset shifts later data.

## High-Level Flow

The zone starts with an `XFile` header:

```text
int Size
int ExternalSize
int BlockSize[MAX_XFILE_COUNT]
```

After the header, the serialized `XAssetList` data follows.

The reader flow is:

```text
1. Parse the XFile header.
2. Initialize stream-block cursors from the header block sizes.
3. Read the XAssetList fields from the serialized stream.
4. Resolve inline/insert pointers in the exact order the EBOOT queues them.
5. Track each materialized pointer span in stream-block coordinates.
6. Resolve offset pointers by matching their target stream address to the materialized spans.
```

Relevant implementation files:

```text
FastFile/FastFile.Logic/Zone/XFileReader.cs
FastFile/FastFile.Logic/Zone/XFileReadContext.cs
FastFile/FastFile.Logic/Zone/XFileReadStreamBlocks.cs
FastFile/FastFile.Logic/Zone/XFileWriterContext.Pointers.cs
FastFile/FastFile.Models/Data/Pointer.cs
```

## Stream Blocks

The EBOOT loads data into logical stream blocks. On PS3 the relevant enum values are:

```text
TEMP    = 0
LARGE   = 4
VERTEX  = 6
```

The simulator tracks an active stream block and an offset inside that block. Every primitive read advances both:

```text
serialized file position += bytes read
active stream block offset += bytes read
```

When the EBOOT pushes a different stream block, serialized reads still continue forward, but allocation accounting moves to the pushed block.

Example:

```text
Push LARGE
  Push TEMP
    read script strings
  Pop TEMP

  read asset array
  resolve queued asset bodies
Pop LARGE
```

The bytes are still consumed in serialized order. The active `g_streamBlocks` destination changes as the EBOOT pushes and pops blocks.

## Pointer Encoding

A pointer field is a 32-bit value, but it is not always an address.

```text
0     => null
-1    => inline data follows in the serialized stream
-2    => insert pointer: inline data plus a reserved alias cell
> 0   => encoded stream-block address
```

Positive pointer decode:

```csharp
encoded = raw - 1;
block   = (encoded >> 28) & 0xF;
offset  = encoded & 0x0FFFFFFF;
```

Example:

```text
raw     = 0x40000101
encoded = 0x40000100
block   = 4 = LARGE
offset  = 0x100
```

That means:

```text
g_streamBlocks[LARGE] + 0x100
```

It does not mean:

```text
seek to zone file offset 0x100
```

## Pointer Kinds

### Null Pointer

```text
raw = 0
```

No data is read and no target is resolved.

### Inline Pointer

```text
raw = -1
```

The pointee data follows in the serialized stream. The loader does not seek. It reads the pointee at the current serialized position and records the active stream-block address as the pointee's memory address.

Example:

```text
field: RawFile.Buffer
raw:   -1

current serialized position: 0x123456
active stream block: LARGE
active stream offset: 0x3000

read compressed/raw bytes here
materialized span:
  source zone offset: 0x123456
  source length:      CompressedLen or Len + 1
  stream block:       LARGE
  stream offset:      0x3000
```

### Insert Pointer

```text
raw = -2
```

An insert pointer has inline data, but the loader also reserves a 4-byte alias cell. Other pointers can point to that reserved cell rather than to the inline data body.

When rewriting, insert pointers must preserve this alias-cell behavior. Treating an insert pointer like a plain direct pointer can break shared references.

### Offset Pointer

```text
raw > 0
```

The pointer encodes a stream-block address. The EBOOT usually does not read data at the pointer field itself. Instead, it expects the target data or alias cell to already be represented in `g_streamBlocks`.

Whether the pointer is a direct data pointer or an alias pointer depends on the EBOOT load function for that field.

## Direct Pointers Vs Alias Pointers

The same raw pointer format can mean two different things depending on the field.

### Direct Pointer

A direct pointer owns nested serialized data.

Examples:

```text
XAssetList.ScriptStrings
XAssetList.Assets
RawFile.Buffer
MenuList.Name
MenuList.Menus
MenuDef.Items
Material.TextureTable
WeaponDef.GunXModel
WeaponDef.BounceSound
```

For a direct pointer, the loader reads the nested data in the current load order and records the materialized span.

### Alias Pointer

An alias pointer refers to a shared asset/reference cell. It is used so assets can be reused instead of serialized repeatedly.

Examples:

```text
XAsset.Header
MenuList.Menus[i]
Material.TechniqueSet
Window.Background
MaterialTextureDef.Image
Weapon.Material
Weapon.XModel
Weapon.Fx
Weapon.Tracer
```

Alias pointers must not be rewritten as owned nested data pointers. They point to a shared target or alias cell.

The field-level rules are recorded in:

```text
FastFile/FastFile.Logic/Zone/EbootPointerRules.cs
```

## Concrete Example: XAssetList

The EBOOT reads the asset list header:

```text
scriptStringCount
scriptStringsPtr   direct
assetCount
assetsPtr          direct
```

Then it follows a specific stream-block order:

```text
Push LARGE
  Push TEMP
    resolve scriptStringsPtr
  Pop TEMP

  resolve assetsPtr
  resolve queued asset bodies
Pop LARGE
```

The key detail is that `scriptStringsPtr` and `assetsPtr` are read from the serialized stream near the start of the zone, but their pointee data is materialized according to the active stream block when the EBOOT resolves them.

## Concrete Example: RawFile

A rawfile is read like:

```text
NamePtr        direct XString
CompressedLen
Len
BufferPtr      direct byte[]
```

For `RawFile.Buffer`, the EBOOT reads:

```text
length = CompressedLen > 0 ? CompressedLen : Len + 1
Push LARGE
  read length bytes
Pop LARGE
```

This means rawfile compression and length changes are valid. The writer must update stream-block addresses for later pointers that move because the rawfile buffer grew or shrank.

Do not assume a changed rawfile length is invalid. The issue is not the length change; the issue is failing to rebase pointers that reference later stream-block data.

## Concrete Example: MenuFiles

A `MenuList` is read like:

```text
NamePtr       direct XString
MenuCount
MenusPtr      direct array
Menus[i]      alias pointer to MenuDef
```

`MenusPtr` owns the array of menu pointers, so it is direct.

Each `Menus[i]` points to a menu asset/reference, so it is alias.

Nested `MenuDef` data has many additional direct and alias pointers:

```text
MenuDef.Items                  direct
MenuDef.ExpressionData         direct
Window.Name                    direct
Window.Group                   direct
Window.Background              alias material reference
MenuEventHandlerSet            direct
MenuEventHandler scripts       direct
Statement entries              direct
```

The correct way to model a menu is not to look for byte patterns. Follow the EBOOT field order and classify each pointer according to how the load routine resolves it.

## Concrete Example: Material

A material includes both owned nested tables and shared asset references.

Examples of direct data:

```text
Material.TextureTable
Material.ConstantTable
Material.StateBitTable
Material.Info.Name
```

Examples of alias references:

```text
Material.TechniqueSet
MaterialTextureDef.Image
```

The difference matters. Texture/constant/state tables are material-owned serialized arrays. Technique sets and images are shared assets.

## How To Determine Pointer Locations Correctly

Do not scan bytes. A field is a pointer only when the EBOOT load function reads it as a pointer.

For every asset reader, track:

```text
1. Current serialized zone offset
2. Current active stream block
3. Field being read
4. Field type
5. Pointer kind from raw value
6. Pointer resolution kind from EBOOT logic: direct or alias
```

When reading a pointer field, record:

```text
pointer field serialized offset
pointer field stream block
pointer field stream offset
raw pointer value
decoded target block/offset, if raw > 0
field path, for example RawFile.Buffer or MenuList.Menus[i]
resolution kind, direct or alias
```

When resolving inline data, record:

```text
source serialized start
source serialized length
source stream block
source stream offset
value type
owning pointer
```

Those materialized spans are the only reliable way to map old pointer targets to new written targets after resizing data.

## How Offset Pointer Targets Are Matched

After the asset graph is read, offset pointers are matched against materialized spans.

For each offset pointer:

```text
target stream block = decoded block
target stream offset = decoded offset
```

Find the materialized span that contains the target:

```text
span stream block == target stream block
span stream offset <= target stream offset < span stream offset + span length
```

Then the pointer target is:

```text
delta = target stream offset - span stream offset
target = span written stream offset + delta
```

When writing, this becomes:

```csharp
newRaw = Pointer.EncodeOffset(writtenBlock, writtenOffset);
```

If the target is an alias pointer, match the alias cell instead of the nested data body.

## Writer Rules

The writer must preserve the EBOOT's load model:

```text
1. Write fields in the same serialized order the EBOOT reads them.
2. Allocate nested data in the same stream-block order the EBOOT uses.
3. Write pointer fields first, usually with the original raw value.
4. Record every materialized source span and its written span.
5. After all data is written, patch offset pointers to the new written stream-block addresses.
6. Keep alias pointers pointing at alias cells/shared assets.
7. Track source length and written length separately.
```

The last point is critical. If a rawfile grows from 0x100 bytes to 0x180 bytes:

```text
source length  = 0x100
written length = 0x180
```

Pointers inside the old span can be mapped by delta. Pointers after the span may need to shift based on the written end of the previous materialized span.

This is why padding-only edits used to work: they avoided changing later stream-block offsets. A correct writer does not need that limitation; it rebases pointers according to the EBOOT load model.

## Practical Checklist For Adding A New Asset Type

When implementing or validating an asset reader/writer:

```text
1. Find the EBOOT load function for the asset.
2. List fields in exact read order.
3. Mark primitive sizes and alignments.
4. For each pointer field, identify:
   - raw field offset
   - target type
   - direct vs alias
   - inline/insert/offset/null behavior
   - stream block used when resolving pointee data
5. Add a proofed pointer rule for the field path.
6. Ensure the reader records materialized spans for owned data.
7. Ensure the writer emits the same field order.
8. Ensure the writer allocates nested data in the same stream block.
9. Compare rewritten block-stream addresses against a known loading file.
10. Only trust differences that are explained by intentional asset changes.
```

## Red Flags

These usually mean the model is wrong:

```text
An alias pointer is treated as direct owned data.
A direct pointer is left pointing at its old stream offset after resized data.
Pointer targets are computed from zone file offsets instead of stream-block offsets.
The writer preserves serialized byte order but not stream-block allocation order.
The writer assumes rawfile length changes are invalid.
The writer scans for pointer-looking values instead of following EBOOT fields.
Nested menu/weapon assets are skipped or treated as opaque blobs.
```

## Short Version

The EBOOT loads the zone by consuming serialized bytes in order while separately maintaining `g_streamBlocks` allocation cursors. Pointer fields must be discovered by following the EBOOT's field reads. A pointer's raw value tells you its kind and possibly a stream-block target, but only the field's load logic tells you whether it is direct owned data or an alias/shared reference.

To safely modify assets, the writer must reproduce the EBOOT load order, record old materialized spans and new written spans, and patch offset pointers to the new stream-block addresses.
