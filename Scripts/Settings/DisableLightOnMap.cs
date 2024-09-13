using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableLightOnMap : MonoBehaviour
{

    [SerializeField] private Camera cam;
    [SerializeField] private Shader unlitShader;

    void Start()
    {
        if(unlitShader == null)  unlitShader = Shader.Find("Unlit/Texture");
        cam.SetReplacementShader(unlitShader, "RenderType");
        //GetComponent<Camera>().RenderWithShader(unlitShader, " ");


        //Camera.onPreCull += OnPreCullCallback;
        //Camera.onPreRender += OnPreRenderCallback;
        //Camera.onPostRender += OnPostRenderCallback;
    }

    //// Unity calls the methods in this delegate's invocation list before rendering any camera
    //void OnPreCullCallback(Camera cam)
    //{
    //    Debug.Log("Camera callback: Camera name is " + cam.name);

    //    // Unity calls this for every active Camera.
    //    // If you're only interested in a particular Camera,
    //    // check whether the Camera is the one you're interested in
    //    if (cam == GetComponent<Camera>())
    //    {
    //        // Put your custom code here
    //        GetComponent<Camera>().enabled = false;
    //    }
    //}

    //// Unity calls the methods in this delegate's invocation list before rendering any camera
    //void OnPreRenderCallback(Camera cam)
    //{
    //    Debug.Log("Camera callback: Camera name is " + cam.name);

    //    // Unity calls this for every active Camera.
    //    // If you're only interested in a particular Camera,
    //    // check whether the Camera is the one you're interested in
    //    if (cam == GetComponent<Camera>())
    //    {
    //        // Put your custom code here
    //        GetComponent<Camera>().enabled = false;
    //    }
    //}

    //// Unity calls the methods in this delegate's invocation list before rendering any camera
    //void OnPostRenderCallback(Camera cam)
    //{
    //    Debug.Log("Camera callback: Camera name is " + cam.name);

    //    // Unity calls this for every active Camera.
    //    // If you're only interested in a particular Camera,
    //    // check whether the Camera is the one you're interested in
    //    if (cam == GetComponent<Camera>())
    //    {
    //        // Put your custom code here
    //        GetComponent<Camera>().enabled = true;
    //    }
    //}

    //// Remove your callback from the delegate's invocation list
    //void OnDestroy()
    //{
    //    Camera.onPreCull -= OnPreCullCallback;
    //    Camera.onPreRender -= OnPreRenderCallback;
    //    Camera.onPostRender -= OnPostRenderCallback;
    //}




    //void OnPreCull()
    //{
    //    if (sunlight != null)
    //        sunlight.enabled = false;
    //}

    //void OnPreRender()
    //{
    //    if (sunlight != null)
    //        sunlight.enabled = false;
    //}

    //void OnPostRender()
    //{
    //    if (sunlight != null)
    //        sunlight.enabled = true;
    //}

}
