# FastFile.Render

Exports loaded map fastfiles into inspection-friendly artifacts.

```bash
dotnet run --project FastFile/FastFile.Render/FastFile.Render.csproj -- \
  /Users/jacob/Repositories/FastFile/Data/official_ff/mp_boneyard.ff \
  --out /Users/jacob/Repositories/FastFile/FastFile/render-output/mp_boneyard
```

Outputs:

- `<map>.gfx.glb`: `GfxMap` render-world mesh, grouped by material pointer/name.
- `<map>.collision-debug.glb`: `ColMapMp` collision triangles plus static-model
  placement markers and trigger debug boxes.
- `<map>.static-xmodels.csv`: `ColMapMp` static model placement rows.
- `<map>.mapents.txt`: raw `MapEnts` entity string.
- `<map>.stages.csv`: `MapEnts` stage rows.

Coordinates are converted from IW-style Z-up to glTF Y-up by default. Pass
`--raw-coordinates` to preserve source coordinates.

Current static xmodel geometry is represented as placement markers. The
`ClipStaticModel` row exposes origin, inverse scaled axes, and raw bounds-like
fields; full instanced XModel mesh emission should wait until the XSurface
vertex stream and static model transform semantics are proven. CModel boxes are
not emitted yet because the currently named `CModel.Mins/Maxs` rows include
outlier values that need a loader/consumer semantics pass before they are safe
for visualization.
