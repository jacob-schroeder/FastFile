using System.Text;

namespace FastFile.Render.Export;

internal static class ViewerHtmlWriter
{
    public static string Build(string stem, bool hasCollision)
    {
        string collisionFlag = hasCollision ? "true" : "false";
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{Html(stem)}} GLB Viewer</title>
  <style>
    html, body { margin: 0; width: 100%; height: 100%; overflow: hidden; background: #111; color: #eee; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    #viewport { width: 100vw; height: 100vh; display: block; }
    .toolbar { position: fixed; left: 16px; top: 16px; display: flex; flex-wrap: wrap; gap: 8px; align-items: center; padding: 10px; background: rgba(20, 20, 20, 0.84); border: 1px solid rgba(255,255,255,.14); border-radius: 8px; backdrop-filter: blur(8px); }
    .status { position: fixed; left: 16px; bottom: 16px; max-width: min(980px, calc(100vw - 32px)); padding: 8px 10px; background: rgba(20, 20, 20, 0.84); border: 1px solid rgba(255,255,255,.14); border-radius: 8px; font-size: 13px; line-height: 1.35; }
    .divider { width: 1px; height: 28px; background: rgba(255,255,255,.16); }
    .control-group { display: grid; grid-template-columns: repeat(3, 34px); gap: 4px; align-items: center; justify-items: center; }
    .control-group.zoom { grid-template-columns: repeat(2, 34px); }
    button { border: 1px solid rgba(255,255,255,.22); background: #262626; color: #f3f3f3; border-radius: 6px; padding: 7px 10px; cursor: pointer; }
    button[disabled] { opacity: .45; cursor: default; }
    button[aria-pressed="true"] { background: #4b7bec; border-color: #78a0ff; }
    .icon-button { width: 34px; height: 34px; padding: 0; font-size: 17px; line-height: 1; }
    .speed-control { display: grid; grid-template-columns: auto 120px; gap: 8px; align-items: center; font-size: 13px; color: rgba(255,255,255,.86); }
    .hit-stack { margin-top: 8px; padding-top: 8px; border-top: 1px solid rgba(255,255,255,.16); color: rgba(255,255,255,.82); }
    .hit-stack div { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    input[type="range"] { accent-color: #78a0ff; }
    code { color: #b8d3ff; }
  </style>
</head>
<body>
  <canvas id="viewport"></canvas>
  <div class="toolbar">
    <button id="gfx" aria-pressed="true">GfxMap</button>
    <button id="collision" aria-pressed="false">Collision</button>
    <button id="opaque" aria-pressed="true">Opaque</button>
    <button id="wire" aria-pressed="false">Wire</button>
    <button id="reset">Reset View</button>
    <div class="divider"></div>
    <div class="control-group zoom" aria-label="Zoom controls">
      <button id="zoom-in" class="icon-button" title="Zoom in" aria-label="Zoom in">+</button>
      <button id="zoom-out" class="icon-button" title="Zoom out" aria-label="Zoom out">-</button>
    </div>
    <button id="dive">Dive</button>
    <button id="back">Back</button>
    <div class="divider"></div>
    <div class="control-group" aria-label="Pan controls">
      <span></span><button id="pan-up" class="icon-button" aria-label="Pan up">&#8593;</button><span></span>
      <button id="pan-left" class="icon-button" aria-label="Pan left">&#8592;</button>
      <button id="pan-home" class="icon-button" aria-label="Center target">&#9679;</button>
      <button id="pan-right" class="icon-button" aria-label="Pan right">&#8594;</button>
      <span></span><button id="pan-down" class="icon-button" aria-label="Pan down">&#8595;</button><span></span>
    </div>
    <div class="divider"></div>
    <label class="speed-control"><span>Step</span><input id="move-step" type="range" min="0.25" max="4" step="0.25" value="1"></label>
    <label class="speed-control"><span>Alpha</span><input id="gfx-alpha" type="range" min="0.15" max="1" step="0.05" value="1"></label>
  </div>
  <div id="status" class="status">Loading GLBs...</div>

  <script type="importmap">
    { "imports": { "three": "https://cdn.jsdelivr.net/npm/three@0.165.0/build/three.module.js", "three/addons/": "https://cdn.jsdelivr.net/npm/three@0.165.0/examples/jsm/" } }
  </script>
  <script type="module">
    import * as THREE from 'three';
    import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
    import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

    const stem = '{{Js(stem)}}';
    const hasCollision = {{collisionFlag}};
    const canvas = document.querySelector('#viewport');
    const status = document.querySelector('#status');
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.outputColorSpace = THREE.SRGBColorSpace;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x111111);
    const camera = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 1, 250000);
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.screenSpacePanning = true;
    controls.zoomSpeed = 1.6;
    controls.panSpeed = 1.4;
    controls.minDistance = 0.05;
    controls.maxDistance = 500000;
    controls.zoomToCursor = true;

    scene.add(new THREE.HemisphereLight(0xffffff, 0x222233, 1.2));
    const sun = new THREE.DirectionalLight(0xffffff, 2.0);
    sun.position.set(5000, 9000, 3000);
    scene.add(sun);

    const grid = new THREE.GridHelper(5000, 50, 0x444444, 0x222222);
    grid.position.y = -150;
    scene.add(grid);

    const loader = new GLTFLoader();
    const version = Date.now();
    const raycaster = new THREE.Raycaster();
    raycaster.params.Line.threshold = 18;
    const pointer = new THREE.Vector2();
    const root = new THREE.Group();
    scene.add(root);
    const marker = new THREE.Mesh(
      new THREE.SphereGeometry(10, 16, 8),
      new THREE.MeshBasicMaterial({ color: 0xfff06a, depthTest: false }));
    marker.visible = false;
    scene.add(marker);

    const manifest = await loadJson(`${stem}.surface-debug.json`);
    const surfaceIndex = buildSurfaceIndex(manifest.Surfaces ?? []);
    const gfx = await loadGlb(`${stem}.gfx.glb`);
    const collision = hasCollision ? await loadGlb(`${stem}.collision-debug.glb`) : new THREE.Group();
    root.add(gfx);
    root.add(collision);
    gfx.name = 'GfxMap';
    collision.name = 'Collision';
    collision.visible = false;
    document.querySelector('#collision').disabled = !hasCollision;

    rememberMaterialState(gfx);
    setGfxOpacity(1);
    setWireframe(false);
    frameScene();
    status.innerHTML = `Loaded <code>${escapeHtml(stem)}</code>. Click visible GfxMap geometry to copy its surface id; double-click to focus.`;

    document.querySelector('#gfx').addEventListener('click', (event) => {
      gfx.visible = !gfx.visible;
      event.currentTarget.setAttribute('aria-pressed', String(gfx.visible));
    });
    document.querySelector('#collision').addEventListener('click', (event) => {
      if (!hasCollision) return;
      collision.visible = !collision.visible;
      event.currentTarget.setAttribute('aria-pressed', String(collision.visible));
    });
    document.querySelector('#opaque').addEventListener('click', (event) => {
      const pressed = event.currentTarget.getAttribute('aria-pressed') !== 'true';
      event.currentTarget.setAttribute('aria-pressed', String(pressed));
      document.querySelector('#gfx-alpha').value = pressed ? '1' : '0.55';
      setGfxOpacity(pressed ? 1 : 0.55);
    });
    document.querySelector('#wire').addEventListener('click', (event) => {
      const pressed = event.currentTarget.getAttribute('aria-pressed') !== 'true';
      event.currentTarget.setAttribute('aria-pressed', String(pressed));
      setWireframe(pressed);
    });
    document.querySelector('#gfx-alpha').addEventListener('input', (event) => {
      const opacity = Number(event.currentTarget.value);
      document.querySelector('#opaque').setAttribute('aria-pressed', String(opacity >= 0.99));
      setGfxOpacity(opacity);
    });
    document.querySelector('#reset').addEventListener('click', frameScene);
    document.querySelector('#zoom-in').addEventListener('click', () => zoomBy(0.72));
    document.querySelector('#zoom-out').addEventListener('click', () => zoomBy(1.38));
    document.querySelector('#dive').addEventListener('click', () => zoomBy(0.28));
    document.querySelector('#back').addEventListener('click', () => zoomBy(2.4));
    document.querySelector('#pan-left').addEventListener('click', () => panScreen(-1, 0));
    document.querySelector('#pan-right').addEventListener('click', () => panScreen(1, 0));
    document.querySelector('#pan-up').addEventListener('click', () => panScreen(0, 1));
    document.querySelector('#pan-down').addEventListener('click', () => panScreen(0, -1));
    document.querySelector('#pan-home').addEventListener('click', centerTargetOnVisibleScene);
    canvas.addEventListener('click', inspectFromPointer);
    canvas.addEventListener('dblclick', focusFromPointer);
    canvas.addEventListener('contextmenu', (event) => event.preventDefault());

    window.addEventListener('keydown', (event) => {
      if (event.target instanceof HTMLInputElement) return;
      const fast = event.shiftKey ? 2.5 : 1;
      const actions = {
        '+': () => zoomBy(0.72), '=': () => zoomBy(0.72), '-': () => zoomBy(1.38), '_': () => zoomBy(1.38),
        ArrowLeft: () => panScreen(-fast, 0), a: () => panScreen(-fast, 0), A: () => panScreen(-fast, 0),
        ArrowRight: () => panScreen(fast, 0), d: () => panScreen(fast, 0), D: () => panScreen(fast, 0),
        ArrowUp: () => panScreen(0, fast), w: () => panScreen(0, fast), W: () => panScreen(0, fast),
        ArrowDown: () => panScreen(0, -fast), s: () => panScreen(0, -fast), S: () => panScreen(0, -fast),
        f: frameScene, F: frameScene, c: centerTargetOnVisibleScene, C: centerTargetOnVisibleScene
      };
      if (actions[event.key]) {
        actions[event.key]();
        event.preventDefault();
      }
    });

    window.addEventListener('resize', () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    });

    renderer.setAnimationLoop(() => {
      controls.update();
      renderer.render(scene, camera);
    });

    async function loadGlb(path) {
      return new Promise((resolve, reject) => {
        loader.load(`${path}?v=${version}`, (gltf) => resolve(gltf.scene), undefined, reject);
      }).catch((error) => {
        status.textContent = `Failed to load ${path}: ${error}`;
        throw error;
      });
    }

    async function loadJson(path) {
      const response = await fetch(`${path}?v=${version}`);
      if (!response.ok)
        throw new Error(`Failed to load ${path}: ${response.status}`);
      return response.json();
    }

    function buildSurfaceIndex(rows) {
      const map = new Map();
      for (const row of rows) {
        const key = row.MaterialKey || '';
        if (!map.has(key))
          map.set(key, []);
        map.get(key).push(row);
      }

      for (const list of map.values())
        list.sort((a, b) => a.PrimitiveTriangleStart - b.PrimitiveTriangleStart);
      return map;
    }

    function inspectFromPointer(event) {
      const hits = pickAll(event, [gfx]);
      if (hits.length === 0) {
        status.textContent = 'No GfxMap surface under pointer.';
        return;
      }

      const resolvedHits = hits.map(resolveHit).slice(0, 10);
      const selected = resolvedHits[0];
      const hit = selected.hit;
      const row = selected.row;
      marker.position.copy(hit.point);
      marker.visible = true;

      if (!row) {
        status.innerHTML = `Hit material <code>${escapeHtml(selected.materialName)}</code> face <code>${hit.faceIndex ?? -1}</code>, but no manifest row matched.${hitStackHtml(resolvedHits)}`;
        return;
      }

      const text = surfaceText(row, hit);
      console.log('FastFile surface pick', row, hit);
      navigator.clipboard?.writeText(`${text}\n\n${hitStackText(resolvedHits)}`).catch(() => {});
      status.innerHTML = `${escapeHtml(text).replaceAll(' | ', '<br>')}${hitStackHtml(resolvedHits)}`;
    }

    function resolveHit(hit) {
      const materialName = hitMaterialName(hit);
      return {
        hit,
        materialName,
        row: findSurface(materialName, hit.faceIndex ?? 0)
      };
    }

    function findSurface(materialName, faceIndex) {
      const rows = surfaceIndex.get(materialName) ?? [];
      return rows.find((row) =>
        faceIndex >= row.PrimitiveTriangleStart &&
        faceIndex < row.PrimitiveTriangleStart + row.PrimitiveTriangleCount);
    }

    function surfaceText(row, hit) {
      return [
        `surface=${row.SurfaceIndex}`,
        `face=${hit.faceIndex ?? -1}`,
        `point=${formatVec3(hit.point)}`,
        `material=${row.MaterialKey}`,
        `techset=${row.TechniqueSet}`,
        `format=${row.WorldVertexFormat}`,
        `uv=${row.SelectedUvDecoder || '<none>'}`,
        `texture=${row.TextureImage || '<none>'}`,
        `decoded=${row.TextureDecoded}`,
        `triangles layer=0x${hex(row.VertexLayerData)} baseVertex=${row.BaseVertex} min=${row.MinVertexIndex} verts=${row.VertexCount} tris=${row.TriCount} baseIndex=${row.BaseIndex}`,
        `firstLayerOffset=0x${hex(row.FirstLayerOffset)}`,
        `layerBytes=${row.FirstLayerBytes}`
      ].join(' | ');
    }

    function hitMaterialName(hit) {
      const material = Array.isArray(hit.object.material)
        ? hit.object.material[hit.face?.materialIndex ?? 0]
        : hit.object.material;
      return material?.name || '';
    }

    function pick(event, roots) {
      return pickAll(event, roots)[0];
    }

    function pickAll(event, roots) {
      const rect = canvas.getBoundingClientRect();
      pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
      raycaster.setFromCamera(pointer, camera);
      return raycaster
        .intersectObjects(roots, true)
        .filter((hit) => isVisibleInHierarchy(hit.object) && hit.object.isMesh);
    }

    function hitStackText(resolvedHits) {
      const lines = ['hit-stack:'];
      for (let i = 0; i < resolvedHits.length; i++) {
        const item = resolvedHits[i];
        const row = item.row;
        lines.push(`${i + 1}. surface=${row?.SurfaceIndex ?? '<unmapped>'} face=${item.hit.faceIndex ?? -1} distance=${item.hit.distance.toFixed(2)} material=${row?.MaterialKey ?? item.materialName} texture=${row?.TextureImage || '<none>'}`);
      }
      return lines.join('\n');
    }

    function hitStackHtml(resolvedHits) {
      if (resolvedHits.length <= 1)
        return '';

      const lines = resolvedHits.slice(0, 8).map((item, index) => {
        const row = item.row;
        const surface = row?.SurfaceIndex ?? '&lt;unmapped&gt;';
        const material = escapeHtml(row?.MaterialKey ?? item.materialName);
        const texture = escapeHtml(row?.TextureImage || '<none>');
        return `<div>${index + 1}. surface=<code>${surface}</code> face=<code>${item.hit.faceIndex ?? -1}</code> dist=<code>${item.hit.distance.toFixed(2)}</code> material=<code>${material}</code> texture=<code>${texture}</code></div>`;
      });
      return `<div class="hit-stack">${lines.join('')}</div>`;
    }

    function focusFromPointer(event) {
      const hit = pick(event, [root]);
      if (!hit)
        return;
      focusPoint(hit.point, 0.14);
    }

    function focusPoint(point, distanceFactor) {
      const currentTarget = controls.target.clone();
      const currentDistance = camera.position.distanceTo(currentTarget);
      const direction = camera.position.clone().sub(currentTarget).normalize();
      const nextDistance = THREE.MathUtils.clamp(currentDistance * distanceFactor, 4, camera.far * 0.4);
      controls.target.copy(point);
      camera.position.copy(point).addScaledVector(direction, nextDistance);
      camera.near = 0.05;
      camera.updateProjectionMatrix();
      controls.update();
    }

    function frameScene() {
      const box = new THREE.Box3().setFromObject(root);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());
      const radius = Math.max(size.x, size.y, size.z) * 0.62;
      controls.target.copy(center);
      camera.position.set(center.x + radius * 0.8, center.y + radius * 0.55, center.z + radius * 0.9);
      camera.near = Math.max(radius / 10000, 0.1);
      camera.far = Math.max(radius * 8, 10000);
      camera.updateProjectionMatrix();
      controls.update();
    }

    function centerTargetOnVisibleScene() {
      const box = new THREE.Box3().setFromObject(root);
      if (box.isEmpty()) return;
      const center = box.getCenter(new THREE.Vector3());
      const delta = center.clone().sub(controls.target);
      controls.target.copy(center);
      camera.position.add(delta);
      controls.update();
    }

    function zoomBy(factor) {
      const offset = camera.position.clone().sub(controls.target);
      const distance = offset.length();
      const nextDistance = THREE.MathUtils.clamp(distance * factor, 0.5, camera.far * 0.45);
      offset.setLength(nextDistance);
      camera.position.copy(controls.target).add(offset);
      camera.near = Math.min(0.05, nextDistance * 0.01);
      camera.updateProjectionMatrix();
      controls.update();
    }

    function panScreen(xSteps, ySteps) {
      const step = panStep();
      const right = new THREE.Vector3().setFromMatrixColumn(camera.matrix, 0).normalize();
      const up = new THREE.Vector3().setFromMatrixColumn(camera.matrix, 1).normalize();
      const delta = new THREE.Vector3().addScaledVector(right, xSteps * step).addScaledVector(up, ySteps * step);
      camera.position.add(delta);
      controls.target.add(delta);
      controls.update();
    }

    function panStep() {
      const multiplier = Number(document.querySelector('#move-step').value);
      const distance = camera.position.distanceTo(controls.target);
      return Math.max(distance * 0.06 * multiplier, 4);
    }

    function setGfxOpacity(opacity) {
      gfx.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        for (const material of materials) {
          const baseOpacity = material.userData.viewerBaseOpacity ?? 1;
          const baseTransparent = material.userData.viewerBaseTransparent === true;
          const baseDepthWrite = material.userData.viewerBaseDepthWrite ?? true;
          const faded = opacity < 0.99;
          material.opacity = baseOpacity * opacity;
          material.transparent = baseTransparent || faded;
          material.depthWrite = baseTransparent || faded ? false : baseDepthWrite;
          material.needsUpdate = true;
        }
      });
    }

