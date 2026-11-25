# Cámara de Man relax

Abrir Assets/Scenes/PlantaBaja_TerceraPersona.unity y pulsar Play.
La escena conserva el personaje agregado y tiene una cámara que sigue su posición.

- Botón derecho + mouse: girar alrededor del personaje.
- Rueda: acercar y alejar.
- En Main Camera > Camara Tercera Persona se ajustan Objetivo, Distancia, Altura Mirada, Inclinacion y Suavizado.
- El objetivo asignado es Man relax. Este cambio no agrega controles de movimiento al personaje.
- El seguimiento se realiza en LateUpdate; el script acerca la cámara cuando detecta obstáculos entre ella y el objetivo.
- CampusExplorer queda deshabilitado y solamente una cámara / AudioListener permanece activa durante el recorrido.
- Se conserva la escena previa en Assets/Scenes/Respaldos. La escena original PlantaBaja.unity no se reemplazó.
- Los perfiles de compilación anteriores aún apuntan al mapa original. Para compilar esta vista, seleccionar PlantaBaja_TerceraPersona como escena de inicio.
