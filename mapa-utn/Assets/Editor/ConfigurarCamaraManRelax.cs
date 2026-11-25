using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ConfigurarCamaraManRelax
{
    const string Request = "Library/UTNThirdPerson.request";
    static ConfigurarCamaraManRelax(){EditorApplication.delayCall += Pendiente;}
    static void Pendiente(){if(!File.Exists(Request))return;if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Pendiente;return;}File.Delete(Request);try{Configurar();}catch(Exception e){File.WriteAllText("/tmp/utn-camera-result.txt",e.ToString());Debug.LogException(e);}}

    [MenuItem("UTN/Camara/Seguir Man relax en tercera persona")]
    public static void Configurar()
    {
        if(EditorApplication.isPlaying)throw new Exception("Detener Play antes de configurar la escena.");
        var targets=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.Equals("Man relax",StringComparison.OrdinalIgnoreCase)).ToArray();
        if(targets.Length!=1)throw new Exception("Se esperaba un solo Man relax; encontrados: "+targets.Length);
        var target=targets[0];var scene=target.gameObject.scene;
        Directory.CreateDirectory("Assets/Scenes/Respaldos");AssetDatabase.Refresh();
        string backup="Assets/Scenes/Respaldos/PlantaBaja_AntesCamara_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("No se pudo guardar respaldo.");
        var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var camera=cameras.FirstOrDefault(c=>c.gameObject.scene==scene&&c.name=="Main Camera");
        if(!camera){var go=new GameObject("Camara tercera persona",typeof(Camera),typeof(AudioListener));SceneManager.MoveGameObjectToScene(go,scene);camera=go.GetComponent<Camera>();}
        Undo.RecordObject(camera,"Configurar tercera persona");camera.enabled=true;camera.tag="MainCamera";camera.nearClipPlane=.03f;camera.fieldOfView=60;camera.farClipPlane=500;
        var follow=camera.GetComponent<CamaraTerceraPersona>();if(!follow)follow=Undo.AddComponent<CamaraTerceraPersona>(camera.gameObject);
        follow.objetivo=target;follow.alturaMirada=1.3f;follow.distancia=4f;follow.inclinacion=15f;follow.giroHorizontal=target.eulerAngles.y;
        foreach(var c in cameras){if(c==camera)continue;Undo.RecordObject(c,"Desactivar camara duplicada");c.enabled=false;}
        foreach(var l in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Undo.RecordObject(l,"Un solo AudioListener");l.enabled=l.gameObject==camera.gameObject;}
        foreach(var e in UnityEngine.Object.FindObjectsByType<UtnMap.CampusExplorer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Undo.RecordObject(e,"Desactivar explorador para tercera persona");e.enabled=false;}
        Physics.SyncTransforms();follow.EncuadrarAhora();
        var start=target.position;var first=camera.transform.position;target.position+=Vector3.right*.1f;Physics.SyncTransforms();follow.EncuadrarAhora();bool moved=Vector3.Distance(camera.transform.position,first)>.01f;target.position=start;Physics.SyncTransforms();follow.EncuadrarAhora();
        string output="Assets/Scenes/PlantaBaja_TerceraPersona.unity";
        EditorUtility.SetDirty(follow);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene,output))throw new Exception("No se pudo guardar escena.");
        SceneManager.SetActiveScene(scene);Selection.activeGameObject=camera.gameObject;
        Directory.CreateDirectory("Capturas");var rt=new RenderTexture(1280,800,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes("Capturas/man-relax-tercera-persona.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        string report="Escena: "+output+"\nRespaldo: "+backup+"\nObjetivo: "+target.name+"\nSeguimiento al mover objetivo: "+moved+"\nCamaras habilitadas: "+cameras.Count(c=>c.enabled)+"\nPosicion objetivo: "+target.position+"\nPosicion camara: "+camera.transform.position;
        File.WriteAllText("/tmp/utn-camera-result.txt",report);Debug.Log("UTN_THIRD_PERSON_CONFIGURED\n"+report);
    }
}
