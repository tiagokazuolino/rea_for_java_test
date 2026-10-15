# 01 — Análise do jogo original

Alvo: `targets/Viral Streamer_ Idle Clicker.apk`
SHA-256 `974d827aba0b6bfdcf4cc423e01689a9c204d6784a0568b012e3de4a6d7a10ea`

| Campo | Valor |
|---|---|
| Pacote | `com.cscmobi.livestream.idle.clicker` |
| Versão | 1.1.8 (versionCode 75) |
| SDK | minSdk 24 / targetSdk 36 |
| Engine | Unity, scripting **IL2CPP** (metadata v31), URP 2D, Spine, DOTween, UniTask, Addressables, TextMeshPro |
| Namespace do jogo | `StreamerClicker.UI` (57 classes de UI) |
| Distribuição | APK base de um bundle: **sem `lib/`** (o `libil2cpp.so` está no split de ABI) e só a sala 1 em `ContentPacks/` (as salas 2–6 vêm por Play Asset Delivery) |

## Como foi extraído

1. `apktool d -s` decodificou o manifesto e os recursos (em `analise/apk_unpacked/apktool/`).
2. `ferramentas/extrair_sprites.py` (UnityPy) leu `data.unity3d`, os Addressables (`aa/Android/*.bundle`) e os `ContentPacks/*`.
   Resultado: **691 sprites, 49 texturas e 57 tabelas de texto** em `analise/extraido/` (fora do git).
3. As tabelas de dados (JSON/CSV dentro de TextAssets) descrevem a economia inteira, sem precisar decompilar código.
4. Nomes de classes vieram das strings do `global-metadata.dat`. **O código C# não foi decompilado**, porque o `libil2cpp.so` não está neste APK. Então o comportamento abaixo foi inferido de dados, textos de UI e nomes de classes, não lido no código.

## Loop principal (core loop)

```
tocar na tela ──► moedas (TapRate)          ┐
tempo passando ─► moedas (PassRate, idle)    ├─► comprar/upar itens da sala ──► + seguidores
doações no chat ─► moedas (Claim Donation)   ┘          │                         │
                                                         ▼                         ▼
                                     upar skills (Talk/Joke/Dance/Sing/Hype)   subir de rank (boost global)
                                                         │                         │
                                                         └──► comprar roupas (boost) ◄┘
                                     item final da sala comprado ──► nova sala (6 no total) + capítulos de história
```

Sistemas extras: **PK** (batalha contra outra streamer, `PkPopup`/`PkWinLosePopup`), **quests** (262 em `QuestNormal`),
**história** (26 capítulos com gatilhos por nível de item), **chat ao vivo** (comentários e nomes falsos em 13 idiomas),
**presente AFK** (`ClaimAfkPopup` com roleta de multiplicador `AfkMultiplierSegment`), **Free Gift**, **Rate** e **notificações**.

## Tabelas de economia (Addressables `defaultlocalgroup`)

| Tabela | Conteúdo |
|---|---|
| `Room1..6.txt` | 6 salas × 11 itens × **75 níveis**. Campos: `Price`, `Follower`, `TapRate`, `PassRate`, `UpgradeTime` (0–300 s), `IndexDependency` |
| `Skills.txt` | 5 skills × **451 níveis** (Talk, Joke, Dance, Sing, Hype) |
| `Clothes.txt` | 24 roupas × 3 níveis, `RankDependency`, `ClothEquipBoostRate` (11500 → 565100) |
| `RankFollower.txt` | 15 ranks: seguidores necessários, `RankBoostRate` (1 → 25000) e recompensa |
| `QuestNormal.txt` | 262 quests (`conditionType` 1–15: gastar/coletar moedas, upar item X ao nível Y, trocar de sala, doações...) |
| `StoryLogic.txt` + `StoryLocale_*` | 26 capítulos; gatilhos como `ITEM_HOUSE_2_LEVEL_ >= 50` |
| `ChatComments_*`, `ChatNames` | Comentários do chat com índices de emoji (sprites `0..36`) |

