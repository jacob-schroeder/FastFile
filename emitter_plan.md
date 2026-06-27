# FastFile Emitter Plan

## Purpose

Build a PS3-loader-accurate emitter/compiler pipeline for FastFile data.

The goal is not to reproduce the original serialized bytes byte-for-byte. The
goal is to emit a new source zone that the original PS3 loader consumes into an
equivalent runtime object graph, with correct pointer semantics, block stream
placement, alignment, destination-cell patching, and asset references.

The current loader work already proves a useful prefix of `patch_mp.ff`, and the
new `FastFile.Emitters` project is the landing place for the reverse operation.
The emitter must be driven by the same loader facts as the reader, not by a
second inferred schema.

## Current Project Layout

- `FastFile.Models`
  - Shared data models.
  - Shared pointer models such as `XPointer<T>`, `XPointerReference`, and
    `XPointerResolutionMode`.
  - Shared codec contracts in `FastFile.Models/Codecs`.
  - Asset family contract catalogs such as `MenuCodecContracts`.

- `FastFile.Loaders`
  - Current PS3-loader-shaped readers.
  - This remains the execution layer for loading official fastfiles.
  - Loaders may consult codec contracts for offsets, pointer modes, sizes, and
    evidence, but must still follow proven PS3 call order.

- `FastFile.Emitters`
  - New shell project.
  - Will become the execution layer for source-zone writing.
  - References `FastFile.Models`.
  - Must not reference `FastFile.Loaders` if avoidable. Shared facts belong in
    `FastFile.Models/Codecs`, not in either execution project.

- Future `FastFile.Compiler` or compiler namespace
  - Linker/orchestration layer.
  - Owns asset graph planning, symbol resolution, dependency order, and wrapper
    generation.
  - May reference `FastFile.Emitters`, `FastFile.Models`, and compression/wrapper
    utilities once those are split cleanly.

## Source Of Truth Files

- `emitter_plan.md`
  - This handoff and implementation plan.

- `emitter_structs.csv`
  - Progress ledger for top-level assets and active structs.
  - Every row has cumulative readiness flags:
    - `loader_proven`
    - `emitter_ready`
    - `compiler_ready`
  - Update this CSV in the same change that promotes a struct or asset.

## Readiness Definitions

### Open

The asset or struct is known to exist, but one or more of these is missing:

- PS3 loader proof.
- Official fastfile validation.
- Correct pointer mode.
- Correct block/alignment behavior.
- Active model/loader implementation.

`Open` rows must not be used by the compiler for authored output except as
external reference-only names when the target asset is already present and the
loader path for the referencing field is proven.

### LoaderProven

The current loader has enough PS3 evidence and official-fastfile validation to
read the struct/asset without drift.

Minimum bar:

- Root size or stride is proven.
- Field order required by the loader is proven.
- Pointer source semantics are proven for fields the loader resolves.
- Block push/pop/alignment behavior is proven for emitted children.
- Packed offset pointers are immediately validated against materialized runtime
  blocks where applicable.
- Scratch can load the relevant official file(s) without pointer validation
  failures.

`LoaderProven` does not imply all field names are semantically final. It means
the loader shape is trustworthy enough to use as an emitter contract candidate.

### EmitterReady

The emitter can write this asset/struct from semantic model data and the result
reloads through the PS3-shaped loader.

Minimum bar:

- An emitter exists for the struct/asset.
- The emitter follows the same contract as the loader.
- Source sentinels, packed offset pointers, inline payloads, nulls, and alias
  cells are written correctly.
- Projected runtime block positions match what the loader will materialize.
- The emitted output can be loaded by Scratch with strict pointer validation.
- Byte-for-byte equality with the official source zone is not required.

### CompilerReady

The compiler/linker can safely author, modify, and relink this asset/struct at a
semantic level.

Minimum bar:

