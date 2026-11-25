# Fuentes y licencias

Mapa y datos consultados el 12 de noviembre de 2026: https://mapear.tech/utnfrt

Los JSON originales se conservan en Referencias y en Assets/Data. El grafo contiene todos los pisos; el generador solo utiliza PB. No se incluye código del sitio en la aplicación. La escala horizontal es 109/872.8 metros por unidad. El renderizador del sitio suma grade/gradeZones a una base común de -0.5 unidades; aquí se elimina esa traslación común y se aplica la misma escala a las cotas. Los patios quedan -0.5 unidades respecto al acceso, el terreno exterior -2.5.

El exterior.svg original lleva atribución © OpenStreetMap contributors (https://www.openstreetmap.org/copyright). Se conserva como referencia; la escena usa geometría propia y los rótulos de las cuatro calles, sin rasterizar el mapa de fondo.

## Modelos Kenney, CC0

Descargas publicadas por el autor en OpenGameArt:
- https://opengameart.org/content/furniture-kit
- https://opengameart.org/content/nature-kit
- https://opengameart.org/content/car-kit
- https://opengameart.org/content/building-kit

Licencias originales: Assets/ThirdParty/*/License.txt. Los modelos utilizados se conservan como OBJ/MTL de texto; el generador crea mallas, materiales URP y prefabs. Se conservan los mapas de color de autos y escaleras. No se importan todos los paquetes.

## Proyecto anterior

My project (2) aportó la configuración Unity 6000.3.28f1 y URP 17.3.0. No se modificó el proyecto anterior. Las carpetas de modelos y prefabs de Toon Gas Station estaban vacías.
