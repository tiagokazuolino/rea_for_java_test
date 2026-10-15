#!/bin/bash
# Registra o servidor MCP do REA (escopo local, fora do repositório) em sessões na nuvem.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REA_VERSION="6.5.0"
cd "${CLAUDE_PROJECT_DIR:-$(pwd)}"

# Pré-baixa o pacote para o cache do npm (o estado do container fica em cache após o hook).
npx -y "rea-agents@${REA_VERSION}" --version >/dev/null

# Idempotente: só registra se ainda não existir.
if ! claude mcp get rea >/dev/null 2>&1; then
  claude mcp add --scope local rea -- npx -y "rea-agents@${REA_VERSION}" mcp >/dev/null
fi
