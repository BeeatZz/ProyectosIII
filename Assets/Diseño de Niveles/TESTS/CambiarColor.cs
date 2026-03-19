using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CambiarColor : MonoBehaviour
{
    //public GameObject cambio1;
    //public GameObject cambio2;
    
    //public OptionsMenu miOptionsMenu;
    public Material materialdefecto;
    public Material materialcambio;
    //private Color colorInicioCambio1;
    //private Color colorInicioCambio2;
    private Color colorInicioBoton;
    private Color colorInicioBotonFondo;
    private Color colorInicioSlider;

    private bool ColorInicio = false;
    private bool ColorBotonInicio = false;
    private bool FuenteInicio = false;
    //
    private Color Blanco = Color.white;
    private Color Negro = Color.black;

    public Button botonColorBlind;
    public Button botonContraste;
    public Slider slider;
    public Slider Masterslider;
    public Slider FBXslider;
    public Slider Ambienceslider;
    public Slider Musicslider;
    public TMP_Dropdown dropdown;

    public TMP_FontAsset nuevaFuente;
    private TMP_FontAsset antiguaFuente;


    private void Start()
    {
        // guardar colores por defecto
        /*
        colorInicioCambio1 = cambio1.GetComponent<MeshRenderer>().material.color;
        colorInicioCambio2 = cambio2.GetComponent<MeshRenderer>().material.color;
        */
        
        //colorInicioslider = slider.fillRect.GetComponent<Image>().color;
        colorInicioBoton = botonColorBlind.GetComponentInChildren<TextMeshProUGUI>().color;
        colorInicioBotonFondo = botonColorBlind.image.color;

        //guardar fuente por defecto
        antiguaFuente = botonColorBlind.GetComponentInChildren<TextMeshProUGUI>().font;

    }

    /*
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
    */

    public void cambiarColorBoton()
    {
        Image fillImage = slider.fillRect.GetComponent<Image>();

        Image MastersliderImage = Masterslider.fillRect.GetComponent<Image>();
        Image FBXsliderImage = FBXslider.fillRect.GetComponent<Image>();
        Image AmbiencesliderImage = Ambienceslider.fillRect.GetComponent<Image>();
        Image MusicsliderImage = Musicslider.fillRect.GetComponent<Image>();
        if(ColorBotonInicio == false)
        {
            botonColorBlind.image.color = Negro;
            botonColorBlind.GetComponentInChildren<TextMeshProUGUI>().color = Blanco;
            //
            botonContraste.image.color = Negro;
            botonContraste.GetComponentInChildren<TextMeshProUGUI>().color = Blanco;
            //
            fillImage.color = Negro;
            MastersliderImage.color = Negro;
            FBXsliderImage.color = Negro;
            AmbiencesliderImage.color = Negro;
            MusicsliderImage.color = Negro;
            //
            //miOptionsMenu.caambiarColores(Blanco, Negro);
            Image bg = dropdown.GetComponent<Image>();
            dropdown.captionText.color = Blanco;
            dropdown.image.color = Negro;
            dropdown.itemText.color = Blanco;
            Transform list = dropdown.transform.root.Find("Dropdown List");

            if(list != null)
            {
                Image fondo = list.GetComponent<Image>();
                fondo.color = Negro;
            }

            // itembg.color = Negro;

            ColorBotonInicio = true;
        }

        else
        {
            botonColorBlind.image.color = Blanco;
            botonColorBlind.GetComponentInChildren<TextMeshProUGUI>().color = Negro;
            //
            botonContraste.image.color = Blanco;
            botonContraste.GetComponentInChildren<TextMeshProUGUI>().color = Negro;

            fillImage.color = colorInicioSlider;
            MastersliderImage.color = colorInicioSlider;
            FBXsliderImage.color = colorInicioSlider;
            AmbiencesliderImage.color = colorInicioSlider;

            //miOptionsMenu.cambiarColores(Negro, Blanco);

            dropdown.captionText.color = Negro;
            dropdown.image.color = Blanco;
            dropdown.itemText.color = Negro;

            ColorBotonInicio = false;
        }

    }

    public void cambiarFuente()
    {
        if (FuenteInicio == false)
        {
            botonColorBlind.GetComponentInChildren<TextMeshProUGUI>().font = nuevaFuente;
            FuenteInicio = true;
        }
        else
        {
            botonColorBlind.GetComponentInChildren<TextMeshProUGUI>().font = antiguaFuente;
            FuenteInicio = false;
        }

    }
}
