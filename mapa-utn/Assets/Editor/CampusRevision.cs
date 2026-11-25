using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UtnMap;
public static class CampusRevision {
 static string Sector(RoomInfo r){if(r.id.StartsWith("aula-")&&r.id!="aula-magna")return "Ala vieja";if(r.id.StartsWith("lab-"))return "Ala nueva";if(r.id=="cafeteria")return "Cafeteria";if(r.id=="aula-magna")return "Aula Magna";if(r.category=="lab")return "Laboratorios restantes";if(r.category=="outdoor"||r.id=="dasuten"||r.id=="vestuarios")return "Exteriores";return "Administracion";}
 static Transform Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
 public static void Organize(){
 var world=GameObject.Find("UTN · Planta baja").transform;var architecture=world.Find("01 Arquitectura");var props=world.Find("03 Mobiliario");var roofs=world.Find("02 Techos");var explorer=UnityEngine.Object.FindFirstObjectByType<CampusExplorer>();
 var sectors=new Dictionary<string,Transform[]>();foreach(var name in new[]{"Ala vieja","Ala nueva","Acceso y circulaciones","Administracion","Cafeteria","Aula Magna","Laboratorios restantes","Exteriores"}){var root=Group(name,world);sectors[name]=new[]{root,Group("Arquitectura",root),Group("Mobiliario",root),Group("Techos",root)};}
 foreach(var r in explorer.rooms){var sector=sectors[Sector(r)];r.transform.SetParent(sector[1],true);var furniture=props.Find(r.code+" · "+r.displayName);if(furniture)furniture.SetParent(sector[2],true);foreach(var name in new[]{"Techo · ","Cielorraso · "}){var roof=roofs.Find(name+r.code);if(roof)roof.SetParent(sector[3],true);}}
 foreach(Transform child in architecture.Cast<Transform>().ToArray())child.SetParent(sectors["Acceso y circulaciones"][1],true);
 world.Find("04 Entorno y vegetacion").SetParent(sectors["Exteriores"][2],true);
 explorer.sectorRoofs=sectors.Values.Select(v=>v[3].gameObject).ToArray();foreach(var r in explorer.sectorRoofs)r.SetActive(false);
 foreach(var pair in sectors){var folder="Assets/Generated/Prefabs/Sectores/"+pair.Key;Directory.CreateDirectory(folder);AssetDatabase.Refresh();PrefabUtility.SaveAsPrefabAsset(pair.Value[0].gameObject,folder+"/"+pair.Key+".prefab");}
 UnityEngine.Object.DestroyImmediate(architecture.gameObject);UnityEngine.Object.DestroyImmediate(props.gameObject);
 AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
 [MenuItem("UTN/Revision v02/Generar y comprobar")]
 public static void VerifyAndBuild(){Audit();CampusBuilder.Capture();CampusBuilder.BuildMac();}
 public static void Generate(){CampusBuilder.Generate();CampusDelivery.Prepare();CampusDelivery.WalkAudit();Audit();CampusBuilder.Capture();}
 [MenuItem("UTN/Revision v02/Comprobar dimensiones y rutas")]
 public static void Audit(){
 EditorSceneManager.OpenScene("Assets/Scenes/PlantaBaja.unity");Physics.SyncTransforms();var explorer=UnityEngine.Object.FindFirstObjectByType<CampusExplorer>();var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);var errors=new List<string>();
 foreach(var t in all.Where(t=>new[]{"Pared","Limite exterior","Dintel","Cornisa"}.Contains(t.name))){if(t.localScale.z>4.001f)errors.Add("Tramo mayor de 4 m: "+t.name);if(t.name=="Pared"&&Mathf.Abs(t.localScale.x-.18f)>.001f)errors.Add("Espesor incorrecto");}
 foreach(var r in explorer.rooms.Where(r=>r.category!="outdoor")){foreach(var t in r.GetComponentsInChildren<Transform>().Where(t=>t.name=="Dintel")){float bottom=t.position.y-t.localScale.y/2;if(Mathf.Abs(bottom-r.focusPoint.y-2)>.005f)errors.Add("Altura puerta: "+r.id);}}
 if(explorer.rooms.Count(r=>Sector(r)=="Ala vieja")!=8||explorer.rooms.Count(r=>Sector(r)=="Ala nueva")!=9)errors.Add("Conteo de alas incorrecto");
 var report=new List<string>{"# Revision v02","","Dimensiones: paredes 4 m (3,86 m + cornisa 0,14 m); espesor 0,18 m; tramos <=4 m; puertas 2 m libres.","62 salas conservadas. Ala vieja: 8 aulas. Ala nueva: 9 laboratorios.","Errores dimensionales: "+errors.Count};report.AddRange(errors);
 report.AddRange(new[]{"","## Pasillos: ancho libre medido en la escena","","Medidas aproximadas del mapa, entre caras de colision a 2,5 m sobre el piso (sobre dinteles para excluir huecos de puertas). No son un relevamiento real.","","| Tramo | Ancho libre |","|---|---:|"});
 foreach(var sample in new[]{new[]{"Acceso Rivadavia","264.2","910"},new[]{"Cafeteria sur","290","830"},new[]{"Cafeteria centro","290","780"},new[]{"Cafeteria norte","290","700"},new[]{"Ala vieja sur","429.3","590"},new[]{"Ala vieja centro","429.3","470"},new[]{"Ala vieja norte","429.3","280"},new[]{"Ala nueva sur","115.7","550"},new[]{"Ala nueva centro","115.7","370"},new[]{"Ala nueva norte","115.7","330"}}){var p=World(float.Parse(sample[1],System.Globalization.CultureInfo.InvariantCulture),float.Parse(sample[2],System.Globalization.CultureInfo.InvariantCulture));if(Physics.Raycast(p+Vector3.up*10,Vector3.down,out var floor,30))p.y=floor.point.y+2.5f;bool left=Physics.Raycast(p,Vector3.left,out var l,40),right=Physics.Raycast(p,Vector3.right,out var rr,40);report.Add("| "+sample[0]+" | "+(left&&right?(l.distance+rr.distance).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" m":"Sin dos limites cercanos")+" |");}
 File.WriteAllLines("Documentacion/REVISION-V02.md",report);RouteAudit(explorer);Debug.Log("UTN_V02_DIMENSIONS errors="+errors.Count);if(errors.Count>0)throw new Exception(string.Join("; ",errors));
 }
 static Vector3 World(float x,float y)=>new Vector3((x-264.2f)*109f/872.8f,0,(918-y)*109f/872.8f);
 static void RouteAudit(CampusExplorer explorer){var graph=JObject.Parse(File.ReadAllText("Assets/Data/graph.json"));var data=JObject.Parse(File.ReadAllText("Assets/Data/planta_baja.json"));var lines=new List<string>{"Rutas completas por el grafo con CharacterController (prueba automatizada, sin teletransporte entre nodos)."};var cc=explorer.walker;
 foreach(var id in new[]{"aula-118","lab-159","cafeteria"}){var room=data["rooms"].First(r=>(string)r["id"]==id);string dest=(string)room["entryNodeId"];var prev=new Dictionary<string,string>{{"node-entry",null}};var queue=new Queue<string>();queue.Enqueue("node-entry");while(queue.Count>0&&!prev.ContainsKey(dest)){var n=queue.Dequeue();foreach(var next in graph[n]["neighbors"].Select(t=>(string)t)){if(graph[next]==null||(string)graph[next]["floor"]!="PB"||prev.ContainsKey(next))continue;prev[next]=n;queue.Enqueue(next);}}
 if(!prev.ContainsKey(dest)){lines.Add(id+": SIN RUTA");continue;}var path=new List<string>();for(string n=dest;n!=null;n=prev[n])path.Add(n);path.Reverse();cc.enabled=false;cc.transform.position=explorer.entrance+Vector3.up*.08f;cc.enabled=true;Physics.SyncTransforms();bool ok=true;string blocked="";
 foreach(var n in path.Skip(1)){var target=World((float)graph[n]["x"],(float)graph[n]["y"]);int steps=0;while(steps++<5000){var delta=target-cc.transform.position;delta.y=0;if(delta.magnitude<.12f)break;var before=cc.transform.position;cc.Move(delta.normalized*Mathf.Min(.04f,delta.magnitude)+Vector3.down*.02f);Physics.SyncTransforms();if((cc.transform.position-before).sqrMagnitude<.000001f){ok=false;blocked=n;break;}}if(steps>=5000){ok=false;blocked=n;}if(!ok)break;}
 lines.Add(id+": "+(ok?"PASS":"BLOCKED en "+blocked)+"; nodos="+path.Count);}
 cc.enabled=false;File.WriteAllLines("Documentacion/rutas-v02.txt",lines);Debug.Log(string.Join("\n",lines));
 }
}
