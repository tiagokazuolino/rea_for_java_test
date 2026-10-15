# rea_for_java_test

Ambiente para testar o [REA](https://github.com/morluto/rea) (Reverse Engineer Anything)
em artefatos Java (JARs, `.class`, APKs).

## Estrutura

```
.
├── .claude/
│   └── skills/
│       └── reverse-engineer-anything/   # skill do REA (instruções + referências)
│           ├── SKILL.md
│           ├── LICENSE                  # MIT, © morluto
│           └── references/
├── targets/    # artefatos a analisar (ignorados pelo git)
├── reports/    # relatórios/evidências produzidos nas investigações
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

Reinicie o Claude Code depois. Para Android/JADX são necessários Java 17+ e o
JAR do JADX MCP (`REA_JADX_MCP_JAR`); veja
`.claude/skills/reverse-engineer-anything/references/android-applications.md`.

## Uso

1. Copie o artefato para `targets/` (ex.: `targets/app.jar`).
2. Peça ao Claude: _"Use o REA para analisar targets/app.jar e explique o que ele faz"_.
3. Salve conclusões em `reports/`.