- It is `EmitterReady`.
- Semantic field names are strong enough for user-facing modification.
- References can be expressed as symbols where appropriate.
- Anonymous nested payloads can be emitted or preserved through semantic model
  data.
- Modifying at least one meaningful field has been tested by reload validation.
- Recursive children are either compiler-ready themselves or explicitly
  reference-only.

## Non-Negotiable Rules

1. PS3 loader behavior is the source of truth.
2. Official fastfiles are read-only evidence.
3. Do not trust the old weapon reader for emitter logic.
4. Do not use opaque source-byte copying for pointer-bearing structs.
5. Do not collapse direct pointers, alias-cell pointers, inline sentinels,
   insert sentinels, nulls, and packed offsets into one generic path.
6. Do not emit `-2` insert behavior unless that exact loader path is proven.
7. Do not promote Xbox names to compiler-facing semantics unless PS3 loader or
   consumer behavior supports the same field.
8. Do not call a struct `CompilerReady` while it still contains unknown semantic
   fields that users can accidentally edit incorrectly.

## Loader Versus Emitter

A loader consumes source bytes and materializes runtime block streams:

```text
source zone bytes -> runtime blockstreams -> semantic objects + pointer metadata
```

An emitter writes source bytes while projecting what the loader will materialize:

```text
semantic objects + link plan -> source zone bytes
source zone bytes -> PS3 loader -> equivalent runtime blockstreams
```

The emitter is not a literal backwards loader. It is a forward writer that
follows the same contract:

- Same root sizes.
- Same field order.
- Same child order.
- Same block pushes/pops.
- Same alignment.
- Same pointer semantics.
- Same destination-cell model.

## Why Runtime Blocks Still Matter During Emit

The serialized source zone contains source pointer values, but packed offset
references target runtime block addresses. Therefore the compiler must maintain
a projected runtime block state while writing source bytes.

For example:

```text
Direct pointer field:
  source field contains encoded runtime target address

Alias-cell pointer field:
  source field contains encoded runtime address of a pointer cell

Inline pointer field:
  source field contains -1
  source payload follows in loader-proven order
  projected runtime destination cell is patched to the projected payload address
```

The projected block state is the bridge between the source writer and the loader
contract.

## Core Emitter Abstractions

### XSourceWriter

Responsible for endian-correct source-zone output.

Responsibilities:

- Big-endian integer writes where PS3 source requires them.
- Floating-point writes.
- CString writes.
- Alignment padding in source where the loader proves source alignment.
- Reservation/patching of source dwords if a two-pass source write is needed.

This writer is not the runtime block stream. It writes serialized source bytes.

### XProjectedBlockState

Compiler-side mirror of loader block materialization.

Responsibilities:

- Track active block.
- Track live positions.
- Track high-water/materialized lengths.
- Support push/pop behavior matching `DB_PushStreamPos` and `DB_PopStreamPos`.
- Apply alignment to the projected runtime destination block.
- Allocate projected destination addresses for roots, arrays, strings, and
  payloads.
- Patch projected destination pointer cells for inline payloads.
- Validate projected block sizes before finalizing XFile block sizes.

This should be modeled after `BlockStreamState`, but it must be write/planning
oriented instead of read/materialization oriented.

### XEmitContext

Shared context passed through emitters.

Expected contents:

- `XSourceWriter Source`.
- `XProjectedBlockState Blocks`.
- `XLinker Linker`.
- `XEmitDiagnostics Diagnostics`.
- Current asset key and current contract stack.

### AssetKey

Symbol key for named top-level assets.

Recommended shape:

```csharp
public readonly record struct AssetKey(XAssetType Type, string Name);
```

Names should not be treated as globally unique across asset types. Use
`(XAssetType, Name)`.

### SymbolRef

Reference to a named asset.

Recommended shape:

```csharp
public readonly record struct SymbolRef(AssetKey Key);
```

Use this for user-authored references such as a menu window background material.

### ObjectNode

