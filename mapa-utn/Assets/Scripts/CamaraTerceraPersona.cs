using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CamaraTerceraPersona : MonoBehaviour
{
    public Transform objetivo;
    [Min(0)] public float alturaMirada = 1.3f;
    [Min(0.3f)] public float distancia = 4f;
    public float inclinacion = 15f;
    public float giroHorizontal;
    public float sensibilidad = 0.15f;
    public float suavizado = 12f;
    public LayerMask obstaculos = ~0;
    public bool exclusiva = true;
    Vector3 focoSuave;
    bool iniciado;

    void OnEnable()
    {
        iniciado = false;
        if (!Application.isPlaying || !exclusiva) return;
        foreach (var otra in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (otra != GetComponent<Camera>()) otra.enabled = false;
        foreach (var audio in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (audio.gameObject != gameObject) audio.enabled = false;
        foreach (var explorador in FindObjectsByType<UtnMap.CampusExplorer>(FindObjectsSortMode.None))
            explorador.enabled = false;
    }

    void LateUpdate()
    {
        if (!objetivo) return;
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                giroHorizontal += delta.x * sensibilidad;
                inclinacion = Mathf.Clamp(inclinacion - delta.y * sensibilidad, -10f, 65f);
            }
            distancia = Mathf.Clamp(distancia - mouse.scroll.ReadValue().y * 0.005f, 1.2f, 7f);
        }
        ActualizarCamara(false);
    }

    public void EncuadrarAhora() => ActualizarCamara(true);

    void ActualizarCamara(bool inmediato)
    {
        if (!objetivo) return;
        Vector3 foco = objetivo.position + Vector3.up * alturaMirada;
        focoSuave = inmediato || !iniciado ? foco : Vector3.Lerp(focoSuave, foco, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        iniciado = true;
        Quaternion rotacion = Quaternion.Euler(inclinacion, giroHorizontal, 0f);
        Vector3 atras = rotacion * Vector3.back;
        float libre = distancia;
        // Acerca la cámara a la espalda si encuentra una pared.
        foreach (var hit in Physics.SphereCastAll(focoSuave, 0.15f, atras, distancia, obstaculos, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(objetivo) || hit.transform == objetivo || objetivo.IsChildOf(hit.transform)) continue;
            libre = Mathf.Min(libre, Mathf.Max(0.05f, hit.distance - 0.08f));
        }
        transform.SetPositionAndRotation(focoSuave + atras * libre, rotacion);
    }
}
