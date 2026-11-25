# Proyecto UTN para continuar en Codex Cloud

El proyecto Unity está en **mapa-utn/**. Incluye Assets, Packages, ProjectSettings, personajes, animaciones, referencias, documentación y capturas. No incluye las cachés que Unity reconstruye.

## Continuar desde otro dispositivo

Entrar a Codex con la misma cuenta y seleccionar el entorno **codex-nube**, conectado a este repositorio y a la rama **main**. Pedir que trabaje dentro de **mapa-utn/**.

Para abrirlo en Unity: clonar o descargar el repositorio, agregar **mapa-utn/** en Unity Hub y usar **Unity 6.3 LTS 6000.3.28f1**. Abrir **Assets/Scenes/PlantaBaja_TerceraPersona.unity** para ver el personaje Man relax y su cámara. La escena de mapa original sigue en **Assets/Scenes/PlantaBaja.unity**.

## Contexto de trabajo

- Mapa de planta baja de UTN FRT basado en https://mapear.tech/utnfrt, 62 espacios, escala de referencia de 109 m.
- v02: paredes de 4 m de altura, tramos de hasta 4 m, espesor de 18 cm y puertas de 2 m libres. Ala vieja: ocho aulas; ala nueva: nueve laboratorios.
- Se preservan los diferentes anchos de circulación y las medidas aproximadas se documentan dentro del proyecto.
- El módulo Animation está habilitado. Se incorporaron los assets de personajes y animaciones agregados localmente.
- CamaraTerceraPersona sigue a Man relax. Botón derecho y mouse giran la vista; rueda ajusta distancia. La configuración de cámara no incorpora un controlador de movimiento al personaje.
- El usuario quiere aprender a crear sus propios scripts de movimiento e interacción; prefiere explicaciones claras y ejemplos antes de cambios no solicitados.
- Hay respaldos de escenas en Assets/Scenes/Respaldos.
- Los perfiles de compilación originales apuntan a PlantaBaja.unity, no a la escena de tercera persona.
- La v02 del mapa se probó en Mac. Windows, Linux y Web conservan perfiles; los cambios posteriores de personajes y cámara no se han recompilado para esas plataformas.

Esta copia contiene archivos guardados en disco. No equivale a una instalación del editor Unity en el entorno cloud ni contiene cambios de escenas que no se hayan guardado. Conservar las licencias de los assets.