Internal linker node for anonymous nested objects and named roots.

Responsibilities:

- Stable object identity during one compile.
- Optional original source/runtime addresses from import.
- Optional symbol key.
- Emitter contract.
- Projected runtime address after layout.
- Canonical pointer cell address for alias-cell references.

### LinkedPointer<T>

Future pointer model for authored/compiler references.

It should preserve the loader field contract separately from the selected
reference target:

```csharp
public sealed record LinkedPointer<T>(
    XPointerResolutionMode Mode,
    XPointerSourceSemantics SourceSemantics,
    SymbolRef? Symbol,
    ObjectNode? InlineObject,
    XBlockAddress? OriginalCell,
    XBlockAddress? OriginalTarget);
```

Do not replace `XPointer<T>` immediately. `XPointer<T>` remains valuable for raw
load diagnostics. `LinkedPointer<T>` is for compiler-side authoring/linking.

## Pointer Emission Rules

### Null

Write `0`.

No payload follows. No runtime block allocation occurs for the target.

### Direct Packed Reference

Write:

```text
raw = (block << 28) | (offset + 1)
```

The encoded address is the projected runtime address of the target object.

### Alias-Cell Packed Reference

Write an encoded pointer to the target object's canonical pointer cell, not to
the target object itself.

The canonical cell is chosen by the linker. For named top-level assets, this can
often be the XAsset table pointer cell. For anonymous nested objects, the
compiler may need to allocate or reuse a stable cell created by the loader path.

### Inline

Write `-1` in source.

Then emit the payload in the child order proven by the loader. The projected
destination pointer cell must be patched to the projected payload address before
payload bytes are projected, matching the loader's destination-cell rule.

### Insert

Write `-2` only for loader paths with proven `DB_InsertPointer` behavior.

Current `patch_mp.ff` work should not generalize insert behavior.

### XString

Treat as a pointer field with direct pointer semantics unless the exact loader
path proves otherwise.

For inline strings, write `-1` then the null-terminated string payload in the
loader-proven block and order.

### ScriptString

ScriptString values are `uint16` indices into the loaded script string table,
not inline C strings inside the owning struct.

Do not emit arbitrary string bytes for ScriptString fields. The compiler must
maintain and emit the XAssetList script string table, then write the corresponding
16-bit indices into ScriptString fields.

## Codec Contracts

Contracts live in `FastFile.Models/Codecs`.

They describe facts shared by loaders and emitters:

- Struct name.
- Serialized size or stride.
- Field offsets.
- Field sizes.
- Pointer target type.
- Pointer resolution mode.
- Pointer source semantics.
- Inline alignment.
- Inline block hints.
- Evidence text.
- Readiness.
- Optional operation recipe.

Contracts should not replace loader call-order work. Complex assets need
operation recipes with named steps, not only flat field tables.

Recommended contract layers:

1. Primitive operations.
2. Struct contracts.
3. Asset contracts.
4. Asset-family catalogs.

## Operation Recipe Model

The current `XCodecRecipe` supports these operation kinds:

- `StreamStruct`
- `Field`
- `Align`
- `PushBlock`
- `PopBlock`
- `Custom`

This is intentionally small. Add operation types only when repeated emitter
logic needs them.

Expected future operations:

- `Pointer`
- `XString`
- `FixedArray`
- `CountedArray`
- `PointerArray`
- `ScriptStringArray`
- `InlineStruct`
- `Union`
- `RawProvenBytes`

Do not add a generic reflection serializer. Loader order matters too much.

## Contract Catalogs

Each asset family should eventually expose a catalog:

```csharp
public static class MenuCodecContracts
{
    public static readonly IReadOnlyList<IXCodecContract> All = [...];
}
```

Catalogs make it possible for diagnostics and compiler setup to register known
contracts without scanning arbitrary static classes.

Existing catalog:

- `FastFile.Models.Assets.Menu.MenuCodecContracts`

