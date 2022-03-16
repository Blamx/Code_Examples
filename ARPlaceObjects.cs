using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

public class ARPlaceObjects : Singleton<ARPlaceObjects>
{

    [SerializeField]
    private ARSessionOrigin arOrigin;

    [SerializeField]
    private ARRaycastManager arRayManager;

    [SerializeField]
    private Pose poseObject;

    public bool placementIsValid { get; private set; } = false;

    [SerializeField]
    public GameObject prefab { get; private set; }

    [SerializeField]
    GameObject previewIcon;

    [SerializeField]
    GameObject InfoBox;

    //list of hit objects
    List<ARRaycastHit> hits;

    [SerializeField]
    bool MultiHighlight;

    private bool overObj = false;

    public float scale = 1;

    public Vector2 scaleMinMax;

    private Vector3 oldFirstTouchPosition = new Vector3(), oldSecondTouchPosition = new Vector3();

    float buffer = 0.0f, timer = 0;

    private bool initialTouch = false;

    private bool touchValid;

    public GameObject CurrentObject { get; private set; }

    public SceneObject GetSceneObject { get; private set; }

    public ARRaycastHit CurrentPlane { get; private set; }

    public bool metric = true;

    public bool viewMode = true;

    private void Update()
    {


        if (ARManager.Instance.scanning)
        {
            viewMode = false;
        }

        ////highlights planes
        if (!viewMode)
        {
            HighlightPlane();
        }
        else
        {
            ARManager.Instance.hidePlanes();
        }



        //updates ray and hitList
        updateRay();

        //UpdatePreview
        updatePreviewIcon();

        //UpdatePreview
        updateInfoBox();

        updateScale();

        //touchCheck
        {
            updateRotation();

            if (GestureDetection.touchCount != 1)
            {
                overObj = false;
                initialTouch = true;
                viewMode = true;
                timer = 0;
            }

            //return if no actions
            if (GestureDetection.touchCount <= 0)
                return;

            timer += Time.deltaTime;

            //return if under buffer
            if (buffer > timer)
                return;

            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                touchValid = true;
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                touchValid = false;
                return;
            }

        }

        //avoid UI
        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return;

        checkForObjects();

