# 02 — Conceito: *Zombie Streamer: Idle Survival*

**Pitch.** O mundo acabou, mas a internet ainda funciona. Mia, uma streamer de 20 e poucos anos, transmite ao vivo a
própria sobrevivência. Cada toque na tela é conteúdo; cada móvel melhorado vira barricada. Os seguidores (vivos **e** zumbis
"fofos-assustadores" no chat) mandam doações. Ela sobe de rank, libera abrigos novos e desbloqueia trajes de sobrevivência.

O loop, os sistemas e a progressão espelham o original (ver [01](01_analise_jogo_original.md)). Muda o tema, a arte, os textos e os nomes.

## Regras de conteúdo (obrigatórias)

- **Todas as personagens são adultas (20+)**, desenhadas e descritas assim. Nada de uniforme escolar sexualizado: o
  uniforme é "da resistência", com saia longa ou calça.
- **Sem sexualização.** Os trajes reveladores do original (biquíni, bunny girl, "hot star") foram trocados por versões
  cobertas: macacão de mecânica, roupa de verão com bermuda, rockstar com jaqueta.
- **Zumbis sem gore.** Pele verde, pontos de costura, olhos grandes, sem sangue nem órgãos. Classificação-alvo: PEGI 7 / Livre–10.
- **Arte, textos, personagens e logo 100% originais.** O original serve só como referência de *função* e *tamanho*
  de cada asset (ver [GUIA_DE_ESTILO](../arte/GUIA_DE_ESTILO.md)).

## Mapa original → novo

### Salas (abrigos)

| # | Original | Novo | Item final que libera a próxima |
|---|---|---|---|
| 1 | Quarto de streamer | **Quarto-Bunker** | Quarto |
| 2 | Sala de aula | **Escola Abandonada** | Sala de Aula |
| 3 | Praia | **Píer Fortificado** | Praia |
| 4 | Cozinha | **Cozinha do Abrigo** | Cozinha |
| 5 | Loja de roupas | **Shopping Saqueado** | Loja |
| 6 | Quarto de luxo | **Cobertura Final** | Quarto Final |

Os itens de cada sala ficam em `design/economia/salas.json` (11 por sala, 75 níveis). Cada item tem 4 *tiers* visuais
(sucata → consertado → reforçado → high-tech neon), como os 4 níveis de arte dos móveis do original.

### Skills

| Original | Novo |
|---|---|
| Talk | Dicas de Sobrevivência |
| Joke | Humor Sombrio |
| Dance | Dança Anti-Zumbi |
| Sing | Canção de Ninar p/ Zumbis |
| Hype | Grito de Guerra |

### Elenco

| Original | Novo | Papel |
|---|---|---|
| Protagonista (main/avt_main/closet) | **Mia** | Streamer sobrevivente |
| Charlotte | **Charlotte** | Rival da elite que vive num bunker de luxo |
| Sam | **Sam** | Mecânica e saqueadora, melhor amiga |
| Rose | **Rose** | Batedora com câmera |
| Hana | **Hana** | Coordenadora do abrigo (antes era a agente) |
| Lara | **Lara** | Médica de campo |
| Chef Marco | **Marco** | Cozinheiro do acampamento |
| Professor | **Prof. Silva** | Cientista que pesquisa a cura |
| Siriee (celular) | **Siri-Z** | Celular-mascote com tela trincada |
| Otaku fans | **Fãs zumbis fofos** | Plateia/torcida |
| Rivais PK (avt_pk1–10) | **Streamers rivais** | Oponentes da batalha de horda |

### Roupas

25 trajes (os 24 do original mais 1 extra) em `design/economia/roupas.json`. A correspondência ícone → traje está em
`ferramentas/mapa_tematico.py` (`SKINS`).

### Ranks

15 títulos, de "Sobrevivente Anônima" a "A Última Streamer". Os limiares de seguidores e os boosts estão em `design/economia/ranks.json`.

### Sistemas

| Original | Novo |
|---|---|
| Chat com comentários e emojis | Chat com comentários de sobreviventes e de zumbis ("braaains 💀") e os mesmos 37 emojis com variante zumbi |
| Doação (Claim Donation) | Doação de suprimentos (moedas) |
| PK (batalha de streamers) | **Batalha de Horda**: quem segura mais zumbis em 30 s tocando na tela |
| Presente AFK | "Enquanto você dormia, o abrigo produziu…" (só o ganho real, sem multiplicador por vídeo) |
| História por capítulo | Uma história por abrigo: a busca pela cura com o Prof. Silva |
| Quests | Mesmos 15 tipos de condição (`conditionType`) |

## Escopo do MVP sugerido

1. Sala 1 completa (11 itens × 4 tiers), 5 skills, 6 roupas, ranks 1–5, chat e doações.
2. Monetização ([MONETIZACAO.md](../design/monetizacao/MONETIZACAO.md)): sem intersticial e sem banner; vídeo opcional só dá
   efeito temporário (boosters, testar roupa/item/skill); permanente só por conquista ou compra (tickets, renda x2, roupa testada).
3. Depois: salas 2–6, PK, história, notificações e Remote Config.
