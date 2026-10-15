# rea_for_java_test

Ambiente para testar o [REA](https://github.com/morluto/rea) (Reverse Engineer Anything)
em artefatos Java (JARs, `.class`, APKs).

## Estrutura

```
.
├── .claude/
│   ├── hooks/session-start.sh           # prepara JADX/Apktool e registra o MCP do REA (nuvem)
│   ├── settings.json                    # ativa o hook SessionStart
│   └── skills/
│       └── reverse-engineer-anything/   # skill do REA (instruções + referências)
│           ├── SKILL.md
│           ├── LICENSE                  # MIT, © morluto
│           └── references/
├── targets/    # artefatos a analisar (ignorados pelo git)
├── reports/    # relatórios/evidências produzidos nas investigações
├── projeto/    # jogo derivado "Zombie Streamer" (análise, prompts, economia, monetização) — ver projeto/README.md
└── README.md
```

## Skill

O skill `reverse-engineer-anything` foi copiado de
`morluto/rea@670ad6bd96d888f488176d1d750479db5ee43d19` (`.agents/skills/reverse-engineer-anything`)
para `.claude/skills/`, onde o Claude Code o carrega automaticamente neste projeto.
Equivale a `npx skills add morluto/rea --skill reverse-engineer-anything`.

Para atualizar:

```bash
npx skills add morluto/rea --skill reverse-engineer-anything
```

## Servidor MCP (opcional, por máquina)

O skill fornece apenas instruções; as ferramentas de análise vêm do servidor MCP
`rea-agents`. O próprio REA recomenda **não** registrá-lo com escopo de projeto
(`.mcp.json` no repositório), então registre localmente:

```bash
claude mcp add --scope local rea -- npx -y rea-agents@6.5.0 mcp
# ou o setup guiado:
npx rea-agents setup
```

Reinicie o Claude Code depois.

Em sessões do Claude Code na nuvem isso é automático. No início de cada sessão o
hook `.claude/hooks/session-start.sh`:

1. baixa para `~/.local/share/rea/android/`, conferindo o SHA-256 fixado:
   - `jadx-headless-mcp-0.7.1-all.jar` (decompilação de APK; hash publicado pelo REA);
   - `apktool_2.12.1.jar` + um launcher `apktool` (recursos e manifesto);
2. exporta `REA_JADX_MCP_JAR` e `REA_APKTOOL_COMMAND` para a sessão;
3. registra o servidor MCP `rea` (escopo local, sem `.mcp.json`) com essas variáveis.

O JDK 17+ completo (com `javac`) e o `ps` já vêm na imagem da nuvem. Fora da
nuvem o hook não faz nada. Para Android/JADX são necessários Java 17+ e o
JAR do JADX MCP (`REA_JADX_MCP_JAR`); veja
`.claude/skills/reverse-engineer-anything/references/android-applications.md`.

## Uso

1. Copie o artefato para `targets/` (ex.: `targets/app.apk`).
2. Peça ao Claude: _"Use o REA para analisar targets/app.apk e explique o que ele faz"_.
   Para APK o REA usa `inspect_android_package`, `search_android_classes`,
   `inspect_android_class`, `inspect_android_method` e `trace_android_references`.
3. Salve conclusões em `reports/`.
