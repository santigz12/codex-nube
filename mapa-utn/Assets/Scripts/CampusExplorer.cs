using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace UtnMap {
 public class CampusExplorer : MonoBehaviour {
  public Camera viewCamera;
  public CharacterController walker;
  public GameObject roofs;
  public GameObject[] sectorRoofs=new GameObject[0];
  void SetRoofs(bool visible){if(roofs)roofs.SetActive(visible);foreach(var roof in sectorRoofs)if(roof)roof.SetActive(visible);}
  
  public Vector3 entrance;
  public RoomInfo[] rooms;
  public bool walking;
  public string selectedId="";
  Vector3 target=new Vector3(4,0,55);
  float yaw=32,pitch=58,distance=215,lookYaw,lookPitch,vertical;
  string query=""; Vector2 listScroll; bool showList;
  GUIStyle title,body,small,button,panel,roomButton;
  Texture2D background,accent; Rect header,pane; int frames;



  void Start(){ Application.targetFrameRate=60; rooms=rooms.OrderBy(r=>r.displayName).ToArray(); ToMap(); if(Application.platform!=RuntimePlatform.WebGLPlayer && Environment.GetCommandLineArgs().Contains("-utnSmoke")) StartCoroutine(Smoke()); }
  public void ToMap(){viewCamera.nearClipPlane=1.5f;walking=false;walker.enabled=false;SetRoofs(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  public void Walk(){viewCamera.nearClipPlane=.07f;walking=true;showList=false;walker.enabled=false;walker.transform.position=entrance+Vector3.up*.08f;walker.enabled=true;lookYaw=0;lookPitch=0;vertical=0;SetRoofs(true);if(Application.platform!=RuntimePlatform.WebGLPlayer){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}else{Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
  
  
  
  void Update(){
   frames++; bool typing=GUI.GetNameOfFocusedControl()=="RoomSearch";
   if(Input.GetKeyDown(KeyCode.Escape)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
   if(Input.GetKeyDown(KeyCode.M)&&!typing)ToMap();
   if(Input.GetKeyDown(KeyCode.Home)){target=new Vector3(4,0,55);distance=215;yaw=32;pitch=58;}
   if(walking){
    if(Application.platform!=RuntimePlatform.WebGLPlayer&&Input.GetMouseButtonDown(0)&&!OverUI()){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
    if(Cursor.lockState==CursorLockMode.Locked||(Application.platform==RuntimePlatform.WebGLPlayer&&Input.GetMouseButton(1)&&!OverUI())){lookYaw+=Input.GetAxis("Mouse X")*2.1f;lookPitch=Mathf.Clamp(lookPitch-Input.GetAxis("Mouse Y")*2.1f,-80,80);}
    walker.transform.rotation=Quaternion.Euler(0,lookYaw,0);
    if(walker.enabled){ if(walker.isGrounded&&vertical<0)vertical=-2;vertical-=18*Time.deltaTime; var move=typing?Vector3.zero:Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")),1); walker.Move((walker.transform.TransformDirection(move)*3.3f+Vector3.up*vertical)*Mathf.Min(Time.deltaTime,.05f));if(walker.transform.position.y < -8){walker.enabled=false;walker.transform.position=entrance;walker.enabled=true;} }
    viewCamera.transform.SetPositionAndRotation(walker.transform.position+Vector3.up*1.00f,Quaternion.Euler(lookPitch,lookYaw,0));
   } else {
    if(!OverUI()){
     distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*distance*.055f,12,270);
     if(Input.GetMouseButton(1)){yaw+=Input.GetAxis("Mouse X")*3;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*3,25,88);}
     if(Input.GetMouseButton(2)){target+=Quaternion.Euler(0,yaw,0)*new Vector3(-Input.GetAxis("Mouse X"),0,-Input.GetAxis("Mouse Y"))*distance*.012f;}
     if(Input.GetMouseButtonDown(0)&&Physics.Raycast(viewCamera.ScreenPointToRay(Input.mousePosition),out var hit,800)){var room=hit.collider.GetComponentInParent<RoomInfo>();if(room)Select(room,false);}
    }
    var v=typing?Vector3.zero:new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));target+=Quaternion.Euler(0,yaw,0)*v*distance*.3f*Time.deltaTime;
    var rot=Quaternion.Euler(pitch,yaw,0);viewCamera.transform.SetPositionAndRotation(target-rot*Vector3.forward*distance,rot);
   }
  }
  bool OverUI(){var p=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);return header.Contains(p)||(showList&&pane.Contains(p))||p.y>Screen.height-65;}
  public void Select(RoomInfo room,bool focus){selectedId=room.id;if(focus){target=room.focusPoint;distance=24;}}
  void Styles(){if(title!=null)return;background=new Texture2D(1,1);background.SetPixel(0,0,new Color(.06f,.12f,.14f,.95f));background.Apply();accent=new Texture2D(1,1);accent.SetPixel(0,0,new Color(.16f,.44f,.40f));accent.Apply(); panel=new GUIStyle(GUI.skin.box){normal={background=background},padding=new RectOffset(18,18,14,14)};title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};body=new GUIStyle(GUI.skin.label){fontSize=15,normal={textColor=new Color(.88f,.94f,.92f)},wordWrap=true};small=new GUIStyle(body){fontSize=12};button=new GUIStyle(GUI.skin.button){fontSize=14,padding=new RectOffset(12,12,9,9),normal={background=accent,textColor=Color.white},hover={background=accent,textColor=Color.white}};roomButton=new GUIStyle(button){alignment=TextAnchor.MiddleLeft,wordWrap=true,fontSize=13};}
  void OnGUI(){Styles();var scale=Mathf.Clamp(Screen.width/1280f,.72f,1.45f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;header=new Rect(14,14,w-28,80);GUI.Box(header,"",panel);GUI.Label(new Rect(32,22,420,31),"UTN / TUCUMÁN",title);GUI.Label(new Rect(33,55,450,24),"PLANTA BAJA   ·   Campus interactivo",small);
   if(GUI.Button(new Rect(w-442,33,132,40),showList?"Cerrar espacios":"62 espacios",button)){showList=!showList;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
   if(GUI.Button(new Rect(w-298,33,132,40),walking?"Vista general":"Recorrer campus",button)){if(walking)ToMap();else Walk();}
   if(GUI.Button(new Rect(w-154,33,120,40),"Centrar",button)){ToMap();target=new Vector3(4,0,55);distance=215;}
   var room=rooms.FirstOrDefault(r=>r.id==selectedId);pane=new Rect(14,105,292,h-180);
   if(showList){GUI.Box(pane,"",panel);GUI.Label(new Rect(28,118,250,25),"ESPACIOS DE PLANTA BAJA",small);GUI.SetNextControlName("RoomSearch");query=GUI.TextField(new Rect(28,151,264,28),query);var matches=rooms.Where(r=>string.IsNullOrWhiteSpace(query)||r.displayName.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||r.code.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();listScroll=GUI.BeginScrollView(new Rect(28,191,264,h-285),listScroll,new Rect(0,0,244,matches.Length*48));for(int i=0;i<matches.Length;i++)if(GUI.Button(new Rect(0,i*48,242,43),matches[i].displayName,roomButton)){ToMap();Select(matches[i],true);}GUI.EndScrollView();}
   GUI.Box(new Rect(14,h-65,w-28,51),"",panel);var label=room?room.displayName+"  ·  "+Category(room.category):(walking?"Recorrido desde Entrada Rivadavia":"Seleccioná un espacio o explorá el campus");GUI.Label(new Rect(28,h-58,w-56,22),label,body);GUI.Label(new Rect(28,h-35,w-56,20),walking?(Application.platform==RuntimePlatform.WebGLPlayer?"WASD / flechas: caminar   ·   Botón derecho + mouse: mirar   ·   M: mapa":"WASD / flechas: caminar   ·   Mouse: mirar   ·   Esc: liberar cursor   ·   M: mapa"):"Rueda: zoom   ·   Botón derecho: girar   ·   Botón central / WASD: desplazar   ·   Inicio: centrar",small);
   header=new Rect(header.x*scale,header.y*scale,header.width*scale,header.height*scale);pane=new Rect(pane.x*scale,pane.y*scale,pane.width*scale,pane.height*scale);
  }
  public static string Category(string c){switch(c){case "classroom":return "Aula";case "lab":return "Laboratorio";case "office":return "Oficina";case "outdoor":return "Exterior";default:return "Servicio";}}
  IEnumerator Smoke(){yield return new WaitForSeconds(3);var args=Environment.GetCommandLineArgs();var ci=Array.IndexOf(args,"-utnCapture");var captures=ci>=0&&ci+1<args.Length?args[ci+1]:Application.persistentDataPath;System.IO.Directory.CreateDirectory(captures);ScreenCapture.CaptureScreenshot(captures+"/runtime-mapa.png");yield return new WaitForSeconds(1);Walk();yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(captures+"/runtime-entrada.png");Debug.Log("UTN_ROOFS_WALK="+sectorRoofs.All(r=>r.activeSelf));var start=walker.transform.position;for(int i=0;i<120;i++){walker.Move(Vector3.forward*.025f);yield return null;}Debug.Log("UTN_SMOKE walking distance="+Vector3.Distance(start,walker.transform.position)+" rooms="+rooms.Length);ToMap();Debug.Log("UTN_ROOFS_MAP_HIDDEN="+sectorRoofs.All(r=>!r.activeSelf));yield return new WaitForSeconds(1);Application.Quit();}
 }
}
