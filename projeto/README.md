# Projeto: *Zombie Streamer: Idle Survival*

Jogo idle clicker com a mesma estrutura de *Viral Streamer: Idle Clicker* (loop, salas, skills, roupas, ranks e monetização),
reambientado num apocalipse zumbi com uma protagonista **adulta**, arte **original** e zumbis sem gore.

## Estrutura

```
projeto/
├── docs/
│   ├── 01_analise_jogo_original.md   # engenharia reversa: engine, sistemas, tabelas, SDKs, monetização
│   └── 02_conceito_zombie_streamer.md# GDD: mapa original → novo (salas, skills, elenco, roupas, sistemas)
├── analise/                          # (fora do git: conteúdo de terceiros, só referência local)
│   ├── apk_unpacked/                 #   assets do APK + saída do apktool
│   ├── extraido/                     #   sprites/, texturas/, textos/ e inventario.json
│   └── relatorios/
├── arte/
│   ├── GUIA_DE_ESTILO.md             # direção de arte, paleta, fluxo de geração e checklist
│   ├── prompts/
│   │   ├── catalogo_sprites.csv/json # 740 sprites: função original, novo asset, prompt e negativo
│   │   └── por_categoria/*.md        # mesmos prompts agrupados por tipo
│   ├── referencia_estilo/            # folhas de personagem e moodboard ORIGINAIS do jogo novo
│   └── novos_sprites/                # saída final (mesmo nome do sprite original)
├── design/
│   ├── economia/                     # salas.json, skills.json, roupas.json, ranks.json
│   └── monetizacao/                  # MONETIZACAO.md, iap_catalogo.json, ad_placements.json
├── jogo/Assets/                      # código Unity (C#)
│   ├── Scripts/Economy/              # carteira, recompensas, RewardGranter
│   ├── Scripts/Monetization/         # ofertas rewarded, compras, pacing + Adapters/ (MAX, Unity IAP, fakes)
│   ├── Resources/Config/             # cópia dos JSON de monetização usada em runtime
│   └── Tests/EditMode/               # testes NUnit (Unity Test Runner)
└── ferramentas/
    ├── extrair_sprites.py            # UnityPy: APK descompactado → sprites/texturas/textos
    ├── mapa_tematico.py              # tabelas de conversão de tema + estilo dos prompts
    └── gerar_catalogo.py             # inventário → catálogo com prompt por sprite
```

## Reproduzir a análise

O `apktool` está em `C:\Users\tiago.lino\.local\share\rea\android\`.

```bash
python -m venv projeto/.venv && projeto/.venv/Scripts/python -m pip install UnityPy Pillow
```

```bash
python -I -c "import zipfile,sys;z=zipfile.ZipFile(sys.argv[1]);z.extractall(sys.argv[2],[n for n in z.namelist() if n.startswith('assets/')])" "targets/Viral Streamer_ Idle Clicker.apk" projeto/analise/apk_unpacked
```

```bash
projeto/.venv/Scripts/python -I projeto/ferramentas/extrair_sprites.py projeto/analise/apk_unpacked/assets projeto/analise/extraido
```

```bash
projeto/.venv/Scripts/python -I projeto/ferramentas/gerar_catalogo.py projeto/analise/extraido/inventario.json projeto/arte/prompts
```

## Usar no Unity

1. Crie um projeto Unity 6 (URP 2D) e copie `jogo/Assets/*` para o `Assets/` dele.
2. Instale `com.unity.nuget.newtonsoft-json` e `com.unity.test-framework`. Depois, Unity IAP e o plugin AppLovin MAX.
3. Mantenha `jogo/Assets/link.xml` (preserva os DTOs no IL2CPP). Adicione `MonetizationBootstrap` numa cena, chame
   `Build(wallet, sink, installTime, persist)` depois de carregar o save e atribua `Resources/Config/ad_placements.json` e `iap_catalogo.json`.
   Ao editar `design/monetizacao/*.json`, copie de novo para `Resources/Config/`.
4. No Editor, os fakes simulam anúncios e compras. Para o build real, defina `ZS_APPLOVIN_MAX` e `ZS_UNITY_IAP` em
   *Player Settings → Scripting Define Symbols* e complete os TODOs de `UnityIapService` para a versão instalada.
5. Rode os testes em *Window → General → Test Runner → EditMode*.

## Avisos importantes

- **Direitos autorais.** Sprites, textos, personagens, logo e nome do original pertencem ao estúdio (CSC Mobi). O que está
  em `analise/` é só referência local: não versione, não publique, não use no build e não faça img2img ou trace deles.
  Mecânicas e estrutura de economia não são protegidas; a expressão (arte, texto, nomes) é. Por isso o projeto
  recria tudo do zero a partir dos prompts.
- **Público.** Personagens adultas, sem sexualização, zumbis sem gore. Confirme a classificação IARC e as políticas de
  anúncios e Families do Google Play antes de publicar (ver `design/monetizacao/MONETIZACAO.md`).
- **Não verificado.** O código C# não foi compilado nem testado aqui (não há Unity nem .NET SDK nesta máquina).
  Ele passou por revisão estática e as correções foram aplicadas. Rode o Test Runner no Unity antes de usar.
  Os adaptadores MAX e IAP precisam ser conferidos contra as versões dos plugins instalados.
