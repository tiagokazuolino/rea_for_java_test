"""Gera o catálogo de sprites com um prompt de recriação por imagem.

Uso:
    projeto/.venv/Scripts/python -I projeto/ferramentas/gerar_catalogo.py \
        projeto/analise/extraido/inventario.json projeto/arte/prompts

Saídas:
    catalogo_sprites.csv / .json   uma linha por sprite/textura (inclui os ignorados)
    por_categoria/<categoria>.md   leitura humana, com prompt e negativo
"""
import csv
import json
import re
import sys
from collections import defaultdict
from dataclasses import asdict, dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import mapa_tematico as M  # noqa: E402

EXPRESSIONS = {"1": "neutral or worried expression", "2": "happy smiling expression",
               "3": "excited surprised expression"}
POSES = ["idle standing", "waving hello", "hands on hips confident", "heart hands",
         "jumping with joy", "peace sign", "dancing step", "thinking pose",
         "pointing at camera", "shy pose", "victory fist pump", "spinning twirl"]
CLOSET_SKIN = {"closet_default": "1", "closet_01": "13", "closet_02": "6",
               "closet_03": "4", "closet_04": "9", "closet_05": "2"}
BG_ROOMS = {"bedroom1": 5, "class1": 1, "beach1": 2, "kitchen1": 3, "boutique1": 4,
            "caffe": None, "Bg1": 0, "BG": None, "Background": None}


@dataclass(frozen=True)
class Entry:
    id: str
    arquivo_original: str
    origem: str
    largura: int
    altura: int
    categoria: str
    funcao_original: str
    novo_asset: str
    prompt: str
    negativo: str
    confianca: str
    observacao: str


def normalize(name: str) -> str:
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", name).strip("_")


def strip_dup(name: str) -> str:
    return re.sub(r"__\d+$", "", name)


def size_hint(w: int, h: int) -> str:
    return f"canvas {w}x{h}px (keep this aspect ratio, generate at 2x and downscale)"


def character_prompt(name: str) -> tuple | None:
    for prefix in sorted(M.CHARACTERS, key=len, reverse=True):
        digits = r"\d+" if prefix == "rose" else r"\d*"
        m = re.fullmatch(rf"{prefix}({digits})", name, flags=re.IGNORECASE)
        if m:
            orig, new, desc = M.CHARACTERS[prefix]
            variant = EXPRESSIONS.get(m.group(1), "neutral expression")
            return ("personagem", orig, f"{new} ({variant})",
                    f"{desc}, {variant}, {M.STYLE_CHARACTER}", "alta")
    return None


def skin_prompt(name: str) -> tuple | None:
    m = re.fullmatch(r"(skin|avt_main)(\d+(?:New)?)", name)
    if not m:
        return None
    kind, n = m.groups()
    orig, new, desc = M.SKINS.get(n, ("?", f"Traje {n}", "survivor outfit"))
    if kind == "skin":
        return ("roupa_icone", f"Ícone de roupa: {orig}", f"Ícone do traje '{new}'",
                f"outfit icon of '{new}': {desc}, displayed on invisible mannequin, no body, "
                f"{M.STYLE_ICON}", "media")
    return ("avatar", f"Avatar da protagonista com {orig}", f"Avatar Mia - '{new}'",
            f"bust portrait avatar of Mia (adult survivor streamer, blonde hair with blue tips, "
            f"gaming headset) wearing {desc}, friendly expression, square crop, "
            f"{M.STYLE_CHARACTER}", "media")


def room1_prompt(name: str) -> tuple | None:
    base = strip_dup(name)
    for prefix in sorted(M.ROOM1_ITEMS, key=len, reverse=True):
        m = re.fullmatch(rf"{prefix}_?(\d*)(?:_bong)?", base)
        if m:
            orig, new, desc = M.ROOM1_ITEMS[prefix]
            tier = M.TIERS.get(m.group(1) or "1", "tier 1")
            if prefix == "room":
                return ("cenario", f"Fundo do quarto ({orig})", f"{new} - {tier.split(':')[0]}",
                        f"{M.ROOMS[0][2]}, {tier}, {M.STYLE_BG}", "alta")
            shadow = " (shadow layer only, soft dark ellipse)" if base.endswith("_bong") else ""
            return ("movel_sala1", f"Móvel '{orig}'", f"{new} - {tier.split(':')[0]}{shadow}",
                    f"{desc}{shadow}, {tier}, {M.STYLE_PROP}", "alta")
    return None