    function rememberMaterialState(group) {
      group.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        for (const material of materials) {
          material.userData.viewerBaseOpacity = material.opacity ?? 1;
          material.userData.viewerBaseTransparent = material.transparent === true;
          material.userData.viewerBaseDepthWrite = material.depthWrite;
        }
      });
    }

    function setWireframe(enabled) {
      gfx.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        for (const material of materials) {
          material.wireframe = enabled;
          material.needsUpdate = true;
        }
      });
    }

    function formatVec3(value) {
      return `(${value.x.toFixed(1)}, ${value.y.toFixed(1)}, ${value.z.toFixed(1)})`;
    }

    function hex(value) {
      if (!Number.isFinite(value) || value < 0)
        return String(value);
      return Math.trunc(value).toString(16).toUpperCase();
    }

    function escapeHtml(value) {
      return String(value).replace(/[&<>"']/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch]);
    }

    function isVisibleInHierarchy(object) {
      for (let current = object; current; current = current.parent) {
        if (!current.visible) return false;
      }
      return true;
    }
  </script>
</body>
</html>
""";
    }

    private static string Html(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private static string Js(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            builder.Append(ch switch
            {
                '\\' => "\\\\",
                '\'' => "\\'",
                '\n' => "\\n",
                '\r' => "\\r",
                _ => ch
            });
        }

        return builder.ToString();
    }
}
