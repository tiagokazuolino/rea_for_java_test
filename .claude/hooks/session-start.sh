#!/bin/bash
# Prepara sessões na nuvem para análise de APK com o REA:
# baixa JADX headless MCP + Apktool e registra o servidor MCP do REA
# (escopo local, fora do repositório).
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REA_VERSION="6.5.0"
JADX_VERSION="0.7.1"
JADX_SHA256="6e5eacf500b64292bfb73c49797c1958f6ee44646e43e868039ae7feb573ff75"
APKTOOL_VERSION="2.12.1"
APKTOOL_SHA256="66cf4524a4a45a7f56567d08b2c9b6ec237bcdd78cee69fd4a59c8a0243aeafa"

TOOLS_DIR="$HOME/.local/share/rea/android"
JADX_JAR="$TOOLS_DIR/jadx-headless-mcp-${JADX_VERSION}-all.jar"
APKTOOL_JAR="$TOOLS_DIR/apktool_${APKTOOL_VERSION}.jar"
APKTOOL_CMD="$TOOLS_DIR/apktool"

cd "${CLAUDE_PROJECT_DIR:-$(pwd)}"
mkdir -p "$TOOLS_DIR"

# Baixa um arquivo só se ausente ou com SHA-256 diferente do fixado.
fetch() {
  local url="$1" dest="$2" sha="$3"
  if [ -f "$dest" ] && echo "$sha  $dest" | sha256sum -c --quiet - 2>/dev/null; then
    return
  fi
  curl -fsSL -o "$dest.part" "$url"
  echo "$sha  $dest.part" | sha256sum -c --quiet -
  mv "$dest.part" "$dest"
}

fetch "https://github.com/1013503897/jadx-headless-mcp/releases/download/v${JADX_VERSION}/jadx-headless-mcp-${JADX_VERSION}-all.jar" \
  "$JADX_JAR" "$JADX_SHA256"
fetch "https://github.com/iBotPeaches/Apktool/releases/download/v${APKTOOL_VERSION}/apktool_${APKTOOL_VERSION}.jar" \
  "$APKTOOL_JAR" "$APKTOOL_SHA256"

cat > "$APKTOOL_CMD" <<WRAPPER
#!/bin/sh
exec java -jar "$APKTOOL_JAR" "\$@"
WRAPPER
chmod +x "$APKTOOL_CMD"

# Também disponível para o CLI `rea` dentro da sessão.
if [ -n "${CLAUDE_ENV_FILE:-}" ] && ! grep -qs REA_JADX_MCP_JAR "$CLAUDE_ENV_FILE"; then
  {
    echo "export REA_JADX_MCP_JAR=\"$JADX_JAR\""
    echo "export REA_APKTOOL_COMMAND=\"$APKTOOL_CMD\""
  } >> "$CLAUDE_ENV_FILE"
fi

# Pré-baixa o pacote para o cache do npm (o estado do container fica em cache após o hook).
npx -y "rea-agents@${REA_VERSION}" --version >/dev/null

# Idempotente: (re)registra só se ausente ou sem as variáveis do Android.
if ! claude mcp get rea 2>/dev/null | grep -q "REA_APKTOOL_COMMAND"; then
  claude mcp remove rea --scope local >/dev/null 2>&1 || true
  claude mcp add --scope local rea \
    -e REA_JADX_MCP_JAR="$JADX_JAR" \
    -e REA_APKTOOL_COMMAND="$APKTOOL_CMD" \
    -- npx -y "rea-agents@${REA_VERSION}" mcp >/dev/null
fi
