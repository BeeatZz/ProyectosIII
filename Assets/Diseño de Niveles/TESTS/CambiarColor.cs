using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CambiarColor : MonoBehaviour
{
    public GameObject cambio1;
    public GameObject cambio2;
    public Material materialdefecto;
    public Material materialcambio;
    private Color colorInicioCambio1;
    private Color colorInicioCambio2;
    private Color colorInicioBoton;
    private Color colorInicioBotonFondo;
    private bool ColorInicio = false;
    private bool FuenteInicio = false;

    public Button botonTexto;
    public TMP_FontAsset nuevaFuente;
    private TMP_FontAsset antiguaFuente;


    private void Start()
    {
        // guardar colores por defecto
        colorInicioCambio1 = cambio1.GetComponent<MeshRenderer>().material.color;
        colorInicioCambio2 = cambio2.GetComponent<MeshRenderer>().material.color;
        colorInicioBoton = botonTexto.GetComponentInChildren<TextMeshProUGUI>().color;
        colorInicioBotonFondo = botonTexto.image.color;

        //guardar fuente por defecto
        antiguaFuente = botonTexto.GetComponentInChildren<TextMeshProUGUI>().font;

    }


    public void cambiarColor()
    {
        if(ColorInicio == false)
        {
            cambio1.GetComponent<MeshRenderer>().material.color = materialdefecto.color;
            cambio2.GetComponent<MeshRenderer>().material.color = materialcambio.color;
            botonTexto.image.color = materialcambio.color;
            botonTexto.GetComponentInChildren<TextMeshProUGUI>().color = Color.green;
            ColorInicio = true;
        }
        else
        {
            cambio1.GetComponent<MeshRenderer>().material.color = colorInicioCambio1;
            cambio2.GetComponent<MeshRenderer>().material.color = colorInicioCambio2;
            botonTexto.image.color = colorInicioBotonFondo;
            botonTexto.GetComponentInChildren<TextMeshProUGUI>().color = colorInicioBoton;
            ColorInicio = false;
        }

    }

    public void cambiarFuente()
    {
        if (FuenteInicio == false)
        {
            botonTexto.GetComponentInChildren<TextMeshProUGUI>().font = nuevaFuente;
            FuenteInicio = true;
        }
        else
        {
            botonTexto.GetComponentInChildren<TextMeshProUGUI>().font = antiguaFuente;
            FuenteInicio = false;
        }

    }
}
