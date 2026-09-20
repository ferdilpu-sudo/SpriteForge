from __future__ import annotations

import argparse
import shutil


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--alpha-threshold", required=True)
    args = parser.parse_args()
    shutil.copyfile(args.input, args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
