# Entrega v02

- Original conservado: ../mapa utn. Proyecto independiente: ../mapa utn v02.
- Unity 6000.3.28f1, URP 17.3.0; cuatro perfiles de compilación conservados.
- 62 espacios y sus identificadores; ocho aulas en Ala vieja, nueve laboratorios en Ala nueva.
- Validación dimensional sin errores; cruce automatizado de puertas sin bloqueos y auditoría de accesos sin interferencias.
- Rutas completas con CharacterController desde Rivadavia hasta Aula 118, Laboratorio 159 y Cafetería: PASS. Esto no reemplaza comprobar todas las rutas posibles manualmente.
- Dinteles compartidos junto al patio corregidos para respetar el piso de las salas. Se unifican los tramos coincidentes de muros exteriores y salas para evitar duplicación por diferencias de cota.
- Techos separados por sector, ocultos en mapa y visibles durante recorrido.
- Capturas/Comparacion.html presenta v01 y v02 lado a lado, con las mismas cámaras.
- La medida Acceso Rivadavia de REVISION-V02.md es el ancho transversal del hall en el punto muestreado, no el ancho del portón exterior. Cafetería sur corresponde al ensanchamiento de conexión al hall.
- Los informes anteriores están en Documentacion/Anterior y las imágenes originales en Capturas/Anterior. No se copiaron caches ni ejecutables antiguos.
- Windows, Linux y Web mantienen configuración; no se recompilaron ni se probaron en esta revisión.
- macOS v02: compilación correcta y ejecución comprobada. Prueba automática: 62 salas, avance de 3 m desde la entrada, techos visibles en recorrido y ocultos al regresar al mapa; sin excepciones registradas. Capturas runtime-mapa.png y runtime-entrada.png.
