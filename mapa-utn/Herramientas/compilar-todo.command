#!/bin/zsh
set -e
UTN_ROOT="${0:A:h:h}"
UTN_EDITOR='/Applications/Unity/Hub/Editor/6000.3.28f1/Unity.app/Contents/MacOS/Unity'
"$UTN_EDITOR" -batchmode -projectPath "$UTN_ROOT" -executeMethod CampusDelivery.Mac -quit -logFile "$UTN_ROOT/Documentacion/build-mac.log"
"$UTN_EDITOR" -batchmode -buildTarget Win64 -projectPath "$UTN_ROOT" -executeMethod CampusBuilder.BuildWindows -quit -logFile "$UTN_ROOT/Documentacion/build-windows.log"
"$UTN_EDITOR" -batchmode -buildTarget Linux64 -projectPath "$UTN_ROOT" -executeMethod CampusBuilder.BuildLinux -quit -logFile "$UTN_ROOT/Documentacion/build-linux.log"
"$UTN_EDITOR" -batchmode -buildTarget WebGL -projectPath "$UTN_ROOT" -executeMethod CampusBuilder.BuildWeb -quit -logFile "$UTN_ROOT/Documentacion/build-web.log"
