using UnityEngine;

public class ReproducirSonidos : MonoBehaviour
{
    public AudioSource reproductorAudio;

    public AudioClip sonidoReproducirAscensor;
    public AudioClip sonidoReproducirCaminar;
    public AudioClip sonidoReproducirCorrer;
    public AudioClip sonidoReproducirCueva;
    public AudioClip sonidoReproducirEngranajes;
    public AudioClip sonidoReproducirPulsarBoton;
    public AudioClip sonidoReproducirRecogerObjeto;
    public AudioClip sonidoReproducirSalto;
    public AudioClip sonidoReproducirSoltarObjeto;


    public void ReproducirSonidoAscensor()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirAscensor);
    }

    public void ReproducirSonidoCaminar()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirCaminar);
    }

    public void ReproducirSonidoCorrer()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirCorrer);
    }

    public void ReproducirSonidoCueva()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirCueva);
    }

    public void ReproducirSonidoEngranajes()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirEngranajes);
    }

    public void ReproducirSonidoPulsarBoton()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirPulsarBoton);
    }

    public void ReproducirSonidoRecogerObjeto()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirRecogerObjeto);
    }

    public void ReproducirSonidoSalto()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirSalto);
    }

    public void ReproducirSonidoSoltarObjeto()
    {
        reproductorAudio.PlayOneShot(sonidoReproducirSoltarObjeto);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
