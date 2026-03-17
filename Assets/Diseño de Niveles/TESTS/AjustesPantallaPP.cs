using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class AjustesPantallaPP : MonoBehaviour
{

    public Volume volumen;
     ColorAdjustments colorAdjustments;

    private bool activado = false;
  

    void Start()
    {
        
        volumen.profile.TryGet<ColorAdjustments>(out colorAdjustments);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void CambioAjustesColor()
    {
        if(activado == false)
        {
            colorAdjustments.contrast.value = -100f;
            activado = true;
        }
        
        else
        {
            colorAdjustments.contrast.value = 100f;
            activado = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
