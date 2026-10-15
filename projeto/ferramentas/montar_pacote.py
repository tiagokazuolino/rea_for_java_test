"""Monta o pacote de trabalho: cada sprite original ao lado dos seus prompts, mais uma galeria HTML.

Uso:
    projeto/.venv/Scripts/python -I projeto/ferramentas/montar_pacote.py \
        projeto/arte/prompts/catalogo_sprites.json projeto/analise/extraido projeto/arte/pacote_referencia

Saída (<destino>, fora do git porque contém arte de terceiros):
    <categoria>/<origem>__<nome>.png          cópia do sprite original
    <categoria>/<origem>__<nome>.prompt.txt   prompt do zero + prompt de transformação + negativo
    index.html                                galeria com filtro por categoria e botões de copiar
"""
import html
import json
import shutil
import sys
from pathlib import Path

PAGE = """<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Sprites e prompts</title>
<style>
:root{{--bg:#16111f;--card:#231a33;--ink:#f3eefc;--muted:#b4a8c9;--accent:#9be564;--pink:#ff4fa3;--line:#3b2c55}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:14px/1.45 system-ui,sans-serif}}
header{{position:sticky;top:0;z-index:2;background:rgba(22,17,31,.95);border-bottom:1px solid var(--line);padding:12px 16px;display:flex;flex-wrap:wrap;gap:8px;align-items:center}}
h1{{font-size:18px;margin:0 12px 0 0}}input,select{{background:var(--card);color:var(--ink);border:1px solid var(--line);border-radius:8px;padding:6px 10px}}
#count{{color:var(--muted)}}main{{display:grid;grid-template-columns:repeat(auto-fill,minmax(340px,1fr));gap:14px;padding:16px}}
.card{{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:12px;display:flex;flex-direction:column;gap:8px}}
.thumb{{height:180px;display:flex;align-items:center;justify-content:center;border-radius:8px;
background:repeating-conic-gradient(#2e2443 0 25%,#261d38 0 50%) 0 0/20px 20px}}
.thumb img{{max-width:100%;max-height:100%;image-rendering:auto}}
.id{{font-family:ui-monospace,monospace;font-size:12px;color:var(--accent);word-break:break-all}}
.meta{{color:var(--muted);font-size:12px}}details summary{{cursor:pointer;color:var(--pink)}}
pre{{white-space:pre-wrap;background:#140f1d;border-radius:8px;padding:8px;font-size:12px;margin:6px 0}}
button{{background:var(--pink);color:#fff;border:0;border-radius:8px;padding:5px 10px;cursor:pointer;font-weight:600}}
button:hover{{filter:brightness(1.1)}}button:focus-visible{{outline:2px solid var(--accent)}}
</style></head><body>
<header><h1>Sprites originais → prompts</h1>
<select id="cat"><option value="">Todas as categorias</option>{options}</select>
<input id="q" type="search" placeholder="Filtrar por nome…"><span id="count"></span></header>
<main id="grid">{cards}</main>
<script>
const cards=[...document.querySelectorAll('.card')],cat=document.getElementById('cat'),q=document.getElementById('q'),count=document.getElementById('count');
function apply(){{let n=0;const c=cat.value,t=q.value.toLowerCase();for(const el of cards){{const ok=(!c||el.dataset.cat===c)&&(!t||el.dataset.id.includes(t));el.hidden=!ok;if(ok)n++;}}count.textContent=n+' sprites';}}
cat.onchange=apply;q.oninput=apply;apply();
document.addEventListener('click',e=>{{const b=e.target.closest('button[data-copy]');if(!b)return;
const txt=b.parentElement.querySelector('pre').textContent;navigator.clipboard.writeText(txt).then(()=>{{b.textContent='Copiado!';setTimeout(()=>b.textContent='Copiar',1200);}});}});
</script></body></html>"""

CARD = """<article class="card" data-cat="{cat}" data-id="{id_lower}">
<div class="thumb"><img loading="lazy" src="{img}" alt="{id}"></div>
<div class="id">{id}</div>
<div class="meta">{w}x{h}px · {cat} · {orig} → <b>{new}</b></div>
<details open><summary>Prompt de transformação</summary><pre>{transform}</pre><button data-copy>Copiar</button></details>
<details><summary>Prompt do zero</summary><pre>{prompt}</pre><button data-copy>Copiar</button></details>
<details><summary>Negativo</summary><pre>{neg}</pre><button data-copy>Copiar</button></details>
</article>"""


def main() -> None:
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    catalog = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    src_root, out = Path(sys.argv[2]), Path(sys.argv[3])
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)
    cards, cats, missing = [], set(), 0
    for e in catalog:
        if e["categoria"] == "ignorar":
            continue
        src = src_root / e["arquivo_original"]
        if not src.exists():
            missing += 1
            continue
        stem = f"{e['origem']}__{Path(e['arquivo_original']).stem}"
        dest_dir = out / e["categoria"]
        dest_dir.mkdir(exist_ok=True)
        shutil.copy2(src, dest_dir / f"{stem}.png")
        (dest_dir / f"{stem}.prompt.txt").write_text(
            f"ID: {e['id']}\nTamanho: {e['largura']}x{e['altura']}\nFunção original: {e['funcao_original']}\n"
            f"Novo asset: {e['novo_asset']}\nSalvar em: arte/novos_sprites/{e['origem']}/{Path(e['arquivo_original']).name}\n\n"
            f"== PROMPT DE TRANSFORMAÇÃO (enviar junto com o .png) ==\n{e['prompt_transformacao']}\n\n"
            f"== PROMPT DO ZERO ==\n{e['prompt']}\n\n== NEGATIVO ==\n{e['negativo']}\n",
            encoding="utf-8")
        cats.add(e["categoria"])
        esc = html.escape
        cards.append(CARD.format(
            cat=esc(e["categoria"]), id=esc(e["id"]), id_lower=esc(e["id"].lower()),
            img=esc(f"{e['categoria']}/{stem}.png"), w=e["largura"], h=e["altura"],
            orig=esc(e["funcao_original"]), new=esc(e["novo_asset"]),
            transform=esc(e["prompt_transformacao"]), prompt=esc(e["prompt"]), neg=esc(e["negativo"])))
    options = "".join(f'<option value="{html.escape(c)}">{html.escape(c)}</option>' for c in sorted(cats))
    (out / "index.html").write_text(PAGE.format(options=options, cards="\n".join(cards)), encoding="utf-8")
    print(f"{len(cards)} sprites em {len(cats)} categorias; {missing} originais não encontrados")


if __name__ == "__main__":
    main()
