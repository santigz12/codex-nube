#!/bin/zsh
UTN_ROOT="${0:A:h}"
UTN_NODE='/Applications/Unity/Hub/Editor/6000.3.28f1/PlaybackEngines/WebGLSupport/BuildTools/Emscripten/node/node'
if [[ ! -x "$UTN_NODE" ]]; then
 UTN_NODE="$(command -v node)"
fi
if [[ -z "$UTN_NODE" ]]; then
 echo 'Se necesita Node.js o el módulo Web de Unity 6000.3.28f1.'
 exit 1
fi
open 'http://127.0.0.1:8080'
"$UTN_NODE" "$UTN_ROOT/Herramientas/servir-web.cjs"