def skill_prompt(name: str) -> tuple | None:
    m = re.fullmatch(r"(talk|joke|dance|sing|hype)(\d+)", name)
    if not m:
        return None
    key, lvl = m.groups()
    orig, new, desc = M.SKILLS[key]
    return ("skill", f"Ícone da skill '{orig}' nível {lvl}", f"Skill '{new}' nível {lvl}",
            f"skill icon '{new}': {desc}, upgrade level {lvl} of 8 (more ornate frame and glow "
            f"as level increases), {M.STYLE_ICON}", "media")


def emoji_prompt(name: str) -> tuple | None:
    if name.isdigit() and int(name) in M.EMOJI_HINTS:
        hint = M.EMOJI_HINTS[int(name)]
    elif name in M.EMOJI_NAMED:
        hint = M.EMOJI_NAMED[name]
    else:
        return None
    return ("emoji_chat", f"Emoji do chat: {hint}", f"Emoji zumbi: {hint}",
            f"chat reaction emoji: {hint}, zombie-themed variant (green tint, stitches or tiny brain "
            f"accent), {M.STYLE_ICON}", "alta")


def closet_prompt(name: str, origin: str) -> tuple | None:
    if origin not in CLOSET_SKIN:
        return None
    n = CLOSET_SKIN[origin]
    orig, new, desc = M.SKINS[n]
    idx = sum(int(x) for x in re.findall(r"\d+", name)) % len(POSES)
    pose = POSES[idx]
    return ("pose_corpo_inteiro", f"Pose de corpo inteiro com {orig} ({name})",
            f"Mia - '{new}' - pose '{pose}'",
            f"full body Mia (adult survivor streamer, blonde hair with blue tips, gaming headset) "
            f"wearing {desc}, {pose}, {M.STYLE_CHARACTER}", "media")


def background_prompt(name: str) -> tuple | None:
    if name not in BG_ROOMS:
        return None
    idx = BG_ROOMS[name]
    if idx is None:
        return ("cenario", f"Fundo genérico ({name})", "Fundo genérico",
                f"soft purple gradient backdrop with faint zombie silhouettes and city ruins, "
                f"{M.STYLE_BG}", "media")
    orig, new, desc = M.ROOMS[idx]
    return ("cenario", f"Miniatura/fundo da sala {orig}", f"Sala '{new}'",
            f"{desc}, {M.STYLE_BG}", "alta")


def ui_prompt(name: str) -> tuple:
    base = strip_dup(name)
    if base in M.UI_EXPLICIT:
        orig, desc = M.UI_EXPLICIT[base]
        style = M.STYLE_ICON if base.lower().startswith(("icon", "ic_", "coin", "x", "ticket", "box")) else M.STYLE_UI
        return ("ui_monetizacao" if re.search(r"(?i)ads|money|x\d|income|pass|tap|ticket|reduce|donat|gift|coin", base)
                else "ui_icone", orig, desc, f"{desc}, {style}", "alta")
    low = base.lower()
    if low.startswith(("button", "bt_", "button_")):
        color = next((c for c in ("green", "blue", "red", "gray", "story", "cong", "skip") if c in low), "purple")
        return ("ui_botao", f"Botão {base}", f"Botão {color}",
                f"empty rounded button background, {color} variant, 9-slice friendly, {M.STYLE_UI}", "media")
    if low.startswith(("frame", "bar", "fill", "progress", "line", "group", "rectangle", "ellipse",
                       "title", "tag", "footer", "outline", "bar_", "circle", "square")):
        return ("ui_moldura", f"Moldura/painel {base}", f"Painel {base}",
                f"UI panel/frame '{base}' (keep same shape and proportions, empty, no text), "
                f"9-slice friendly, {M.STYLE_UI}", "baixa")
    if low.startswith(("icon", "ic_", "iconx", "icon_")):
        subject = re.sub(r"(?i)^(icon_?|ic_)", "", base).replace("_", " ")
        return ("ui_icone", f"Ícone {base}", f"Ícone '{subject}'",
                f"icon representing '{subject}', {M.STYLE_ICON}", "media")
    if low.startswith("avt_pk"):
        return ("avatar", f"Avatar de rival PK {base}", f"Rival streamer {base[-1]}",
                f"bust portrait avatar of a rival survivor streamer (adult woman, unique hairstyle "
                f"and survival outfit #{re.sub(r'\D', '', base)}), {M.STYLE_CHARACTER}", "media")
    return ("diversos", f"Asset {base}", f"Asset {base}",
            f"game asset representing '{base.replace('_', ' ')}', zombie-streamer theme, {M.STYLE_ICON}",
            "baixa")


