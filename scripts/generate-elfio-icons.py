"""Generate Elfio Studio icons with transparent background (like Hoblio)."""
from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image

ASSETS = Path(__file__).resolve().parents[1] / "src" / "Hoblio.Studio" / "Assets"
PUBLIC = Path(__file__).resolve().parents[1] / "src" / "Hoblio.Studio" / "flow-canvas" / "public"
SOURCE = ASSETS / "elf_domestic.webp"


def remove_background_flood(img: Image.Image, bg: tuple[int, int, int] = (255, 255, 255), tol: int = 28) -> Image.Image:
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()

    def is_bg(r: int, g: int, b: int) -> bool:
        return abs(r - bg[0]) <= tol and abs(g - bg[1]) <= tol and abs(b - bg[2]) <= tol

    seen = [[False] * w for _ in range(h)]
    q: deque[tuple[int, int]] = deque()

    for x in range(w):
        for y in (0, h - 1):
            if is_bg(*px[x, y][:3]):
                q.append((x, y))
                seen[x][y] = True
    for y in range(h):
        for x in (0, w - 1):
            if not seen[x][y] and is_bg(*px[x, y][:3]):
                q.append((x, y))
                seen[x][y] = True

    while q:
        x, y = q.popleft()
        px[x, y] = (px[x, y][0], px[x, y][1], px[x, y][2], 0)
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and is_bg(*px[nx, ny][:3]):
                seen[nx][ny] = True
                q.append((nx, ny))

    return img


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Missing source image: {SOURCE}")

    ASSETS.mkdir(parents=True, exist_ok=True)
    img = remove_background_flood(Image.open(SOURCE))

    img.save(ASSETS / "elfio.ico", format="ICO", sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
    img.resize((256, 256), Image.Resampling.LANCZOS).save(ASSETS / "elfio.png")
    img.resize((32, 32), Image.Resampling.LANCZOS).save(ASSETS / "elfio-32.png")

    PUBLIC.mkdir(parents=True, exist_ok=True)
    (PUBLIC / "favicon.png").write_bytes((ASSETS / "elfio-32.png").read_bytes())

    print("Generated elfio.ico, elfio.png, elfio-32.png, flow-canvas/public/favicon.png (transparent bg)")


if __name__ == "__main__":
    main()
