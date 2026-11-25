# Mapa UTN · Planta baja · v02

Proyecto independiente para Unity 6.3 LTS **6000.3.28f1**, URP 17.3.0.
El proyecto anterior permanece en Descargas/mapa utn. Esta copia se abre desde Descargas/mapa utn v02.

## Descargar desde GitHub

Repositorio: https://github.com/santigz12/juegardo-mapa

Clonar el repositorio o usar **Code → Download ZIP** y extraerlo. En Unity Hub, agregar la carpeta que contiene **Assets**, **Packages** y **ProjectSettings**, con Unity **6000.3.28f1**. Abrir **Assets/Scenes/PlantaBaja.unity** y pulsar **Play**.

Se incluyen los modelos, materiales, archivos .meta, licencias, escena generada y datos del mapa. No hace falta descargar assets adicionales. La primera importación necesita Internet para que Unity resuelva sus paquetes.

Library, Logs, UserSettings y Builds no están en Git: Unity recrea los archivos locales y los ejecutables se generan desde **UTN → Compilar**. La versión anterior se conserva en el historial del repositorio.

## Abrir

1. Unity Hub → Add project from disk → seleccionar esta carpeta.
2. Esperar la importación y abrir Assets/Scenes/PlantaBaja.unity.
3. Pulsar Play. La escena está generada y no requiere volver a generarla.

La jerarquía y Assets/Generated/Prefabs/Sectores contienen Ala vieja, Ala nueva, Acceso y circulaciones, Administracion, Cafeteria, Aula Magna, Laboratorios restantes y Exteriores. Cada sector separa Arquitectura, Mobiliario y Techos. Los modelos básicos compartidos permanecen en Prefabs y las licencias en ThirdParty.

## Medidas

Paredes de 4 m de altura, piezas de hasta 4 m de longitud, espesor de 18 cm; puertas de 2 m de altura libre. Se conservan los polígonos y anchos diferentes de circulación. Ver Documentacion/REVISION-V02.md y Documentacion/APROXIMACIONES.md.

## Controles

Mapa: rueda para zoom, botón derecho para girar, botón central o WASD/flechas para desplazar. Clic selecciona una sala. Inicio centra el mapa.
Recorrer campus: WASD/flechas para caminar, mouse para mirar, Escape libera el cursor, M vuelve al mapa. En Web se mira manteniendo el botón derecho.

## Regenerar y compilar

UTN → Revision v02 → Generar y comprobar reconstruye la escena, los sectores y sus prefabs. Reemplaza la escena y Assets/Generated: guardar una copia antes de realizar cambios manuales. Los cambios de diseño permanentes deben incorporarse al generador o a los datos fuente.
UTN → Revision v02 → Comprobar dimensiones y rutas actualiza los informes.
UTN → Capturar vistas guarda imágenes en Capturas; Capturas/Anterior conserva las de v01.
UTN → Compilar permite construir Mac, Windows, Linux y Web con el módulo correspondiente instalado en Unity Hub. Se conservan los cuatro perfiles; esta revisión no incluye ejecutables anteriores.

Referencias y datos del mapa están incluidos: la ejecución no necesita conexión al sitio Mapear. Las pruebas históricas están separadas en Documentacion/Anterior.