def texture_prompt(name: str) -> tuple:
    return ("vfx", f"Textura/efeito {name}", f"Efeito {name}",
            f"VFX texture '{name.replace('_', ' ')}' (glow/particle/light), additive blending friendly, "
            f"black or transparent background, toxic green and pink tint", "media")


def classify(item: dict) -> tuple:
    name, origin = normalize(item["nome"]), item["origem"]
    if not name or name.startswith(M.IGNORE_PREFIXES) or name.endswith(M.IGNORE_SUFFIXES):
        return ("ignorar", "SDK/engine/terceiros", "-", "", "alta")
    if item["tipo"] == "textura" or origin == "shared":
        return texture_prompt(name)
    for fn in (lambda n: closet_prompt(n, origin), room1_prompt if origin == "room_01" else None,
               skin_prompt, character_prompt, skill_prompt, emoji_prompt, background_prompt):
        if fn is None:
            continue
        result = fn(name)
        if result:
            return result
    return ui_prompt(name)


def build(inventory: list) -> list:
    entries = []
    for item in inventory:
        if item["tipo"] == "texto":
            continue
        cat, orig, new, subject, conf = classify(item)
        w, h = item["largura"], item["altura"]
        prompt = "" if cat == "ignorar" else f"{subject}. {M.STYLE}. {size_hint(w, h)}"
        note = {"ignorar": "Não recriar: asset de SDK/engine.",
                "baixa": "Conferir o original para entender a função antes de gerar."}.get(
            cat if cat == "ignorar" else conf, "")
        entries.append(Entry(
            id=f"{item['origem']}/{normalize(item['nome']) or 'sem_nome'}", arquivo_original=item["arquivo"],
            origem=item["origem"], largura=w, altura=h, categoria=cat, funcao_original=orig,
            novo_asset=new, prompt=prompt, negativo="" if cat == "ignorar" else M.NEGATIVE,
            confianca=conf, observacao=note))
    return sorted(entries, key=lambda e: (e.categoria, e.id))


def write_outputs(entries: list, out: Path) -> None:
    out.mkdir(parents=True, exist_ok=True)
    rows = [asdict(e) for e in entries]
    (out / "catalogo_sprites.json").write_text(json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8")
    with open(out / "catalogo_sprites.csv", "w", newline="", encoding="utf-8-sig") as fh:
        writer = csv.DictWriter(fh, fieldnames=list(rows[0]), delimiter=";")
        writer.writeheader()
        writer.writerows(rows)
    groups = defaultdict(list)
    for e in entries:
        groups[e.categoria].append(e)
    cat_dir = out / "por_categoria"
    cat_dir.mkdir(exist_ok=True)
    for stale in cat_dir.glob("*.md"):
        stale.unlink()
    for cat, items in groups.items():
        lines = [f"# {cat} ({len(items)} sprites)", ""]
        if cat != "ignorar":
            lines += ["Negativo comum a todos:", "", f"```\n{M.NEGATIVE}\n```", ""]
        for e in items:
            lines += [f"## `{e.id}`", "",
                      f"- Original: `analise/extraido/{e.arquivo_original}` ({e.largura}x{e.altura})",
                      f"- Função original: {e.funcao_original}",
                      f"- Novo asset: **{e.novo_asset}** (confiança {e.confianca})",
                      f"- Salvar em: `arte/novos_sprites/{e.origem}/{e.id.split('/')[-1]}.png`"]
            if e.observacao:
                lines.append(f"- Obs.: {e.observacao}")
            if e.prompt:
                lines += ["", "```", e.prompt, "```"]
            lines.append("")
        (cat_dir / f"{cat}.md").write_text("\n".join(lines), encoding="utf-8")
    counts = {k: len(v) for k, v in sorted(groups.items())}
    print("categorias:", counts, "| total:", len(entries))


def main() -> None:
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    inventory = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    write_outputs(build(inventory), Path(sys.argv[2]))


if __name__ == "__main__":
    main()
