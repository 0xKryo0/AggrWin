from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

SIZE = 1024
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "AggrWin.ico"
OUT.parent.mkdir(parents=True, exist_ok=True)

# Dark rounded tile.
im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
tile = Image.new("RGBA", (SIZE, SIZE), (5, 7, 13, 255))
mask = Image.new("L", (SIZE, SIZE), 0)
ImageDraw.Draw(mask).rounded_rectangle((8, 8, SIZE - 8, SIZE - 8), radius=210, fill=255)
im.paste(tile, (0, 0), mask)

def gradient_polygon(points, top, bottom):
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    grad = Image.new("RGBA", (SIZE, SIZE))
    px = grad.load()
    for y in range(SIZE):
        t = y / (SIZE - 1)
        color = tuple(round(top[i] * (1 - t) + bottom[i] * t) for i in range(4))
        for x in range(SIZE):
            px[x, y] = color
    poly_mask = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(poly_mask).polygon(points, fill=255)
    layer.paste(grad, (0, 0), poly_mask)
    return layer

# Silver left stroke and electric-blue right stroke form a clean geometric A.
left = [(126, 795), (452, 238), (585, 238), (281, 795)]
right = [(570, 238), (896, 795), (744, 795), (505, 390)]
bar = [(457, 585), (690, 585), (752, 695), (394, 695)]

im = Image.alpha_composite(im, gradient_polygon(left, (255, 255, 255, 255), (145, 157, 180, 255)))
im = Image.alpha_composite(im, gradient_polygon(right, (0, 183, 255, 255), (0, 54, 225, 255)))
im = Image.alpha_composite(im, gradient_polygon(bar, (0, 190, 255, 255), (8, 42, 170, 255)))

# Supersampled multi-resolution Windows icon.
im = im.resize((256, 256), Image.Resampling.LANCZOS)
im.save(OUT, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print(f"Generated {OUT}")
