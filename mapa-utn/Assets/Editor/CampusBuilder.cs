using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UtnMap;
public static class CampusBuilder {
 public const float WallHeight=4f, DoorHeight=2f, WallModule=4f;
 const string Gen="Assets/Generated";
 static JObject data,env,graph,meta;
 static float s;
 static Transform architecture,roofs,props,landscape,labels;
 static Material wall,stone,grass,paving,ink,wood,white,water;
 static Dictionary<string,Material> mats;
 static Dictionary<string,GameObject> prefabs;
 static List<Vector2[]> outlines,courtyards;
 static List<Vector2> doors;
 static List<RoomInfo> rooms;
 static int meshId;
 static HashSet<string> wallKeys; static List<Vector2> wallPoints;
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static float N(JToken t,string k,float fallback=0)=>t?[k]==null?fallback:(float)t[k];
 static string T(JToken t,string k,string fallback="")=>t?[k]?.ToString()??fallback;
 static IEnumerable<JToken> Items(JToken t)=>t as JArray??new JArray();
 static Vector2[] Ring(JToken t)=>Items(t).Select(p=>new Vector2((float)p[0],(float)p[1])).ToArray();
 static Vector2 V(JToken t)=>new Vector2(N(t,"x"),N(t,"y"));
 static Vector3 W(Vector2 p,float y)=>new Vector3((p.x-264.2f)*s,y,(918-p.y)*s);
 static bool Inside(Vector2 p,Vector2[] poly){bool c=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){var a=poly[i];var b=poly[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)c=!c;}return c;}
 static Vector2 Center(Vector2[] p)=>p.Aggregate(Vector2.zero,(a,b)=>a+b)/p.Length;
 static bool InBuilding(Vector2 p)=>outlines.Any(r=>Inside(p,r));
 static float RawHeight(Vector2 p){
  foreach(var t in Items(env["terraces"]))if(Inside(p,Ring(t["outline"])))return N(t,"elevation");
  foreach(var t in Items(env["surfaces"]))if(t["elevation"]!=null&&Inside(p,Ring(t["path"])))return N(t,"elevation");
  if(!InBuilding(p))return -2.5f;
  foreach(var t in Items(env["courtyards"]))if(Inside(p,Ring(t["outline"]))&&t["curb"]==null)return -.5f;
  foreach(var t in Items(env["gradeZones"]))if(Inside(p,Ring(t["outline"])))return N(t,"elevation");
  return N(env,"grade");
 }
 static float Height(Vector2 p){
  foreach(var t in Items(env["gradeTransitions"])){
   float x=N(t,"x"),y=N(t,"y"),w=N(t,"width"),h=N(t,"height");if(p.x<x||p.x>x+w||p.y<y||p.y>y+h)continue;
   var dir=T(t,"direction");Vector2 low=dir=="north"?new Vector2(x+w/2,y+h+.2f):dir=="south"?new Vector2(x+w/2,y-.2f):dir=="east"?new Vector2(x-.2f,y+h/2):new Vector2(x+w+.2f,y+h/2);
   float f=dir=="north"?1-(p.y-y)/h:dir=="south"?(p.y-y)/h:dir=="east"?(p.x-x)/w:1-(p.x-x)/w;
   return (RawHeight(low)+f*N(t,"elevation"))*s;
  }return RawHeight(p)*s;
 }
 static Transform Group(string name,Transform parent=null){var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform;}
 static Material Mat(string name,string hex){if(mats.TryGetValue(name,out var m))return m;ColorUtility.TryParseHtmlString(hex,out var c);m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=c};m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,Gen+"/Materials/"+name+".mat");mats[name]=m;return m;}
 static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material m,bool collider=true){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;if(!collider)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.isStatic=true;return o;}
 static GameObject MeshObject(string name,Transform parent,List<Vector3> verts,List<int> triangles,Material material,bool collider=true,List<Vector2> uv=null){var mesh=new Mesh{name=name,indexFormat=verts.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);if(uv!=null)mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Gen+"/Meshes/mesh_"+(meshId++)+".asset");var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=material;if(collider)o.AddComponent<MeshCollider>().sharedMesh=mesh;o.isStatic=true;return o;}
 static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
 static List<int> Triangulate(Vector2[] p){var remaining=Enumerable.Range(0,p.Length).ToList();float area=0;for(int i=0;i<p.Length;i++){var b=p[(i+1)%p.Length];area+=p[i].x*b.y-b.x*p[i].y;}if(area<0)remaining.Reverse();var result=new List<int>();int guard=p.Length*p.Length;while(remaining.Count>2&&guard-->0){bool cut=false;for(int i=0;i<remaining.Count;i++){int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];if(Cross(p[a],p[b],p[c])<=.001f)continue;bool occupied=remaining.Any(k=>k!=a&&k!=b&&k!=c&&Cross(p[a],p[b],p[k])>-.001f&&Cross(p[b],p[c],p[k])>-.001f&&Cross(p[c],p[a],p[k])>-.001f);if(occupied)continue;result.AddRange(new[]{a,b,c});remaining.RemoveAt(i);cut=true;break;}if(!cut)break;}if(result.Count==0)throw new Exception("Cannot triangulate polygon");return result;}
 static GameObject Polygon(string name,Transform parent,Vector2[] p,float height,Material m,bool collide=true){return MeshObject(name,parent,p.Select(v=>W(v,height)).ToList(),Triangulate(p),m,collide);}
 static void Label(string text,Transform parent,Vector3 p,float size=1,bool flat=true){var o=new GameObject(text,typeof(TextMesh));o.transform.SetParent(parent,false);o.transform.position=p;o.transform.rotation=flat?Quaternion.Euler(90,0,0):Quaternion.identity;var tm=o.GetComponent<TextMesh>();tm.text=text;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=64;tm.characterSize=.08f*size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.13f,.24f,.25f);o.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;}
 static Vector2 Project(Vector2 p,Vector2 a,Vector2 b)=>a+Vector2.Dot(p-a,b-a)/Mathf.Max(.001f,(b-a).sqrMagnitude)*(b-a);
 static void SegmentPiece(string name,Transform parent,Vector2 a,Vector2 b,float y,float h,Material m,float thickness=.18f){int count=Mathf.CeilToInt(Vector2.Distance(a,b)*s/WallModule); if(count>1){for(int i=0;i<count;i++)SegmentPiece(name,parent,Vector2.Lerp(a,b,(float)i/count),Vector2.Lerp(a,b,(float)(i+1)/count),y,h,m,thickness);return;} var pa=W(a,y+h/2);var pb=W(b,y+h/2);var o=Box(name,parent,(pa+pb)/2,new Vector3(thickness,h,Vector3.Distance(pa,pb)),m);o.transform.rotation=Quaternion.LookRotation(pb-pa);}
 static void Segment(string name,Transform parent,Vector2 a,Vector2 b,float y,float h,Material m,float thickness=.18f){
 var delta=b-a;float len=delta.magnitude;var cuts=new List<float>{0,1};foreach(var point in wallPoints){var q=Project(point,a,b);float t=Vector2.Dot(q-a,delta)/(len*len);if(t>.001f&&t<.999f&&Vector2.Distance(q,point)<.03f)cuts.Add(t);}cuts=cuts.OrderBy(v=>v).ToList();for(int i=0;i<cuts.Count-1;i++){var aa=Vector2.Lerp(a,b,cuts[i]);var bb=Vector2.Lerp(a,b,cuts[i+1]);if(Vector2.Distance(aa,bb)*s<.015f)continue;var p1=W(aa,y);var p2=W(bb,y);string k1=$"{p1.x:F2},{p1.z:F2}",k2=$"{p2.x:F2},{p2.z:F2}";string key=string.CompareOrdinal(k1,k2)<0?k1+"/"+k2:k2+"/"+k1;key+="/"+(name=="Dintel"?"Dintel":name=="Cornisa"?"Cornisa":"Muro");if(wallKeys.Add(key))SegmentPiece(name,parent,aa,bb,y,h,m,thickness);}
 }
 static float OpeningFloor(Vector2 p,float fallback){float result=fallback;foreach(var room in Items(data["rooms"])){var node=graph[T(room,"entryNodeId")];if(node!=null&&Vector2.Distance(V(node),p)*s<1f)result=Mathf.Max(result,RawHeight(Center(Ring(room["polygon"])))*s);}return result;}
 static void Walls(Vector2[] poly,Transform parent,float floor,string prefix,Material cap){for(int i=0;i<poly.Length;i++){
  Vector2 a=poly[i],b=poly[(i+1)%poly.Length];float len=Vector2.Distance(a,b);if(len<.1f)continue;float wallFloor=float.IsNaN(floor)?Height((a+b)/2):floor;
  var intervals=new List<Vector2>();foreach(var d in doors){var q=Project(d,a,b);float t=Vector2.Dot(q-a,b-a)/(len*len);if(t<-.02f||t>1.02f||Vector2.Distance(d,q)*s>.32f)continue;float half=.78f/s/len;intervals.Add(new Vector2(Mathf.Clamp01(t-half),Mathf.Clamp01(t+half)));}
  foreach(var en in Items(env["entrances"])){var d=V(en);var q=Project(d,a,b);var t=Vector2.Dot(q-a,b-a)/(len*len);if(t<0||t>1||Vector2.Distance(d,q)*s>.2f)continue;float half=N(en,"width")*.5f/len;intervals.Add(new Vector2(Mathf.Clamp01(t-half),Mathf.Clamp01(t+half)));}
  var merged=new List<Vector2>(); foreach(var v in intervals.OrderBy(x=>x.x)){if(merged.Count==0||v.x>merged[merged.Count-1].y)merged.Add(v);else {var prev=merged[merged.Count-1];merged[merged.Count-1]=new Vector2(prev.x,Mathf.Max(prev.y,v.y));}} float last=0;foreach(var hole in merged){if(hole.y>hole.x+.001f){float openingFloor=OpeningFloor(Vector2.Lerp(a,b,(hole.x+hole.y)/2),wallFloor);Segment("Dintel",parent,Vector2.Lerp(a,b,hole.x),Vector2.Lerp(a,b,hole.y),openingFloor+DoorHeight,wallFloor+WallHeight-.14f-openingFloor-DoorHeight,wall);Segment("Cornisa",parent,Vector2.Lerp(a,b,hole.x),Vector2.Lerp(a,b,hole.y),wallFloor+WallHeight-.14f,.14f,cap,.21f);}if(hole.x>last+.001f){var aa=Vector2.Lerp(a,b,last);var bb=Vector2.Lerp(a,b,hole.x);Segment(prefix,parent,aa,bb,wallFloor,WallHeight-.14f,wall);Segment("Cornisa",parent,aa,bb,wallFloor+WallHeight-.14f,.14f,cap,.21f);}last=Mathf.Max(last,hole.y);}if(last<.999f){Segment(prefix,parent,Vector2.Lerp(a,b,last),b,wallFloor,WallHeight-.14f,wall);Segment("Cornisa",parent,Vector2.Lerp(a,b,last),b,wallFloor+WallHeight-.14f,.14f,cap,.21f);}
 }}
 static void LoadAssets(){prefabs=new Dictionary<string,GameObject>();foreach(var file in Directory.GetFiles("Assets/ThirdParty","*.obj.txt",SearchOption.AllDirectories)){
  var name=Path.GetFileName(file).Replace(".obj.txt","");var directory=Path.GetDirectoryName(file).Replace('\\','/');var colors=new Dictionary<string,Material>();string current="default";var mtl=file.Replace(".obj.txt",".mtl.txt");if(File.Exists(mtl))foreach(var line in File.ReadAllLines(mtl)){var a=line.Trim().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(a.Length==0)continue;if(a[0]=="newmtl")current=a[1];if(a[0]=="Kd"){var c=new Color(float.Parse(a[1],Inv),float.Parse(a[2],Inv),float.Parse(a[3],Inv));colors[current]=Mat(Path.GetFileName(directory)+"_"+current,"#"+ColorUtility.ToHtmlStringRGB(c));}if(a[0]=="map_Kd"&&colors.ContainsKey(current)){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(directory+"/"+a[1]);if(tex)colors[current].SetTexture("_BaseMap",tex);}}
  var pos=new List<Vector3>();var texcoords=new List<Vector2>();var groups=new Dictionary<string,List<string[]>>();current="default";
  foreach(var line in File.ReadAllLines(file)){var a=line.Trim().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(a.Length==0)continue;if(a[0]=="v")pos.Add(new Vector3(float.Parse(a[1],Inv),float.Parse(a[2],Inv),float.Parse(a[3],Inv)));if(a[0]=="vt")texcoords.Add(new Vector2(float.Parse(a[1],Inv),float.Parse(a[2],Inv)));if(a[0]=="usemtl")current=a[1];if(a[0]=="f"){if(!groups.ContainsKey(current))groups[current]=new List<string[]>();groups[current].Add(a.Skip(1).ToArray());}}
  var bounds=new Bounds(pos[0],Vector3.zero);foreach(var p in pos)bounds.Encapsulate(p);var origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);var root=Group(name);
  foreach(var kv in groups){var vs=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();foreach(var face in kv.Value){int first=vs.Count;foreach(var f in face){var a=f.Split('/');int v=int.Parse(a[0]);vs.Add(pos[v>0?v-1:pos.Count+v]-origin);int ti=a.Length>1&&a[1]!=""?int.Parse(a[1]):0;uv.Add(ti>0?texcoords[ti-1]:Vector2.zero);}for(int k=1;k<face.Length-1;k++)tris.AddRange(new[]{first,first+k,first+k+1});}MeshObject(kv.Key,root,vs,tris,colors.TryGetValue(kv.Key,out var mat)?mat:white,false,uv);}
  prefabs[name]=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Gen+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
 }}
 static GameObject Model(string key,Transform parent,Vector3 position,Vector3 dimensions,float angle=0,bool collider=true){if(!prefabs.TryGetValue(key,out var prefab))throw new Exception("Missing model "+key);var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);var b=new Bounds();bool first=true;foreach(var mf in o.GetComponentsInChildren<MeshFilter>()){if(first){b=mf.sharedMesh.bounds;first=false;}else b.Encapsulate(mf.sharedMesh.bounds);}o.transform.localScale=new Vector3(dimensions.x/Mathf.Max(.001f,b.size.x),dimensions.y/Mathf.Max(.001f,b.size.y),dimensions.z/Mathf.Max(.001f,b.size.z));o.transform.position=position;o.transform.rotation=Quaternion.Euler(0,angle,0);if(collider){var c=o.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}o.isStatic=true;return o;}
 static Material Category(string c){switch(c){case "classroom":return Mat("Aulas","#8FBCC9");case "lab":return Mat("Laboratorios","#B5A3C8");case "office":return Mat("Oficinas","#8DBCB1");case "outdoor":return grass;default:return Mat("Servicios","#D5BC84");}}

 static List<Vector2> Clip(List<Vector2> poly,Func<Vector2,float> side,bool inside){var result=new List<Vector2>();if(poly.Count==0)return result;for(int i=0;i<poly.Count;i++){var a=poly[i];var b=poly[(i+1)%poly.Count];float da=side(a),db=side(b);bool ia=inside?da>=0:da<=0,ib=inside?db>=0:db<=0;if(ia)result.Add(a);if(ia!=ib)result.Add(Vector2.Lerp(a,b,da/(da-db)));}return result;}
 static void ExteriorSurface(string name,Vector2[] p,float h,Material m){var indices=Triangulate(p);var polys=new List<List<Vector2>>();for(int k=0;k<indices.Count;k+=3)polys.Add(new List<Vector2>{p[indices[k]],p[indices[k+1]],p[indices[k+2]]});foreach(var building in outlines.Skip(1)){float left=building.Min(v=>v.x)-.8f,right=building.Max(v=>v.x)+.8f,top=building.Min(v=>v.y)-.8f,bottom=building.Max(v=>v.y)+.8f;var next=new List<List<Vector2>>();foreach(var polygon in polys){var rest=polygon;foreach(var plane in new Func<Vector2,float>[]{v=>v.x-left,v=>right-v.x,v=>v.y-top,v=>bottom-v.y}){var outside=Clip(rest,plane,false);if(outside.Count>=3)next.Add(outside);rest=Clip(rest,plane,true);if(rest.Count<3)break;}}polys=next;}
 var verts=new List<Vector3>();var tris=new List<int>();foreach(var polygon in polys){int start=verts.Count;verts.AddRange(polygon.Select(v=>W(v,h)));for(int i=1;i<polygon.Count-1;i++)tris.AddRange(new[]{start,start+i,start+i+1});}MeshObject(name,landscape,verts,tris,m);
 }
 static void Ground(){
  Box("Terreno",landscape,W(new Vector2(290,470),-.725f),new Vector3(185,.35f,188),Mat("Terreno","#8EA78E"));
  foreach(var surface in Items(env["surfaces"])){var p=Ring(surface["path"]);if(p.Length<3)continue;var material=T(surface,"kind")=="grass"?grass:paving;ExteriorSurface(T(surface,"id"),p,(surface["elevation"]!=null?N(surface,"elevation")*s:-2.5f*s)+(T(surface,"kind")=="grass"?(T(surface,"id").StartsWith("platabanda")?.04f:-.005f):.015f),material);}
  foreach(var t in Items(env["terraces"]))Polygon(T(t,"id"),architecture,Ring(t["outline"]),N(t,"elevation")*s,stone);
  // Tessellated common floor follows grade zones and transition ramps, avoiding filled courtyard holes.
  var verts=new List<Vector3>();var tris=new List<int>();float step=3.2f;
  for(float x=-270;x<955;x+=step)for(float y=-146;y<957;y+=step){var center=new Vector2(x+step/2,y+step/2);if(!InBuilding(center)||courtyards.Any(p=>Inside(center,p)))continue;int k=verts.Count;foreach(var p in new[]{new Vector2(x,y),new Vector2(x+step,y),new Vector2(x+step,y+step),new Vector2(x,y+step)})verts.Add(W(p,Height(p)-.016f));tris.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}
  MeshObject("Circulacion y cotas PB",architecture,verts,tris,stone);
  foreach(var court in Items(env["courtyards"]))Polygon(T(court,"label"),landscape,Ring(court["outline"]),-.5f*s,grass);
  foreach(var path in Items(env["walkways"])){
 foreach(var rect in Items(path["rects"])){float x=N(rect,"x"),y=N(rect,"y"),w=N(rect,"width"),h=N(rect,"height");Box(T(path,"id"),landscape,W(new Vector2(x+w/2,y+h/2),-.5f*s+.018f),new Vector3(w*s,.035f,h*s),paving);}
 foreach(var r in Items(path["roundabouts"])){var vs=new List<Vector3>();var ts=new List<int>();for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vs.Add(W(V(r)+d*N(r,"r"),-.5f*s+.04f));vs.Add(W(V(r)+d*N(r,"island"),-.5f*s+.04f));if(i<48){int k=i*2;ts.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}}MeshObject("Rotonda patio",landscape,vs,ts,paving,false);}
 }
  // Readable perimeter streets; neighboring city is deliberately omitted.
  Box("Rivadavia",landscape,W(new Vector2(290,1155),-.35f),new Vector3(182,.1f,9),Mat("Asfalto","#68767A"));
  Box("Laprida",landscape,W(new Vector2(290,-223),-.35f),new Vector3(182,.1f,9),mats["Asfalto"]);
  Box("España",landscape,W(new Vector2(-445,470),-.35f),new Vector3(9,.1f,180),mats["Asfalto"]);
  Box("Italia",landscape,W(new Vector2(1040,470),-.35f),new Vector3(9,.1f,180),mats["Asfalto"]);
  foreach(var t in Items(meta["exteriorLabels"]))Label(T(t,"text").ToUpperInvariant(),labels,W(V(t),-.20f),T(t,"kind")=="place"?.65f:1.2f);
 }
 static bool SafeFurniture(Vector2 point,Vector2[] poly,Vector2 door,float rx,float rz){float dx=rx/s,dz=rz/s;foreach(var q in new[]{point+new Vector2(dx,dz),point+new Vector2(-dx,dz),point+new Vector2(dx,-dz),point+new Vector2(-dx,-dz)})if(!Inside(q,poly))return false;return Vector2.Distance(point,door)*s>1.8f;}
 static void Furnish(RoomInfo room,Vector2[] poly,Vector2 entry,float y){if(room.category=="outdoor")return;var parent=Group(room.code+" · "+room.displayName,props);var c=Center(poly);float minX=poly.Min(p=>p.x),maxX=poly.Max(p=>p.x),minY=poly.Min(p=>p.y),maxY=poly.Max(p=>p.y);float width=(maxX-minX)*s,depth=(maxY-minY)*s;var cat=room.category;
  if(cat=="classroom"){
   int columns=room.id=="aula-magna"?4:2;int rows=3;
   for(int r=0;r<rows;r++)for(int col=0;col<columns;col++){var p=new Vector2((minX+maxX)/2+((col-(columns-1)*.5f)*1.65f)/s,(minY+maxY)/2+((r-1)*1.8f)/s);if(!SafeFurniture(p,poly,entry,.68f,.8f))continue;var wp=W(p,y+.025f);if(room.id!="aula-magna")Model("table",parent,wp,new Vector3(.95f,.73f,.55f));Model("chair",parent,wp+Vector3.back*.57f,new Vector3(.44f,.86f,.48f));room.furnitureCount++;}
   var board=W(new Vector2((minX+maxX)/2,minY+1.3f),y+1.65f);Box("Pizarra",parent,board,new Vector3(Mathf.Min(width*.65f,2.4f),1.05f,.07f),Mat("Pizarra","#27584B"),false);
  }else if(cat=="lab"){
   for(int i=0;i<2;i++){var p=c+new Vector2((i==0?-1:1)*Mathf.Min(width*.24f,1.9f)/s,0);if(!SafeFurniture(p,poly,entry,.7f,.75f))continue;Model("table",parent,W(p,y+.025f),new Vector3(1.3f,.86f,.75f));if(room.id.Contains("lab-")||room.id.Contains("sistemas"))Model("computerScreen",parent,W(p,y+.89f),new Vector3(.45f,.35f,.18f),180,false);room.furnitureCount++;}
  }else if(cat=="office"){
   var p=c+new Vector2(Mathf.Min(width*.2f,1.2f)/s,0);if(SafeFurniture(p,poly,entry,.7f,.7f)){Model("desk",parent,W(p,y+.025f),new Vector3(1.25f,.76f,.65f));Model("chairDesk",parent,W(p,y)+Vector3.back*.65f,new Vector3(.48f,.85f,.5f));room.furnitureCount++;}
  }else{
   var p=c;if(!SafeFurniture(p,poly,entry,.6f,.6f))return;
   if(room.id.Contains("bano")){Model("toilet",parent,W(p,y+.025f),new Vector3(.45f,.8f,.7f));Model("bathroomSink",parent,W(p,y)+Vector3.right*.9f,new Vector3(.6f,.85f,.4f));}
   else if(room.id=="cafeteria"){Model("table",parent,W(p,y+.025f),new Vector3(1.3f,.75f,.8f));Model("chair",parent,W(p,y)+Vector3.back*.65f,new Vector3(.45f,.85f,.48f));}
   else Model("desk",parent,W(p,y+.025f),new Vector3(1.2f,.75f,.6f));room.furnitureCount++;
  }
 }
 static void Rooms(){rooms=new List<RoomInfo>();foreach(var token in Items(data["rooms"])){var poly=Ring(token["polygon"]);var center=Center(poly);var g=Group(T(token,"id"),architecture);var info=g.gameObject.AddComponent<RoomInfo>();info.id=T(token,"id");info.displayName=T(token,"name");info.category=T(token,"category");info.code=T(token,"code",info.id);var node=graph[T(token,"entryNodeId")];Vector2 entry=node!=null?V(node):center;float y=RawHeight(center)*s;info.focusPoint=W(center,y);info.entryPoint=W(entry,Height(entry));rooms.Add(info);var material=Category(info.category);
  Polygon("Piso · "+info.code,g,poly,y+.008f,material);
  if(info.category!="outdoor"){Walls(poly,g,y,"Pared",material);var roof=Polygon("Techo · "+info.code,roofs,poly,y+WallHeight+.04f,stone,false);var rend=roof.GetComponent<MeshRenderer>();rend.shadowCastingMode=ShadowCastingMode.TwoSided; // visible ceiling uses a reversed copy
   var ceiling=Polygon("Cielorraso · "+info.code,roofs,poly,y+WallHeight,white,false);var mesh=ceiling.GetComponent<MeshFilter>().sharedMesh;var tr=mesh.triangles;Array.Reverse(tr);mesh.triangles=tr;mesh.RecalculateNormals();
  }
  Label(info.code,labels,W(center,y+.04f),Mathf.Clamp(Mathf.Min(poly.Max(v=>v.x)-poly.Min(v=>v.x),poly.Max(v=>v.y)-poly.Min(v=>v.y))*s/6,.3f,1));
  if(info.category!="outdoor"){
 var outward=(W(entry,0)-W(center,0)).normalized;var sign=Group("Cartel · "+info.code,g);sign.position=W(entry,y+2.55f)+outward*.15f;sign.rotation=Quaternion.LookRotation(-outward);var plate=Box("Placa",sign,sign.position,new Vector3(1.35f,.28f,.035f),ink,false);plate.transform.rotation=sign.rotation;Label(info.code,sign,sign.position+outward*.025f,.27f,false);var text=sign.GetComponentInChildren<TextMesh>();text.color=Color.white;text.transform.rotation=sign.rotation;
 }
 Furnish(info,poly,entry,y);
 }}
 static void Transitions(){foreach(var t in Items(env["gradeTransitions"])){
  var dir=T(t,"direction");float x=N(t,"x"),y=N(t,"y"),w=N(t,"width"),h=N(t,"height"),rise=N(t,"elevation")*s;bool alongX=dir=="east"||dir=="west";
  var low=dir=="north"?new Vector2(x+w/2,y+h+.2f):dir=="south"?new Vector2(x+w/2,y-.2f):dir=="east"?new Vector2(x-.2f,y+h/2):new Vector2(x+w+.2f,y+h/2);float start=RawHeight(low)*s;var group=Group(T(t,"id"),architecture);
  var corners=new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)};
  var vs=corners.Select(p=>{float f=dir=="north"?1-(p.y-y)/h:dir=="south"?(p.y-y)/h:dir=="east"?(p.x-x)/w:1-(p.x-x)/w;return W(p,start+f*rise+.01f);}).ToList();
  // Smooth, invisible ramp collider beneath visual treads guarantees traversable small risers.
  var ramp=MeshObject("Rampa de colision",group,vs,new List<int>{0,1,2,0,2,3},stone);if(T(t,"type")=="ramp")continue;ramp.GetComponent<Renderer>().enabled=false;
  int count=(int)N(t,"risers",4);bool mixed=T(t,"type")=="stairs_with_ramp";
  for(int i=0;i<count;i++){float f=(i+.5f)/count;var point=new Vector2(x+w/2,y+h/2);if(dir=="north")point.y=y+h*(1-f);if(dir=="south")point.y=y+h*f;if(dir=="east")point.x=x+w*f;if(dir=="west")point.x=x+w*(1-f);var dim=new Vector3(w*s,rise*(i+1)/count,h*s);if(alongX)dim.x/=count;else dim.z/=count;if(mixed){dim.x*=.65f;point.x-=w*.175f;}Box("Peldaño "+i,group,W(point,start+dim.y/2),dim,stone,false);}
  if(mixed){var pp=vs.Select(v=>v).ToList();float xx=W(new Vector2(x+w*.65f,y),0).x;pp[0]=new Vector3(xx,vs[0].y,vs[0].z);pp[3]=new Vector3(xx,vs[3].y,vs[3].z);MeshObject("Rampa lateral",group,pp,new List<int>{0,1,2,0,2,3},paving,false);}
 }
 foreach(var st in Items(env["stairwells"])){var p=V(st)+new Vector2(N(st,"width")/2,N(st,"height")/2);var y=Height(p);var group=Group(T(st,"id"),architecture);float width=N(st,"width")*s,depth=N(st,"height")*s;bool up=st["above"]?.Type!=JTokenType.Null&&st["above"]!=null;var model=Model("stairs-closed",group,W(p,y),new Vector3(Mathf.Min(width,2.3f),up?2.2f:1.3f,Mathf.Min(depth,3.5f)),0,false);
 // Closed limit is explicit; higher/lower floors are out of this scene.
 Box("Limite nivel no incluido",group,W(p,y+1.4f)+Vector3.forward*Mathf.Min(depth,3.5f)*.5f,new Vector3(Mathf.Min(width,2.3f),2.8f,.12f),Category("office"));Label(up?"P1 · NO INCLUIDO":"SS · NO INCLUIDO",labels,W(p,y+3.2f),.5f);
 }
 foreach(var e in Items(env["elevators"])){var p=V(e)+new Vector2(N(e,"width")/2,N(e,"height")/2);var pos=W(p,Height(p)+1.4f);Box("Ascensor · sin servicio",architecture,pos,new Vector3(N(e,"width")*s,2.8f,N(e,"height")*s),Mat("Ascensor","#83949C"));}
 }

 static void Shell(){
 var edges=graph.Properties().Where(p=>T(p.Value,"floor")=="PB").SelectMany(p=>Items(p.Value["neighbors"]).Select(n=>new[]{p.Value,graph[n.ToString()]})).Where(e=>e[1]!=null&&T(e[1],"floor")=="PB").ToArray();
 foreach(var poly in outlines.Concat(courtyards)){
 for(int i=0;i<poly.Length;i++){var a=poly[i];var b=poly[(i+1)%poly.Length];foreach(var e in edges){var c=V(e[0]);var d=V(e[1]);var r=b-a;var q=d-c;float det=r.x*q.y-r.y*q.x;if(Mathf.Abs(det)<.001f)continue;var delta=c-a;float t=(delta.x*q.y-delta.y*q.x)/det,u=(delta.x*r.y-delta.y*r.x)/det;if(t>=0&&t<=1&&u>=0&&u<=1)doors.Add(a+t*r);}}
 Walls(poly,architecture,float.NaN,"Limite exterior",stone);
 }
 }
 static void Outdoors(){int i=0;foreach(var t in Items(env["trees"])){var p=V(t);float y=Height(p);Model(i++%2==0?"tree_simple":"tree_small_dark",landscape,W(p,y),new Vector3(2.6f,4.2f,2.6f),(i*137)%360,false);var trunk=Box("Tronco colision",landscape,W(p,y+1),new Vector3(.35f,2,.35f),wood);trunk.GetComponent<Renderer>().enabled=false;}
 foreach(var t in Items(env["benches"])){var p=V(t);Model("bench",landscape,W(p,Height(p)+.03f),new Vector3(1.7f,.85f,.55f),T(t,"axis")=="north-south"?90:0);}
 for(int j=0;j<6;j++){var p=j<3?new Vector2(-230+j*70,790):new Vector2(850,400+(j-3)*65);Model(j%2==0?"sedan":"hatchback-sports",landscape,W(p,-2.5f*s+.03f),new Vector3(1.8f,1.45f,4.2f),90);}
 foreach(var court in rooms.Where(r=>r.id.StartsWith("cancha"))){var pos=court.focusPoint;var poly=Ring(Items(data["rooms"]).First(r=>T(r,"id")==court.id)["polygon"]);float width=(poly.Max(p=>p.x)-poly.Min(p=>p.x))*s,depth=(poly.Max(p=>p.y)-poly.Min(p=>p.y))*s;float y=pos.y+.03f;foreach(var sign in new[]{-1,1}){Box("Linea lateral",landscape,new Vector3(pos.x+sign*(width/2-.5f),y,pos.z),new Vector3(.08f,.025f,depth-1),white,false);Box("Linea fondo",landscape,new Vector3(pos.x,y,pos.z+sign*(depth/2-.5f)),new Vector3(width-1,.025f,.08f),white,false);}Box("Linea central",landscape,new Vector3(pos.x,y,pos.z),new Vector3(width-1,.025f,.08f),white,false);}
 }
 [MenuItem("UTN/Generar planta baja")]
 public static void Generate(){
  data=JObject.Parse(File.ReadAllText("Assets/Data/planta_baja.json"));env=(JObject)data["envelope"];graph=JObject.Parse(File.ReadAllText("Assets/Data/graph.json"));meta=JObject.Parse(File.ReadAllText("Assets/Data/metadata.json"));s=N(meta["scale"],"meters")/Vector2.Distance(V(meta["scale"]["from"]),V(meta["scale"]["to"]));
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);if(AssetDatabase.IsValidFolder(Gen))AssetDatabase.DeleteAsset(Gen);foreach(var d in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Gen+"/"+d);AssetDatabase.Refresh();mats=new Dictionary<string,Material>();meshId=0;
  wall=Mat("Muros","#E6E5DC");stone=Mat("Circulacion","#D4D5CD");grass=Mat("Cesped","#729C80");paving=Mat("Adoquines","#B4B1A0");ink=Mat("Tinta","#304F52");wood=Mat("Tronco","#866248");white=Mat("Blanco","#F4F3E8");water=Mat("Azul","#709DA6");
  outlines=Items(env["outlines"]).Select(Ring).ToList();courtyards=Items(env["courtyards"]).Select(t=>Ring(t["outline"])).ToList();doors=graph.Properties().Where(p=>p.Name.StartsWith("door-")&&T(p.Value,"floor")=="PB").Select(p=>V(p.Value)).ToList();
  var world=Group("UTN · Planta baja");architecture=Group("01 Arquitectura",world);roofs=Group("02 Techos",world);props=Group("03 Mobiliario",world);landscape=Group("04 Entorno y vegetacion",world);labels=Group("05 Rotulos",world);
  wallKeys=new HashSet<string>();wallPoints=Items(data["rooms"]).SelectMany(t=>Ring(t["polygon"])).Concat(outlines.SelectMany(p=>p)).Concat(courtyards.SelectMany(p=>p)).ToList();foreach(var d in doors)foreach(var v in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})wallPoints.Add(d+v*.78f/s);
  LoadAssets();Ground();Rooms();Shell();Transitions();Outdoors();new GameObject("Superficies exteriores v2");
  var sun=new GameObject("Sol",typeof(Light));var light=sun.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;light.color=new Color(1,.96f,.87f);light.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-32,0);RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.40f,.45f,.48f);RenderSettings.fog=false;
  var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";var cam=camera.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.78f,.84f,.84f);cam.nearClipPlane=1.5f;cam.farClipPlane=600;cam.fieldOfView=50;cam.allowHDR=false;cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
  var runtime=new GameObject("Explorador",typeof(CampusExplorer)).GetComponent<CampusExplorer>();runtime.viewCamera=cam;runtime.roofs=roofs.gameObject;runtime.rooms=rooms.ToArray();runtime.entrance=W(V(graph["node-entry"]),Height(V(graph["node-entry"])));
  var player=new GameObject("Visitante",typeof(CharacterController));var controller=player.GetComponent<CharacterController>();controller.height=1.78f;controller.radius=.24f;controller.center=new Vector3(0,.89f,0);controller.stepOffset=.24f;controller.slopeLimit=55;controller.skinWidth=.025f;controller.enabled=false;runtime.walker=controller;
  var urp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");urp.msaaSampleCount=2;urp.shadowDistance=70;urp.supportsHDR=false;GraphicsSettings.defaultRenderPipeline=urp;for(int q=0;q<QualitySettings.names.Length;q++){QualitySettings.SetQualityLevel(q,false);QualitySettings.renderPipeline=urp;}QualitySettings.vSyncCount=0;
  PlayerSettings.companyName="UTN Campus";PlayerSettings.productName="Mapa UTN · Planta baja";PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);settings.FindProperty("activeInputHandler").intValue=2;settings.ApplyModifiedPropertiesWithoutUndo();
  cam.transform.position=new Vector3(4,0,55)-Quaternion.Euler(58,32,0)*Vector3.forward*215;cam.transform.rotation=Quaternion.Euler(58,32,0);roofs.gameObject.SetActive(false);
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/PlantaBaja.unity",true)};AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/PlantaBaja.unity");
  CampusRevision.Organize();
  Validate();Debug.Log("UTN_GENERATED rooms="+rooms.Count+" meshes="+meshId+" scale="+s); 
 }
 [MenuItem("UTN/Validar escena")]
 public static void Validate(){var found=UnityEngine.Object.FindObjectsByType<RoomInfo>(FindObjectsSortMode.None);if(found.Length!=62)throw new Exception("Expected 62 rooms, got "+found.Length);if(found.Select(r=>r.id).Distinct().Count()!=62)throw new Exception("Duplicate room id");foreach(var r in found){if(!r.GetComponentInChildren<MeshCollider>())throw new Exception("Missing room floor "+r.id);foreach(var ren in r.GetComponentsInChildren<Renderer>())if(ren.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported))throw new Exception("Invalid material "+r.id);}Physics.SyncTransforms();var lines=new List<string>{"62 espacios con IDs unicos; pisos con colisiones; materiales validos.","Escala: 109 / 872.8 = "+(109f/872.8f).ToString(Inv)+" metros/unidad.","Mobiliario por espacio:"};lines.AddRange(found.Select(r=>r.id+": "+r.furnitureCount));Directory.CreateDirectory("Documentacion");File.WriteAllLines("Documentacion/validacion.txt",lines);Debug.Log("UTN_VALIDATION_OK");}
 [MenuItem("UTN/Capturar vistas")]
 public static void Capture(){EditorSceneManager.OpenScene("Assets/Scenes/PlantaBaja.unity");var explorer=UnityEngine.Object.FindFirstObjectByType<CampusExplorer>();var camera=explorer.viewCamera;explorer.roofs.SetActive(false);Directory.CreateDirectory("Capturas");Shot(camera,"01-vista-general",new Vector3(4,0,55)-Quaternion.Euler(58,32,0)*Vector3.forward*215,Quaternion.Euler(58,32,0));foreach(var spec in new[]{new[]{"aula-118","02-aula"},new[]{"lab-159","03-laboratorio"}}){var room=explorer.rooms.First(r=>r.id==spec[0]);var target=room.focusPoint;Shot(camera,spec[1],target+new Vector3(0,11,-5),Quaternion.Euler(65,0,0));}Shot(camera,"04-acceso-desnivel",new Vector3(1,6,21),Quaternion.Euler(42,0,0));}
 static void Shot(Camera cam,string name,Vector3 pos,Quaternion rotation){cam.nearClipPlane=1.5f;cam.transform.SetPositionAndRotation(pos,rotation);var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("Capturas/"+name+".png",image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
 public static void BuildMac()=>Build(BuildTarget.StandaloneOSX,"Builds/macOS/Mapa UTN.app");
 public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Windows/Mapa UTN.exe");
 public static void BuildLinux()=>Build(BuildTarget.StandaloneLinux64,"Builds/Linux/Mapa UTN.x86_64");
 public static void BuildWeb(){PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.decompressionFallback=false;Build(BuildTarget.WebGL,"Builds/Web");File.AppendAllText("Builds/Web/TemplateData/style.css","\nhtml,body{width:100%;height:100%;margin:0;overflow:hidden}#unity-container.unity-desktop{position:fixed;inset:0;width:100%;height:100%;transform:none}#unity-canvas{width:100%!important;height:100%!important;display:block}#unity-footer{display:none}\n");}
 static void Build(BuildTarget target,string path){CampusDelivery.Polish();PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"ar.edu.utn.frt.mapacampus");Directory.CreateDirectory(Path.GetDirectoryName(path));if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,target)&&target!=BuildTarget.WebGL)throw new Exception("Build module missing "+target);PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/PlantaBaja.unity"},locationPathName=path,target=target,options=BuildOptions.None});File.AppendAllText("Documentacion/builds.txt",target+": "+report.summary.result+"; "+report.summary.totalSize+" bytes\n");if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed "+target);}
}
