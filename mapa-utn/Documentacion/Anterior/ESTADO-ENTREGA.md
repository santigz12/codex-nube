# Estado de entrega — 12 de noviembre de 2026

## Entrega completa

Proyecto independiente Unity 6.3 LTS 6000.3.28f1, URP 17.3.0. Escena editable: Assets/Scenes/PlantaBaja.unity. El proyecto anterior no se modificó.

| Plataforma | Compilación | Ejecución comprobada |
|---|---|---|
| macOS universal (Apple Silicon + Intel) | Correcta | En este Mac: carga, mapa, recorrido y desplazamiento automático de 3 m sin excepciones |
| Windows x64 | Correcta | Pendiente de probar en un equipo Windows |
| Linux x64 | Correcta | Pendiente de probar en un equipo Linux |
| Web / WebGL 2 | Correcta | Navegador integrado Chromium: carga, buscador 118 y enfoque, entrada a primera persona, tecla W, botón derecho, retorno con M; consola sin errores tras la corrección |

## Validación de escena

- 62 espacios con identificadores únicos, geometría de piso y materiales válidos.
- 58 puertas interiores sin interferencias con paredes/mobiliario.
- 58 cruces de puertas con CharacterController aprobados.
- Escala 109/872.8 = 0.1248854 m por unidad del mapa.
- Modelos seleccionados Kenney CC0 y licencias originales incluidos.
- Fuentes JSON locales; no se consulta Mapear durante la ejecución.

## Correcciones comprobadas

La cámara del mapa usa un plano cercano de 1.5 m, evitando pérdida de precisión de profundidad en Web. El recorrido conserva 0.07 m. En Web se mira manteniendo el botón derecho, sin solicitar bloqueo del cursor: esto evita el error de Chromium encontrado en la primera prueba. En escritorio se conserva el cursor capturado y Escape lo libera. La página Web ocupa la ventana completa.

## Límites de esta validación

Las compilaciones de Windows y Linux no equivalen a pruebas en esos sistemas. No se probaron Safari ni Firefox ni dispositivos móviles. No se promete una tasa de cuadros para equipos distintos. Web requiere navegador de escritorio con WebGL 2, teclado y mouse; se sirve por HTTP(S), no abriendo index.html directamente.

El mobiliario, acabados y altura de muros son representativos: ver APROXIMACIONES.md. La prueba automática de puertas valida un cruce corto por cada acceso, no todas las trayectorias posibles del campus.

## Evidencia

- validacion.txt: inventario y comprobaciones de escena.
- accesos.txt: interferencias estáticas.
- recorrido-puertas.txt: cruce de cada puerta con CharacterController.
- runtime-mac.log: prueba del ejecutable Mac.
- builds.txt: historial de resultados; la última entrada de cada plataforma corresponde a la entrega final.
- build-*.log: registros completos de Unity.
- Capturas/: cuatro vistas de la escena y dos capturas del ejecutable Mac.
