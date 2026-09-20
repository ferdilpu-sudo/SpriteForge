from __future__ import annotations

import argparse
from io import BytesIO
from pathlib import Path

from PIL import Image
from rembg import remove


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="SpriteForge local background-removal worker")
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--alpha-threshold", type=float, default=0.05)
    return parser.parse_args()


def apply_alpha_threshold(image: Image.Image, threshold: float) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    cutoff = max(0, min(255, round(threshold * 255)))
    alpha = alpha.point(lambda value: 0 if value < cutoff else value)
    rgba.putalpha(alpha)
    return rgba


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    output_path = Path(args.output)
    if not input_path.is_file():
        raise FileNotFoundError(f"Input file does not exist: {input_path}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    result = remove(input_path.read_bytes())
    with Image.open(BytesIO(result)) as image:
        processed = apply_alpha_threshold(image, args.alpha_threshold)
        processed.save(output_path, format="PNG")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
