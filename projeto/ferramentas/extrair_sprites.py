"""Extrai Sprites/Texture2D/TextAssets/Spine dos arquivos Unity do APK descompactado.

Uso:
    projeto/.venv/Scripts/python -I projeto/ferramentas/extrair_sprites.py \
        projeto/analise/apk_unpacked/assets projeto/analise/extraido

Saídas (em <destino>):
    sprites/<pasta_origem>/<nome>.png    Sprites recortados do atlas
    texturas/<pasta_origem>/<nome>.png   Texture2D sem Sprite associado (atlas, fundos)
    textos/<pasta_origem>/<nome>.<ext>   TextAssets (json, atlas Spine, configs)
    inventario.json                      Metadados de tudo que foi extraído

Os arquivos extraídos são de terceiros: servem só como referência local de
layout/tamanho e não devem ser versionados, publicados nem usados no jogo final.
"""
import json
import re
import sys
from pathlib import Path

import UnityPy

UNITY_FILE_HINTS = (".bundle", ".unity3d", ".assets", ".resource", ".resS")
SAFE_NAME = re.compile(r"[^A-Za-z0-9_.-]+")


def safe(name: str) -> str:
    return SAFE_NAME.sub("_", name).strip("_") or "sem_nome"


def is_candidate(path: Path) -> bool:
    if path.is_dir():
        return False
    if path.suffix in UNITY_FILE_HINTS:
        return True
    # ContentPacks e "unity default resources" não têm extensão
    return "ContentPacks" in path.parts or path.name.startswith(("level", "sharedassets", "data"))


def unique(path: Path) -> Path:
    if not path.exists():
        return path
    i = 1
    while True:
        alt = path.with_name(f"{path.stem}__{i}{path.suffix}")
        if not alt.exists():
            return alt
        i += 1


def origin_label(src: Path, root: Path) -> str:
    rel = src.relative_to(root)
    first = rel.parts[0] if len(rel.parts) > 1 else rel.stem
    if first == "ContentPacks":
        return safe(rel.parts[1])
    if first == "aa":
        return "addressables_" + safe(rel.stem.split("_assets_")[0])
    return safe(first if first != "bin" else "base")


def extract_file(src: Path, root: Path, out: Path, inventory: list, seen_textures: set) -> None:
    try:
        env = UnityPy.load(str(src))
    except Exception as exc:  # arquivo não-Unity ou corrompido
        print(f"[pula] {src}: {exc}", file=sys.stderr)
        return
    label = origin_label(src, root)
    sprite_textures = set()

    for obj in env.objects:
        kind = obj.type.name
        try:
            if kind == "Sprite":
                data = obj.read()
                img = data.image
                dest = unique(out / "sprites" / label / f"{safe(data.m_Name)}.png")
                dest.parent.mkdir(parents=True, exist_ok=True)
                img.save(dest)
                tex = data.m_RD.texture
                if tex and tex.path_id:
                    sprite_textures.add(tex.path_id)
                inventory.append({
                    "tipo": "sprite", "nome": data.m_Name, "origem": label,
                    "arquivo": dest.relative_to(out).as_posix(),
                    "largura": img.width, "altura": img.height,
                })
            elif kind == "TextAsset":
                data = obj.read()
                raw = data.m_Script
                content = raw.encode("utf-8", "surrogateescape") if isinstance(raw, str) else bytes(raw)
                name = safe(data.m_Name)
                ext = "" if "." in name else ".txt"
                dest = unique(out / "textos" / label / f"{name}{ext}")
                dest.parent.mkdir(parents=True, exist_ok=True)
                dest.write_bytes(content)
                inventory.append({
                    "tipo": "texto", "nome": data.m_Name, "origem": label,
                    "arquivo": dest.relative_to(out).as_posix(), "bytes": len(content),
                })
        except Exception as exc:
            print(f"[erro] {kind} em {src.name}: {exc}", file=sys.stderr)

    # Texturas soltas (fundos, atlas Spine) que não viraram Sprite
    for obj in env.objects:
        if obj.type.name != "Texture2D" or obj.path_id in sprite_textures:
            continue
        try:
            data = obj.read()
            key = (label, data.m_Name, data.m_Width, data.m_Height)
            if key in seen_textures or data.m_Width < 16 or data.m_Height < 16:
                continue
            seen_textures.add(key)
            img = data.image
            dest = unique(out / "texturas" / label / f"{safe(data.m_Name)}.png")
            dest.parent.mkdir(parents=True, exist_ok=True)
            img.save(dest)
            inventory.append({
                "tipo": "textura", "nome": data.m_Name, "origem": label,
                "arquivo": dest.relative_to(out).as_posix(),
                "largura": img.width, "altura": img.height,
            })
        except Exception as exc:
            print(f"[erro] Texture2D em {src.name}: {exc}", file=sys.stderr)


def main() -> None:
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    root, out = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
    out.mkdir(parents=True, exist_ok=True)
    inventory: list = []
    seen: set = set()
    for src in sorted(p for p in root.rglob("*") if is_candidate(p)):
        if "fonts_" in src.name:  # bundles só de fontes
            continue
        print(f"[lendo] {src.relative_to(root)}")
        extract_file(src, root, out, inventory, seen)
    (out / "inventario.json").write_text(json.dumps(inventory, ensure_ascii=False, indent=1), encoding="utf-8")
    counts = {}
    for item in inventory:
        counts[item["tipo"]] = counts.get(item["tipo"], 0) + 1
    print("total:", counts)


if __name__ == "__main__":
    main()
