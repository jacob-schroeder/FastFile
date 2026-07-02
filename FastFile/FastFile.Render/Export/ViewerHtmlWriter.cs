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
    controls.enableZoom = false;
    controls.enablePan = false;
    controls.enableRotate = false;
    controls.enabled = false;
    controls.zoomSpeed = 1.6;
    controls.panSpeed = 1.4;
    controls.minDistance = 0.05;
    controls.maxDistance = 500000;
    controls.zoomToCursor = true;
    controls.mouseButtons = {
      LEFT: THREE.MOUSE.NONE,
      MIDDLE: THREE.MOUSE.NONE,
      RIGHT: THREE.MOUSE.NONE
    };
    controls.touches = {
      ONE: THREE.TOUCH.NONE,
      TWO: THREE.TOUCH.NONE
    };
    camera.rotation.order = 'YXZ';
    let lastFrameMs = null;
    const lookSensitivity = 0.003;
    const movePlaneUp = new THREE.Vector3(0, 1, 0);
    const moveState = {
      forward: false,
      backward: false,
      left: false,
      right: false,
      boost: false
    };
    let yaw = 0;
    let pitch = 0;
    let isSurfaceWindowVisible = true;
    let mouseLook = null;
    let suppressNextClick = false;
    let lastTouchLook = null;

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
    const multiplyMaterials = buildMultiplyMaterialSet(manifest.Surfaces ?? []);
    const additiveMaterials = buildAdditiveMaterialSet(manifest.Surfaces ?? []);
    const additiveAlphaMaterials = buildAdditiveAlphaMaterialSet(manifest.Surfaces ?? []);
    const skyMaterials = buildSkyMaterialSet(manifest.Surfaces ?? []);
    const layerBlendMaterials = await buildLayerBlendMaterialMap(manifest.Surfaces ?? []);
    const gfx = await loadGlb(`${stem}.gfx.glb`);
    const collision = hasCollision ? await loadGlb(`${stem}.collision-debug.glb`) : new THREE.Group();
    const skybox = await buildSkybox(manifest.SkyImages ?? []);
    if (skybox)
      scene.add(skybox);
    root.add(gfx);
    root.add(collision);
    gfx.name = 'GfxMap';
    collision.name = 'Collision';
    collision.visible = false;
    document.querySelector('#collision').disabled = !hasCollision;

    applyMultiplyMaterials(gfx, multiplyMaterials);
    applyLayerBlendMaterials(gfx, layerBlendMaterials);
    applyAdditiveMaterials(gfx, additiveMaterials);
    applyAdditiveAlphaMaterials(gfx, additiveAlphaMaterials);
    applySkyMaterials(gfx, skyMaterials);
    rememberMaterialState(gfx);
    setGfxOpacity(1);
    setWireframe(false);
    frameScene();
    syncCameraAnglesFromView();
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
    canvas.addEventListener('click', (event) => {
      if (suppressNextClick) {
        suppressNextClick = false;
        event.preventDefault();
        return;
      }
      inspectFromPointer(event);
    });
    canvas.addEventListener('dblclick', focusFromPointer);
    canvas.addEventListener('contextmenu', (event) => event.preventDefault());
    canvas.addEventListener('pointerdown', (event) => {
      if (event.pointerType !== 'mouse' || event.button !== 0)
        return;
      mouseLook = { x: event.clientX, y: event.clientY, moved: false };
      canvas.setPointerCapture(event.pointerId);
    });
    canvas.addEventListener('pointermove', (event) => {
      if (!mouseLook || event.pointerType !== 'mouse')
        return;
      const moveX = event.clientX - mouseLook.x;
      const moveY = event.clientY - mouseLook.y;
      if (!moveX && !moveY)
        return;
      mouseLook = { x: event.clientX, y: event.clientY, moved: mouseLook.moved || Math.hypot(moveX, moveY) > 2 };
      lookBy(moveX, moveY);
      event.preventDefault();
    });
    canvas.addEventListener('pointerup', (event) => {
      if (!mouseLook || event.pointerType !== 'mouse')
        return;
      if (mouseLook.moved) {
        suppressNextClick = true;
        setTimeout(() => { suppressNextClick = false; }, 0);
      }
      mouseLook = null;
      if (canvas.hasPointerCapture(event.pointerId))
        canvas.releasePointerCapture(event.pointerId);
    });
    canvas.addEventListener('pointercancel', () => {
      mouseLook = null;
    });
    canvas.addEventListener('wheel', (event) => {
      if (event.target instanceof HTMLInputElement) return;
      moveCameraByWheel(event.deltaY < 0 ? 1 : -1);
      event.preventDefault();
    }, { passive: false });
    window.addEventListener('blur', () => {
      mouseLook = null;
      lastTouchLook = null;
    });
    canvas.addEventListener('touchstart', (event) => {
      if (event.touches.length !== 2)
        return;
      const first = event.touches[0];
      const second = event.touches[1];
      lastTouchLook = {
        x: (first.clientX + second.clientX) * 0.5,
        y: (first.clientY + second.clientY) * 0.5
      };
      event.preventDefault();
    }, { passive: false });
    canvas.addEventListener('touchmove', (event) => {
      if (event.touches.length !== 2)
        return;
      if (!lastTouchLook)
        return;
      const first = event.touches[0];
      const second = event.touches[1];
      const x = (first.clientX + second.clientX) * 0.5;
      const y = (first.clientY + second.clientY) * 0.5;
      const moveX = x - lastTouchLook.x;
      const moveY = y - lastTouchLook.y;
      lastTouchLook = { x, y };

      lookBy(moveX, moveY);
      event.preventDefault();
    }, { passive: false });
    canvas.addEventListener('touchend', (event) => {
      if (event.touches.length === 2)
        return;
      lastTouchLook = null;
    }, { passive: false });

    window.addEventListener('keydown', (event) => {
      if (event.target instanceof HTMLInputElement) return;
      const key = event.key.toLowerCase();
      if (key === 'w') moveState.forward = true;
      if (key === 's') moveState.backward = true;
      if (key === 'a') moveState.left = true;
      if (key === 'd') moveState.right = true;
      if (key === 'shift') moveState.boost = true;
      const fast = event.shiftKey ? 2.5 : 1;
      const actions = {
        '+': () => zoomBy(0.72), '=': () => zoomBy(0.72), '-': () => zoomBy(1.38), '_': () => zoomBy(1.38),
        ArrowLeft: () => panScreen(-fast, 0),
        ArrowRight: () => panScreen(fast, 0),
        ArrowUp: () => panScreen(0, fast),
        ArrowDown: () => panScreen(0, -fast),
        f: frameScene, F: frameScene
      };
      if (actions[event.key]) {
        actions[event.key]();
        event.preventDefault();
      }

      if (key === 'q' || event.code === 'KeyQ') {
        toggleSurfaceWindow();
        event.preventDefault();
      }

      if (key === 'c' || event.code === 'KeyC') {
        copySurfaceWindowText();
        event.preventDefault();
      }
    });

    window.addEventListener('keyup', (event) => {
      if (event.target instanceof HTMLInputElement) return;
      const key = event.key.toLowerCase();
      if (key === 'w') moveState.forward = false;
      if (key === 's') moveState.backward = false;
      if (key === 'a') moveState.left = false;
      if (key === 'd') moveState.right = false;
      if (key === 'shift') moveState.boost = false;
    });

    window.addEventListener('resize', () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    });

    renderer.setAnimationLoop((time) => {
      const deltaMs = lastFrameMs == null ? 0 : time - lastFrameMs;
      lastFrameMs = time;
      moveByInput(Math.min(deltaMs, 60) * 0.001);
      if (skybox)
        skybox.position.copy(camera.position);
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

    function textureUrl(path) {
      return path.split('/').map((part) => encodeURIComponent(part)).join('/');
    }

    function samplerRepeats(samplerState) {
      if (typeof samplerState !== 'string')
        return true;
      const value = Number.parseInt(samplerState, 16);
      if (!Number.isFinite(value))
        return true;
      return (value & 0x40) === 0 && (value & 0x80) === 0;
    }

    async function loadTexture(path, repeat = true) {
      return new Promise((resolve, reject) => {
        new THREE.TextureLoader().load(`${textureUrl(path)}?v=${version}`, (texture) => {
          texture.colorSpace = THREE.SRGBColorSpace;
          texture.wrapS = repeat ? THREE.RepeatWrapping : THREE.ClampToEdgeWrapping;
          texture.wrapT = repeat ? THREE.RepeatWrapping : THREE.ClampToEdgeWrapping;
          resolve(texture);
        }, undefined, reject);
      });
    }

    async function buildSkybox(images) {
      const decoded = images.filter((image) => image.Decoded && image.Path);
      if (decoded.length === 0)
        return null;

      const byFace = new Map(decoded.map((image) => [String(image.Face || '').toLowerCase(), image]));
      const fallback = byFace.get('ft') ?? decoded[0];
      const faceSlots = [
        { face: 'rt', rotation: -Math.PI / 2 },
        { face: 'lf', rotation: -Math.PI / 2 },
        { face: 'up', rotation: -Math.PI / 2 },
        { face: 'dn', rotation: -Math.PI / 2 },
        { face: 'ft', rotation: -Math.PI / 2 },
        { face: 'bk', rotation: -Math.PI / 2 }
      ];
      const textures = await Promise.all(faceSlots.map(async (slot) => {
        const image = byFace.get(slot.face) ?? fallback;
        const texture = await loadTexture(image.Path, samplerRepeats(image.SamplerState));
        texture.center.set(0.5, 0.5);
        texture.rotation = slot.rotation;
        texture.needsUpdate = true;
        return texture;
      }));
      const materials = textures.map((texture) => new THREE.MeshBasicMaterial({
        map: texture,
        side: THREE.BackSide,
        depthWrite: false,
        depthTest: false,
        fog: false
      }));
      const sky = new THREE.Mesh(new THREE.BoxGeometry(200000, 200000, 200000), materials);
      sky.name = 'Skybox';
      sky.renderOrder = -1000;
      sky.frustumCulled = false;
      return sky;
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

    function buildMultiplyMaterialSet(rows) {
      const names = new Set();
      for (const row of rows) {
        if (String(row.TechniqueSet || '').toLowerCase().includes('multiply') && row.MaterialKey)
          names.add(row.MaterialKey);
      }
      return names;
    }

    function buildAdditiveMaterialSet(rows) {
      const names = new Set();
      for (const row of rows) {
        if (row.RenderBlend === 'additive' && row.RenderAlpha !== 'intensity' && row.MaterialKey)
          names.add(row.MaterialKey);
      }
      return names;
    }

    function buildAdditiveAlphaMaterialSet(rows) {
      const names = new Set();
      for (const row of rows) {
        if (row.RenderBlend === 'additive' && row.RenderAlpha === 'intensity' && row.MaterialKey)
          names.add(row.MaterialKey);
      }
      return names;
    }

    function buildSkyMaterialSet(rows) {
      const names = new Set();
      for (const row of rows) {
        if (row.TechniqueSet === 'wc_sky' && row.MaterialKey)
          names.add(row.MaterialKey);
      }
      return names;
    }

    async function buildLayerBlendMaterialMap(rows) {
      const specs = new Map();
      for (const row of rows) {
        if (!row.MaterialKey || !row.LayerTextureDecoded || !row.LayerTexturePath)
          continue;
        if (!specs.has(row.MaterialKey))
          specs.set(row.MaterialKey, {
            path: row.LayerTexturePath,
            samplerState: row.LayerTextureSamplerState,
            baseTint: parseVec4(row.LayerBaseTint),
            layerTint: parseVec4(row.LayerTextureTint)
          });
      }

      const materials = new Map();
      await Promise.all([...specs].map(async ([name, spec]) => {
        materials.set(name, {
          texture: await loadTexture(spec.path, samplerRepeats(spec.samplerState)),
          baseTint: spec.baseTint,
          layerTint: spec.layerTint
        });
      }));
      return materials;
    }

    function parseVec4(text) {
      if (typeof text !== 'string')
        return new THREE.Vector4(1, 1, 1, 1);
      const values = text.trim().split(/\s+/).map(Number);
      return values.length >= 4 && values.every(Number.isFinite)
        ? new THREE.Vector4(values[0], values[1], values[2], values[3])
        : new THREE.Vector4(1, 1, 1, 1);
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
        `textureSlot=${row.TextureSlot || '<none>'}`,
        row.TextureSamplerState ? `textureSampler=${row.TextureSamplerState}` : '',
        `texture=${row.TextureImage || '<none>'}`,
        `decoded=${row.TextureDecoded}`,
        row.LayerTextureSlot ? `layerTextureSlot=${row.LayerTextureSlot}` : '',
        row.LayerTextureSamplerState ? `layerTextureSampler=${row.LayerTextureSamplerState}` : '',
        row.LayerTextureImage ? `layerTexture=${row.LayerTextureImage}` : '',
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
        lines.push(`${i + 1}. surface=${row?.SurfaceIndex ?? '<unmapped>'} face=${item.hit.faceIndex ?? -1} distance=${item.hit.distance.toFixed(2)} material=${row?.MaterialKey ?? item.materialName} textureSlot=${row?.TextureSlot || '<none>'} texture=${row?.TextureImage || '<none>'}`);
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
        const textureSlot = escapeHtml(row?.TextureSlot || '<none>');
        const texture = escapeHtml(row?.TextureImage || '<none>');
        return `<div>${index + 1}. surface=<code>${surface}</code> face=<code>${item.hit.faceIndex ?? -1}</code> dist=<code>${item.hit.distance.toFixed(2)}</code> material=<code>${material}</code> textureSlot=<code>${textureSlot}</code> texture=<code>${texture}</code></div>`;
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
      syncCameraAnglesFromView();
      applyMouseLook();
      controls.update();
    }

    function frameScene() {
      const target = new THREE.Vector3(0, 0, 0);
      const height = 1400;
      controls.target.copy(target);
      camera.position.set(0, height, height * 1.35);
      camera.near = 0.1;
      camera.far = 250000;
      camera.updateProjectionMatrix();
      syncCameraAnglesFromView();
      applyMouseLook();
      controls.update();
    }

    function syncCameraAnglesFromView() {
      const forward = camera.getWorldDirection(new THREE.Vector3());
      yaw = Math.atan2(forward.x, forward.z);
      pitch = Math.asin(THREE.MathUtils.clamp(forward.y, -0.999, 0.999));
    }

    function toggleSurfaceWindow() {
      isSurfaceWindowVisible = !isSurfaceWindowVisible;
      status.style.display = isSurfaceWindowVisible ? 'block' : 'none';
    }

    function copySurfaceWindowText() {
      const text = surfaceWindowText();
      if (!text)
        return;
      navigator.clipboard?.writeText(text).catch(() => copyTextFallback(text)) ?? copyTextFallback(text);
    }

    function surfaceWindowText() {
      const clone = status.cloneNode(true);
      clone.querySelectorAll('br').forEach((br) => br.replaceWith('\n'));
      clone.querySelectorAll('div').forEach((div) => div.append('\n'));
      return clone.textContent.trim();
    }

    function copyTextFallback(text) {
      const textarea = document.createElement('textarea');
      textarea.value = text;
      textarea.setAttribute('readonly', '');
      textarea.style.position = 'fixed';
      textarea.style.left = '-9999px';
      document.body.append(textarea);
      textarea.select();
      document.execCommand('copy');
      textarea.remove();
    }

    function lookBy(moveX, moveY) {
      yaw += moveX * lookSensitivity;
      pitch += moveY * lookSensitivity;
      pitch = THREE.MathUtils.clamp(pitch, -Math.PI / 2 + 0.01, Math.PI / 2 - 0.01);
      applyMouseLook();
    }

    function applyMouseLook() {
      camera.rotation.set(pitch, yaw, 0, 'YXZ');
      const forward = new THREE.Vector3(0, 0, -1).applyQuaternion(camera.quaternion);
      controls.target.copy(camera.position).addScaledVector(forward, 128);
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
      syncCameraAnglesFromView();
      applyMouseLook();
      controls.update();
    }

    function moveByInput(deltaSeconds) {
      if (deltaSeconds <= 0)
        return;

      const forwardAxis = (moveState.forward ? 1 : 0) - (moveState.backward ? 1 : 0);
      const rightAxis = (moveState.right ? 1 : 0) - (moveState.left ? 1 : 0);
      if (!forwardAxis && !rightAxis)
        return;

      const forward = camera.getWorldDirection(new THREE.Vector3());
      if (!forward.lengthSq())
        return;
      forward.normalize();
      const right = new THREE.Vector3().crossVectors(forward, movePlaneUp).normalize();

      const direction = forward.multiplyScalar(forwardAxis).addScaledVector(right, rightAxis);
      if (!direction.lengthSq())
        return;
      direction.normalize();

      const multiplier = Number(document.querySelector('#move-step').value);
      const baseSpeed = Math.max(camera.position.length() * 0.12, 60);
      const speed = baseSpeed * multiplier * (moveState.boost ? 2.5 : 1);
      const distance = direction.multiplyScalar(deltaSeconds * speed);

      camera.position.add(distance);
      controls.target.add(distance);
      applyMouseLook();
    }

    function moveCameraByWheel(direction) {
      const forward = new THREE.Vector3();
      camera.getWorldDirection(forward);
      const distance = camera.position.distanceTo(controls.target);
      const step = Math.max(distance * 0.08, 4);
      camera.position.addScaledVector(forward, step * direction);
      controls.target.addScaledVector(forward, step * direction);
      applyMouseLook();
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

    function applyMultiplyMaterials(group, materialNames) {
      group.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        let hasMultiply = false;
        for (const material of materials) {
          if (!materialNames.has(material.name))
            continue;

          material.transparent = true;
          material.depthWrite = false;
          material.depthTest = true;
          material.vertexColors = !!object.geometry?.getAttribute('color');
          material.blending = THREE.CustomBlending;
          material.blendEquation = THREE.AddEquation;
          material.blendSrc = THREE.DstColorFactor;
          material.blendDst = THREE.ZeroFactor;
          material.onBeforeCompile = (shader) => {
            shader.fragmentShader = shader.fragmentShader.replace(
              '#include <opaque_fragment>',
              'gl_FragColor = vec4(diffuseColor.rgb, 1.0);');
          };
          material.customProgramCacheKey = () => 'unlit-multiply-v7';
          material.polygonOffset = true;
          material.polygonOffsetFactor = -1;
          material.polygonOffsetUnits = -1;
          material.needsUpdate = true;
          hasMultiply = true;
        }

        if (hasMultiply)
          object.renderOrder = Math.max(object.renderOrder || 0, 10);
      });
    }

    function applyLayerBlendMaterials(group, materialTextures) {
      group.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        for (const material of materials) {
          const layerSpec = materialTextures.get(material.name);
          if (!layerSpec)
            continue;
          if (object.geometry?.getAttribute('uv1') && !object.geometry.getAttribute('uv2'))
            object.geometry.setAttribute('uv2', object.geometry.getAttribute('uv1'));
          if (!object.geometry?.getAttribute('uv2'))
            continue;

          material.vertexColors = false;
          material.onBeforeCompile = (shader) => {
            shader.uniforms.layerMap = { value: layerSpec.texture };
            shader.uniforms.layerBaseTint = { value: layerSpec.baseTint };
            shader.uniforms.layerTextureTint = { value: layerSpec.layerTint };
        shader.vertexShader = shader.vertexShader
          .replace(
            '#include <common>',
            '#include <common>\nattribute vec2 uv2;\nvarying vec2 vLayerMapUv;')
          .replace(
            '#include <begin_vertex>',
            '#include <begin_vertex>\nvLayerMapUv = uv2;');
        shader.fragmentShader = shader.fragmentShader
          .replace(
            '#include <common>',
            '#include <common>\nuniform sampler2D layerMap;\nuniform vec4 layerBaseTint;\nuniform vec4 layerTextureTint;\nvarying vec2 vLayerMapUv;')
          .replace(
            '#include <map_fragment>',
            [
              '#include <map_fragment>',
              'vec4 layerDiffuseColor = texture2D(layerMap, vLayerMapUv);',
              'vec3 baseLayerColor = diffuseColor.rgb * layerBaseTint.rgb;',
              'vec3 secondaryLayerColor = layerDiffuseColor.rgb * layerTextureTint.rgb;',
              'float baseLayerScale = layerDiffuseColor.a * layerDiffuseColor.b;',
              'float secondaryLayerScale = layerDiffuseColor.a * layerDiffuseColor.a;',
              'diffuseColor.rgb = mix(baseLayerColor * baseLayerScale, layerDiffuseColor.rgb * secondaryLayerColor, secondaryLayerScale);'
            ].join('\n'));
      };
      material.customProgramCacheKey = () => `layer-blend-v4-${material.name}`;
          material.needsUpdate = true;
        }
      });
    }

    function applyAdditiveMaterials(group, materialNames) {
      group.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        let hasAdditive = false;
        for (const material of materials) {
          if (!materialNames.has(material.name))
            continue;

          material.transparent = true;
          material.depthWrite = false;
          material.depthTest = true;
          material.vertexColors = !!object.geometry?.getAttribute('color');
          material.blending = THREE.CustomBlending;
          material.blendEquation = THREE.AddEquation;
          material.blendSrc = THREE.OneFactor;
          material.blendDst = THREE.OneFactor;
          material.onBeforeCompile = (shader) => {
            shader.fragmentShader = shader.fragmentShader.replace(
              '#include <opaque_fragment>',
              'gl_FragColor = vec4(diffuseColor.rgb, 1.0);');
          };
          material.customProgramCacheKey = () => 'unlit-add-v1';
          material.polygonOffset = true;
          material.polygonOffsetFactor = -1;
          material.polygonOffsetUnits = -1;
          material.needsUpdate = true;
          hasAdditive = true;
        }

        if (hasAdditive)
          object.renderOrder = Math.max(object.renderOrder || 0, 10);
      });
    }

    function applyAdditiveAlphaMaterials(group, materialNames) {
      group.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        let hasAdditiveAlpha = false;
        for (const material of materials) {
          if (!materialNames.has(material.name))
            continue;

          material.transparent = true;
          material.depthWrite = false;
          material.depthTest = true;
          material.blending = THREE.CustomBlending;
          material.blendEquation = THREE.AddEquation;
          material.blendSrc = THREE.SrcAlphaFactor;
          material.blendDst = THREE.OneFactor;
          material.onBeforeCompile = (shader) => {
            shader.fragmentShader = shader.fragmentShader.replace(
              '#include <opaque_fragment>',
              [
                'float additiveAlpha = diffuseColor.a * dot(diffuseColor.rgb, vec3(0.2126, 0.7152, 0.0722));',
                'gl_FragColor = vec4(diffuseColor.rgb, additiveAlpha);'
              ].join('\n'));
          };
          material.customProgramCacheKey = () => 'unlit-add-alpha-v1';
          material.polygonOffset = true;
          material.polygonOffsetFactor = -1;
          material.polygonOffsetUnits = -1;
          material.needsUpdate = true;
          hasAdditiveAlpha = true;
        }

        if (hasAdditiveAlpha)
          object.renderOrder = Math.max(object.renderOrder || 0, 10);
      });
    }

    function applySkyMaterials(group, materialNames) {
      group.traverse((object) => {
        if (!object.material) return;
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        for (const material of materials) {
          if (materialNames.has(material.name))
            material.visible = false;
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