Needed catalogs:

- `MaterialCodecContracts`
- `TechniqueSetCodecContracts`
- `StringTableCodecContracts`
- `StructuredDataCodecContracts`
- `RawFileCodecContracts`
- `LocalizeCodecContracts`
- `WeaponCodecContracts`
- `XAssetListCodecContracts`

## Asset Emitter Interfaces

Add these in `FastFile.Emitters` once the first real emitter is implemented:

```csharp
public interface IXAssetEmitter<in TAsset>
    where TAsset : BaseAsset
{
    IXAssetCodecContract Contract { get; }
    void EmitAsset(XEmitContext context, TAsset asset);
}
```

For nested structs:

```csharp
public interface IXStructEmitter<in T>
{
    IXCodecContract Contract { get; }
    void EmitStruct(XEmitContext context, T value);
}
```

Avoid making emitters generic over field reflection. Most structs need explicit
loader-shaped code.

## Compiler Passes

### Pass 1: Import Graph

Input:

- Loaded `XFile`.
- Loaded `XAssetListSnapshot`.
- Runtime block/pointer metadata preserved by loaders.

Output:

- Asset nodes.
- Object nodes.
- Symbol table.
- Original pointer/cell information for diagnostics.

Rules:

- Register top-level assets by `(XAssetType, Name)`.
- Keep anonymous nested objects as object nodes.
- Preserve original raw pointer values for diagnostics.
- Preserve direct versus alias-cell mode from loader contracts.

### Pass 2: Edit Graph

Input:

- Imported object graph.
- User modifications.

Output:

- Modified semantic graph.
- Dirty asset/object set.

Rules:

- A field can be editable only when its owning struct is `CompilerReady`.
- Reference-only children can be relinked by symbol but not rewritten.
- Open structs must block semantic editing.

### Pass 3: Link Plan

Input:

- Modified graph.
- Symbol table.
- Contract registry.

Output:

- Emit order.
- Canonical pointer cell plan.
- Inline versus reference plan.
- Projected runtime block plan.

Rules:

- Named top-level assets are emitted once.
- Alias-cell references need canonical cells.
- Direct references need target runtime addresses.
- Inline objects consume source bytes in loader-proven child order.
- Cycles must be represented through references/cells, not recursive inline
  expansion.

### Pass 4: Source Zone Emit

Input:

- Link plan.
- Semantic object graph.

Output:

- XFile source zone bytes.
- XFile block sizes.
- Diagnostics.

Rules:

- Write roots and children in loader-proven order.
- Maintain projected runtime block positions while writing source.
- Emit packed pointers using projected runtime addresses.
- Emit sentinels only where the loader path supports them.
- Validate projected block bounds.

### Pass 5: Fastfile Wrap

Input:

- Source zone bytes.
- XFile header.
- DB header fields.

Output:

- `.ff` bytes.

This should be easier than the zone/linker work. Keep it separate from asset
emission so the emitter can be tested against raw zone bytes first.

### Pass 6: Reload Validation

Input:

- Emitted `.ff` or emitted source zone harness.

Output:

- Strict loader validation result.
- Pointer validation counts.
- Block position report.
- Asset graph comparison report.

Minimum success:

- Scratch loads through the intended asset range.
- No packed pointer validation failures.
- No block-size overruns.
- All assets resolve by expected `(type, name)`.

## Initial Milestones

### M0: Shell Project

Status: complete.

Done:

- Created `FastFile.Emitters`.
- Added it to `FastFile.sln`.
- Referenced `FastFile.Models`.
- Build passes after restore.

### M1: Planning And Ledger

Status: this document plus `emitter_structs.csv`.

Done when:

- The plan exists.
- The CSV exists.
- Current statuses are conservative and up to date.

### M2: Emitter Core Primitives

Implement in `FastFile.Emitters`:

