"""Generate original seamless road detail and a soft smoke sprite; no external images."""
from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[1] / 'Assets/Resources/SurfaceTextures'
root.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(420)
noise = rng.random((512, 512))
smooth = sum(np.roll(np.roll(noise, x, 0), y, 1) for x in range(-2, 3) for y in range(-2, 3)) / 25
grain = .23 + noise * .15 + smooth * .15
grain += (rng.random((512, 512)) > .99) * .16
rgb = np.stack([grain * .91, grain * .95, grain], axis=2)
Image.fromarray(np.uint8(np.clip(rgb, 0, 1) * 255)).save(root / 'Asphalt.png')
dx = (np.roll(noise, 1, 1) - np.roll(noise, -1, 1)) * .3
dy = (np.roll(noise, 1, 0) - np.roll(noise, -1, 0)) * .3
normal = np.stack([dx, dy, np.ones_like(dx)], axis=2)
normal /= np.linalg.norm(normal, axis=2, keepdims=True)
Image.fromarray(np.uint8((normal * .5 + .5) * 255)).save(root / 'AsphaltNormal.png')
y, x = np.mgrid[-1:1:128j, -1:1:128j]
alpha = np.clip(1 - np.sqrt(x*x + y*y), 0, 1) ** 1.7
smoke = np.ones((128, 128, 4)); smoke[:, :, 3] = alpha
Image.fromarray(np.uint8(smoke * 255)).save(root / 'Smoke.png')
for name, size in [('Asphalt.png', (512, 512)), ('AsphaltNormal.png', (512, 512)), ('Smoke.png', (128, 128))]:
    with Image.open(root / name) as image:
        assert image.size == size
        image.verify()
print('PASS three generated surface images decode with expected dimensions.')