        //move current object if exists
        if (overObj && CurrentObject != null)
        {

            viewMode = false;

            List<ARRaycastHit> NewHits = new List<ARRaycastHit>();

            arRayManager.Raycast(Camera.current.ViewportToScreenPoint(Camera.current.ScreenToViewportPoint(Input.GetTouch(0).position)), NewHits, TrackableType.Planes);

            if (placementIsValid)
            {
                CurrentObject.transform.position = NewHits[0].pose.position;
            }
        }
        else if (CurrentObject == null)
        {

            //place UI
            if (initialTouch)
            {
                PlaceObject();
            }

            initialTouch = false;
        }

    }

    #region Internal Functions

    #region Update Functions

    //updates the ray for placing and moveing
    private void updateRay()
    {
        hits = new List<ARRaycastHit>();
        arRayManager.Raycast(Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f)), hits, TrackableType.Planes);

        placementIsValid = hits.Count > 0;
    }

    private void updateScale()
    {
        UIManager.Instance.SetScaleUI(scale);

        if (CurrentObject != null)
            CurrentObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void updatePreviewIcon()
    {
        if (placementIsValid)
        {
            previewIcon.SetActive(true);
            previewIcon.transform.position = hits[0].pose.position;
            previewIcon.transform.rotation = hits[0].pose.rotation;
        }
        else
        {
            previewIcon.SetActive(false);
        }
    }

    private void updateInfoBox()
    {
        //InfoBox.transform.position = previewIcon.transform.position;

        //transform.LookAt(Camera.current.transform);

        //transform.position += transform.right * 10;
    }

    private void updateRotation()
    {
        //Check for Rotation
        if (Input.touchCount == 2)
        {

            //avoid UI
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(1).fingerId))
                return;


            if (oldFirstTouchPosition == Vector3.zero)
            {
                oldFirstTouchPosition = Input.GetTouch(0).position;
                oldSecondTouchPosition = Input.GetTouch(1).position;
            }
            else
            {
                Vector3 first = Input.GetTouch(0).position;
                Vector3 second = Input.GetTouch(1).position;

                float angle = Vector2.Angle(first - second, oldFirstTouchPosition - oldSecondTouchPosition);


                if (Vector2.Dot(new Vector2(Vector3.Normalize(first - second).y, -Vector3.Normalize(first - second).x), Vector3.Normalize(oldFirstTouchPosition - oldSecondTouchPosition)) > 0.0)
                {
                    rotateObject(-angle * 5f);
                }
                else
                {
                    rotateObject(angle * 5f);
                }

                float scaleDiff = (Vector3.Distance(first, second) - Vector3.Distance(oldFirstTouchPosition, oldSecondTouchPosition)) * 0.0005f * 0.5f;

                if (scale + scaleDiff >= scaleMinMax.y)
                {
                    scale = scaleMinMax.y;
                }
                else if (scale + scaleDiff <= scaleMinMax.x)
                {
                    scale = scaleMinMax.x;
                }
                else
                {
                    scale += scaleDiff;
                    UIManager.Instance.SetScaleUI(scale);
                    UIManager.Instance.SetScaleSlider(scale);
                }


                oldFirstTouchPosition = Input.GetTouch(0).position;
                oldSecondTouchPosition = Input.GetTouch(1).position;
            }
        }
        else
        {
            oldFirstTouchPosition = Vector3.zero;
            oldSecondTouchPosition = Vector3.zero;
        }


    }

    #endregion


    //draws current plane or all if multi is true
    private void HighlightPlane()
    {
        if (placementIsValid)
        {

            if (MultiHighlight)
                ARManager.Instance.highlightAll();
            else
            {
                if (ARManager.Instance.scanning)
                {
                    ARManager.Instance.highlightPlane(hits[0].trackableId);
                    ARManager.Instance.currentPlane = hits[0].trackableId;
                }
            }
        }

    }

    //checks to see if you pressed object
    //sets current object to one you pressed
    private void checkForObjects()
    {
        if (GestureDetection.touchCount != 1)
            return;

        RaycastHit rayHit;
        if (Physics.Raycast(Camera.current.ScreenPointToRay(Input.GetTouch(0).position), out rayHit))
        {
            if (interactableCheck(rayHit.transform.gameObject))
            {
                overObj = true;
            }
        }
    }

    //checks if it is interactable or a child of an interactable object
    private bool interactableCheck(GameObject t)
    {
        if (t.tag != "Interactable")
        {
            if (t.transform.parent != null)
            {
                return interactableCheck(t.transform.parent.gameObject);
            }
        }
        else
        {
            return true;
        }

        return false;
    }


    private void rotateObject(float r)
    {
        CurrentObject.transform.localEulerAngles += new Vector3(0, r, 0);
    }

    //sets object to where ray hits
    public void PlaceObject()
    {
        hits = new List<ARRaycastHit>();
        arRayManager.Raycast(Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f)), hits, TrackableType.Planes);

        placementIsValid = hits.Count > 0;

        if (placementIsValid)
        {
            if (CurrentObject != null)
            {
                DestroyImmediate(CurrentObject);
            }

            CurrentObject = GameObject.Instantiate(prefab, previewIcon.transform.position, previewIcon.transform.rotation);
            CurrentObject.transform.eulerAngles += new Vector3(0,-90,0);
           GetSceneObject = CurrentObject.GetComponent<SceneObject>();

            GetSceneObject.metric = metric;

            UIManager.Instance.setMetricUI();
        }
    }

    #endregion

    #region External Functions

    public void ghostCurrentObject(string _tag)
    {
        if (CurrentObject != null)
        {
            CurrentObject.GetComponent<SceneObject>().GhostAllBut(_tag);
            hideDimensions();
        }
    }

    public void showDimensions()
    {
        if (CurrentObject != null)
        {
            CurrentObject.GetComponent<SceneObject>().toggleDimensions(true);
        }
    }

    public void hideDimensions()
    {
        if (CurrentObject != null)
        {
            CurrentObject.GetComponent<SceneObject>().toggleDimensions(false);
        }
    }

    public void toggleDimensions()
    {
        if (CurrentObject != null)
        {
            CurrentObject.GetComponent<SceneObject>().toggleDimensions();

            if (CurrentObject.GetComponent<SceneObject>().dimensionUI.activeSelf)
                ClearGhost();
        }
    }

    public void ClearGhost()
    {
        if (CurrentObject != null)
        {
            CurrentObject.GetComponent<SceneObject>().ClearGhost();
        }
    }


    public void toggleMetric()
    {
        metric = !metric;
        if (GetSceneObject != null)
        {
            GetSceneObject.metric = metric;
        }
    }
    #endregion

    #region Button Functions

    //enable/disables all planes
    public void setMulti(bool state)
    {
        MultiHighlight = state;
    }

    //enable/disables all planes
    public bool ToggleMulti()
    {
        return MultiHighlight = !MultiHighlight;
    }

    public void clear()
    {
        if (CurrentObject != null)
        {
            Destroy(CurrentObject);
        }
    }

    public void ChangePrefab(GameObject obj)
    {
        prefab = obj;


    }

    #endregion










}
