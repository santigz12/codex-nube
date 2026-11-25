using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UtnMap;
public static class CampusDelivery {
 [MenuItem("UTN/Preparar perfiles y auditar accesos")]
 public static void Prepare(){
  Directory.CreateDirectory("Assets/BuildProfiles");
  foreach(var target in new[]{BuildTarget.StandaloneOSX,BuildTarget.StandaloneWindows64,BuildTarget.StandaloneLinux64,BuildTarget.WebGL}){
   string path="Assets/BuildProfiles/"+target+".asset";if(AssetDatabase.LoadAssetAtPath<BuildProfile>(path))continue;
   var profile=ScriptableObject.CreateInstance<BuildProfile>();profile.name=target.ToString();var serialized=new SerializedObject(profile);serialized.FindProperty("m_BuildTarget").intValue=(int)target;serialized.FindProperty("m_Subtarget").intValue=target==BuildTarget.WebGL?0:(int)StandaloneBuildSubtarget.Player;serialized.ApplyModifiedPropertiesWithoutUndo();
   // Unity 6.3 has no public profile factory (introduced in 6.5). Use its own validation/initialization methods.
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(BuildProfile).GetMethod("ValidateDataConsistency",flags).Invoke(profile,null);typeof(BuildProfile).GetMethod("TryCreatePlatformSettings",flags).Invoke(profile,null);
   profile.overrideGlobalScenes=true;profile.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/PlantaBaja.unity",true)};AssetDatabase.CreateAsset(profile,path);
  }AssetDatabase.SaveAssets();Audit();
 }
 [MenuItem("UTN/Auditar puertas")]
 public static void Audit(){
  EditorSceneManager.OpenScene("Assets/Scenes/PlantaBaja.unity");Physics.SyncTransforms();var lines=new List<string>();int blocked=0;
  foreach(var room in UnityEngine.Object.FindObjectsByType<RoomInfo>(FindObjectsSortMode.None)){
   if(room.category=="outdoor")continue;var p=room.entryPoint;float ground=room.focusPoint.y;var hits=Physics.OverlapCapsule(new Vector3(p.x,ground+.28f,p.z),new Vector3(p.x,ground+1.5f,p.z),.23f).Where(c=>!(c is CharacterController)&&c.bounds.size.y>.1f).ToArray();if(hits.Length>0)blocked++;
   lines.Add(room.id+": "+(hits.Length==0?"PASS":string.Join(", ",hits.Select(c=>c.name))));
  }
  lines.Insert(0,"Accesos con interferencias: "+blocked);File.WriteAllLines("Documentacion/accesos.txt",lines);Debug.Log("UTN_DOOR_AUDIT blocked="+blocked);
 }

 public static void Polish(){EditorSceneManager.OpenScene("Assets/Scenes/PlantaBaja.unity");if(GameObject.Find("Superficies exteriores v2"))return;var env=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Assets/Data/planta_baja.json"))["envelope"];foreach(var t in env["surfaces"]){var obj=GameObject.Find(t["id"].ToString());if(!obj)continue;bool grass=t["kind"].ToString()=="grass";float shift=grass?(t["id"].ToString().StartsWith("platabanda")?0:-.045f):.007f;obj.transform.position+=Vector3.up*shift;}new GameObject("Superficies exteriores v2");EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());}

 public static void WalkAudit(){
 EditorSceneManager.OpenScene("Assets/Scenes/PlantaBaja.unity");var explorer=UnityEngine.Object.FindFirstObjectByType<CampusExplorer>();var cc=explorer.walker;var lines=new List<string>();int failures=0;
 foreach(var room in explorer.rooms.Where(r=>r.category!="outdoor")){
 var inward=room.focusPoint-room.entryPoint;inward.y=0;inward.Normalize();var origin=room.entryPoint-inward*.65f;origin.y=room.focusPoint.y+.08f;cc.enabled=false;cc.transform.position=origin;cc.enabled=true;Physics.SyncTransforms();
 for(int i=0;i<70;i++){cc.Move(inward*.025f+Vector3.down*.012f);Physics.SyncTransforms();}
 float crossed=Vector3.Dot(cc.transform.position-room.entryPoint,inward);bool ok=crossed>.15f;lines.Add(room.id+": "+(ok?"PASS":"BLOCKED")+" cruce="+crossed.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));if(!ok)failures++;
 }
 cc.enabled=false;lines.Insert(0,"Prueba de cruce de puertas con CharacterController; bloqueos: "+failures);File.WriteAllLines("Documentacion/recorrido-puertas.txt",lines);Debug.Log("UTN_WALK_AUDIT failures="+failures);
 }
 public static void Mac(){Polish();Prepare();CampusBuilder.Capture();CampusBuilder.BuildMac();}
 [MenuItem("UTN/Compilar/macOS")] public static void MacMenu()=>CampusBuilder.BuildMac();
 [MenuItem("UTN/Compilar/Windows")] public static void WindowsMenu()=>CampusBuilder.BuildWindows();
 [MenuItem("UTN/Compilar/Linux")] public static void LinuxMenu()=>CampusBuilder.BuildLinux();
 [MenuItem("UTN/Compilar/Web")] public static void WebMenu()=>CampusBuilder.BuildWeb();
}