Curvas medidas (preço do nível n = base × g^n):

| Sala | Preço base | g médio | Tap/Pass por nível |
|---|---|---|---|
| 1 Quarto | 5 | 1,155 | 200–400 |
| 2 Sala de aula | 31.772 | 1,125 | 400 |
| 3 Praia | 2,2e7 | 1,123 | 300 |
| 4 Cozinha | 1,18e10 | 1,079 | 400 |
| 5 Loja | 1,54e12 | 1,083 | 400 |
| 6 Quarto final | 2,44e14 | 1,075 | 400 |
| Skills | 5 | 1,0831 | 300 |

Essas curvas viraram parâmetros em `design/economia/*.json`. A economia nova usa fórmulas, sem copiar tabela por tabela.

## Monetização observada

**Anúncios.** AppLovin **MAX** como mediador (adaptadores AdMob, Meta Audience Network, Pangle/ByteDance, Mintegral, Unity Ads,
ironSource, Vungle/Liftoff, InMobi, Fyber/DT, Moloco, BidMachine, Bigo, Yandex), consentimento via **Google UMP** e
**MaxCmpService**. A string `"Interstitial pacing"` indica controle de frequência dos intersticiais, e `UI.AdBreak.Title` = "Ad break"
indica um aviso antes do intersticial.

**Pontos de vídeo recompensado** (inferidos dos sprites e textos):

| Ponto | Evidência |
|---|---|
| Booster x4 toque | `Buttonx4Tap`, `x4Click` |
| Booster x4 passivo | `ButtonX4Pass`, `x4Pass` |
| Booster x10 renda | `Buttonx10Income`, `x10Income` |
| Autoclique | `BoosterAutoClickPopup`, texto "Auto click in {0}" |
| Moeda x2 | `Boosterx2HeartPopup`, "Tap for x2 coin amount in {0}" |
| Multiplicador AFK | `ClaimAfkPopup`, `AfkMultiplierSegment` |
| Doação x2 | `ClaimDonatePopup`, "Claim X2" / "Claim x{0}" |
| Presente grátis | `FreeGiftPopup` |
| Upgrade de skill grátis | "No Video! / Skill Upgrade" |

**Compras (Google Play Billing via Unity IAP v5).**
- **Remove Ads** não-consumível. Pelo texto, remove banner e tela cheia e mantém o vídeo recompensado opcional ("Keep reward ads for bonus").
- **Bundle Remove Ads + Tickets** (badges "MOST", "POPULAR", "SAVE").
- **Tickets para pular vídeo** ("Skip RV Ads Ticket"): pacotes 5 / 15 (+50%) / 35 (+40%) / 80 (+60%) / 140 (+85%) / 300 (+120%), vistos nos sprites `Group_54/55/56/272/275/276`.
- **Golden Pack**, **Big/Small Money** e **oferta de roupa com 15% OFF** (`ButtonReduce`, `FrameReduce`).
- IDs internos `pack_1` … `pack_6` no metadata.

**Analytics e infra.** Firebase (Analytics, Crashlytics, Remote Config, Messaging), AppsFlyer (validação de recibo), AppMetrica,
Unity Notifications, Google Play Review e In-App Update.

## Lacunas

- Os sprites das salas 2–6 não estão neste APK; só existem os fundos-miniatura (`class1`, `beach1`, `kitchen1`, `boutique1`, `bedroom1`).
  Para recriar os móveis dessas salas, use os nomes da tabela `RoomN.txt` (já em `design/economia/salas.json`).
- As animações Spine das personagens (o `curtain.atlas` está aqui; o resto está nos packs) não foram extraídas.
- Durações exatas dos boosters, cooldowns e preços de IAP ficam no Remote Config ou no código IL2CPP. Os valores em `design/` são propostas.