- `XSourceWriter`.
- `XProjectedBlockState`.
- `XEmitContext`.
- `XEmitDiagnostics`.
- Pointer encoding helpers that consume `XPointerFieldContract`.

Validation:

- Unit-level tests for pointer encoding and block projection.
- No asset emission yet.

### M3: First Real Asset Round Trip

Recommended first asset: `Localize`.

Reason:

- Root is only `0x08`.
- Contains two direct XStrings.
- No recursive asset graph.

Done when:

- `LocalizeAssetEmitter` exists.
- A loaded Localize asset can be emitted to source bytes.
- Re-loading emitted bytes produces equivalent Localize data.
- `Localize` row in CSV becomes `EmitterReady`.

### M4: Simple Data Assets

Targets:

- `RawFile`
- `StringTable`
- `StructuredDataDefSet`

Done when:

- Each asset has an emitter.
- Each emitted asset reloads under strict pointer validation.
- CSV rows become `EmitterReady`.

### M5: Menu Family Emitter

Targets:

- `MenuFile`
- `MenuDef`
- `WindowDef`
- `ItemDef`
- expression/event/type-data nested structs.

Done when:

- Menu family emits from semantic model.
- Menu background material references use alias-cell semantics from contracts.
- Direct pointer arrays stay direct where PS3 loader proves direct.
- `patch_mp.ff` menu assets can be re-emitted and reloaded.

### M6: Material And TechniqueSet

Targets:

- `MaterialTechniqueSet`
- `MaterialTechnique`
- `MaterialPass`
- shaders and shader args.
- `Material`
- material texture/constant/statebit children.

Done when:

- Techset emits and reloads.
- Material emits and reloads for known `patch_mp.ff` cases.
- GfxImage references remain reference-only until image payload semantics are
  compiler-ready.

### M7: Weapon Reference-Only Compiler Path

Target:

- Weapon root and `WeaponDef` re-emission with recursive child assets treated as
  reference-only unless already compiler-ready.

Done when:

- Weapon assets can be emitted without expanding unknown recursive child graphs.
- XModel, Material, Fx, GfxImage, Tracer, PhysPreset, and PhysCollmap references
  preserve field-specific pointer semantics.
- Scratch reloads emitted weapon assets with strict pointer validation.

### M8: Full Recursive Compiler

Target:

- Recursive children become compiler-ready one family at a time.

Done when:

- The compiler can modify a weapon field and repack a zone.
- The compiler can modify a menu material reference by name.
- The compiler can rebuild script string tables.
- The emitted fastfile loads and behaves under the real PS3 loader expectations.

## Promotion Workflow

When promoting a row in `emitter_structs.csv`:

1. Add or update the relevant codec contract.
2. Add or update the loader to consume shared contract facts where appropriate.
3. Add emitter code if promoting to `EmitterReady`.
4. Add validation output or tests.
5. Update the CSV row.
6. Mention exact evidence in the PR/commit notes.

Do not update the CSV first. The CSV records proven status, not intention.

## Immediate Next Tasks

1. Add `LocalizeCodecContracts`.
2. Add `XSourceWriter`.
3. Add `XProjectedBlockState`.
4. Add `XEmitContext`.
5. Implement `LocalizeAssetEmitter`.
6. Build a tiny source-zone harness for one Localize root.
7. Reload emitted Localize bytes through the existing loader path.
8. Promote `Localize` to `EmitterReady` only after reload validation passes.

## Handoff Notes

- Keep `FastFile.Emitters` small until the first asset round trip works.
- Keep contracts in `FastFile.Models`.
- Do not make `FastFile.Emitters` depend on `FastFile.Loaders` unless there is a
  very specific reason.
- Prefer explicit emitter code over generic reflection.
- Use `emitter_structs.csv` to choose work and report progress.
- Re-run `dotnet build FastFile/FastFile.sln --no-restore` after code changes.
- Re-run Scratch against `patch_mp.ff` for loader-impacting changes.
