# Guia de estilo e fluxo de recriação dos sprites

## O que está pronto

- `prompts/catalogo_sprites.csv` (separador `;`, abre no Excel) e `catalogo_sprites.json`: **740 linhas**, uma por
  sprite ou textura extraída. **591 têm prompt**; 149 são de SDK ou engine (ApplePay, SRDebugger, fontes...) e estão marcadas como `ignorar`.
- `prompts/por_categoria/*.md`: os mesmos dados agrupados, prontos para copiar e colar.

| Categoria | Qtde | O que é |
|---|---|---|
| personagem | 46 | NPCs e protagonista (3 expressões cada) |
| pose_corpo_inteiro | 67 | Poses da tela de roupas (closet) |
| avatar | 35 | Avatares da Mia por traje e rivais PK |
| roupa_icone | 24 | Ícones de traje |
| movel_sala1 | 78 | Móveis da sala 1 em 4 tiers, com sombras |
| cenario | 17 | Fundos de sala |
| skill | 40 | 5 skills × 8 níveis de ícone |
| emoji_chat | 49 | Emojis de reação do chat |
| ui_monetizacao | 26 | Botões de booster, tickets, moedas (os de no-ads podem virar "testar roupa") |
| ui_botao / ui_moldura / ui_icone | 22 / 114 / 51 | Botões, painéis 9-slice, ícones |
| vfx | 22 | Brilhos, partículas, raios |

Cada linha traz: arquivo original (para ver **tamanho e função**), função original, nome do novo asset,
**prompt do zero**, **prompt de transformação**, **prompt negativo**, confiança da classificação e caminho de saída em `novos_sprites/`.

- `pacote_referencia/` (fora do git): os 591 sprites **originais** separados por categoria, cada um com seu
  `.prompt.txt`, e `index.html`, uma galeria com filtro e botões de copiar. Abra no navegador.
  Para recriar: `ferramentas/montar_pacote.py` (ver README do projeto).

## Direção de arte

- **Estilo:** ilustração anime de jogo mobile, cel-shading suave, lineart limpa, cores saturadas e brilho "gloss".
  O mesmo nível de acabamento do original, mas com **identidade própria**: tema apocalipse aconchegante.
- **Paleta:** verde-tóxico `#9BE564` (zumbi, sucesso), rosa-choque `#FF4FA3` (live, coração, destaque), roxo profundo
  `#3B1F5C` (UI base), creme `#FFF4E0` (painéis), laranja `#FF8A3D` (alerta, fogo).
- **Personagens:** contorno branco grosso tipo adesivo, corpo inteiro, fundo transparente, adultas, roupas não reveladoras.
- **Props:** vista isométrica 3/4, sombra suave, fundo transparente. Os 4 tiers precisam ser variações do mesmo objeto.
- **UI:** formas arredondadas "candy" com detalhes de fita adesiva e arranhões. Molduras desenhadas para **9-slice**.
- **Zumbis:** fofos-assustadores (pele verde, pontos, olhos grandes), **sem sangue**.

## Fluxo recomendado

1. **Folha de personagem primeiro.** Gere a Mia (frente, 3/4, costas) com o prompt de `personagem.md` e fixe a
   identidade com seed fixa, LoRA ou *character reference* da ferramenta. Faça o mesmo para cada NPC.
2. **Gere cada sprite.** Há dois prompts por sprite. Abra `pacote_referencia/index.html`: cada card mostra o original e
   os dois prompts, com botão de copiar. Na pasta da categoria, cada `.png` vem com o `.prompt.txt` correspondente.
   - **Prompt de transformação:** envie o `.png` original **junto** com o prompt para um modelo de edição de imagem
     (img2img / "edit image"). Ele mantém pose, enquadramento e tamanho e redesenha o resto. É o caminho mais rápido
     para protótipo e para manter o encaixe das animações.
   - **Prompt do zero:** só texto, sem o original. É o caminho seguro para os assets **finais publicados**.

   ⚠️ **Risco de direitos autorais:** imagem gerada a partir do sprite original pode ser considerada obra derivada da arte
   da CSC Mobi, mesmo redesenhada. Use a transformação para protótipo e layout. Para a versão publicada, regere pelo
   prompt do zero ou redesenhe à mão até não restar traço, composição ou detalhe reconhecível do original.
   Para manter só a *pose*, um esqueleto OpenPose desenhado por você também funciona com o prompt do zero.
3. **Mantenha o tamanho.** Gere em 2× e reduza para o `largura x altura` do catálogo, mantendo o pivô no mesmo lugar
   (personagens pelo pé, ícones pelo centro).
4. **Salve** em `novos_sprites/<origem>/<nome>.png` com o **mesmo nome** do original. Assim dá para trocar os assets em lote no Unity.
5. **Revise** com o checklist abaixo.

## Checklist por asset

- [ ] Fundo transparente (PNG RGBA), sem halo branco
- [ ] Personagem claramente adulta, roupa não reveladora, pose não sugestiva
- [ ] Sem texto embutido (o texto vai na UI, por causa da localização), exceto o logo e badges como "x4"
- [ ] Legível a 64 px (ícones) ou em 9-slice (painéis)
- [ ] Consistente com a folha de personagem e a paleta
- [ ] Nada copiado do original (traço, composição única, logo)

## Regerar o catálogo

Se mudar as tabelas temáticas em `ferramentas/mapa_tematico.py`:

```bash
projeto/.venv/Scripts/python -I projeto/ferramentas/gerar_catalogo.py projeto/analise/extraido/inventario.json projeto/arte/prompts
```
