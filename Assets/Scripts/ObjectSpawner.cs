using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class ObjectSpawner : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _uiParent;
    [SerializeField] private TMP_Text _countdown;
    [SerializeField] private TMP_Dropdown _perspective;
    [SerializeField] private TMP_InputField _numberOfObjectsField;
    [SerializeField] private TMP_InputField _distanceOfObjectsField;
    [SerializeField] private TMP_Dropdown _attractorShapeDropdown;
    [SerializeField] private TMP_Dropdown _attractorColourDropdown;
    [SerializeField] private TMP_Dropdown _distractorShapeDropdown;
    [SerializeField] private Toggle _randomizeDistractorShapeToggle;
    [SerializeField] private TMP_Dropdown _distractorColourDropdown;
    [SerializeField] private Toggle _randomizeDistractorColourToggle;
    [SerializeField] private TMP_InputField _timeInMSField;
    [SerializeField] private TMP_Text _errorMessage;
    
    [Header("Spawner")]
    [SerializeField] private Transform _parent;
    [SerializeField] private Material _material;
    [SerializeField] private Camera _camera;
    [SerializeField] private Vector3 _camera2DPos = new(0, 30, 0);
    [SerializeField] private Vector3 _camera2DRot = new(90, 0, 0);
    [SerializeField] private Vector3 _camera3DPos = new(0, 30, -30);
    [SerializeField] private Vector3 _camera3DRot = new(40, 0, 0);
    
    private int _numberOfObjects = 50;
    private float _spawnDistance = 2.0f;
    private PrimitiveType _attractorShape = PrimitiveType.Sphere;
    private Color _attractorColour = Color.white;
    private PrimitiveType _distractorShape = PrimitiveType.Cube;
    private bool _randomizeDistractorShape = false;
    private Color _distractorColour = Color.white;
    private bool _randomizeDistractorColour = false;
    private int _shownTime;
    
    private GameObject[] _objects;

    private readonly List<string> _shapes = new() { "Sphere", "Cube", "Cylinder", "Capsule" };
    private readonly List<string> _colours = new() { "white", "green", "blue", "red", "black", "yellow", "cyan", "magenta", "grey" };

    private void Start()
    {
        _attractorShapeDropdown.AddOptions(_shapes);
        _attractorColourDropdown.AddOptions(_colours);
        _distractorShapeDropdown.AddOptions(_shapes);
        _distractorColourDropdown.AddOptions(_colours);
    }

    public async void StartTest()
    {
        CleanupObjects();
        _errorMessage.gameObject.SetActive(false);
        
        try
        {
            _numberOfObjects = int.Parse(_numberOfObjectsField.text);
            _spawnDistance = float.Parse(_distanceOfObjectsField.text);
            _attractorShape = Enum.Parse<PrimitiveType>(_attractorShapeDropdown.options[_attractorShapeDropdown.value].text);
            ColorUtility.TryParseHtmlString(_attractorColourDropdown.options[_attractorColourDropdown.value].text,
                out var attCol);
            _attractorColour = attCol;
            _distractorShape = Enum.Parse<PrimitiveType>(_distractorShapeDropdown.options[_distractorShapeDropdown.value].text);
            _randomizeDistractorShape = _randomizeDistractorShapeToggle.isOn;
            ColorUtility.TryParseHtmlString(_distractorColourDropdown.options[_distractorColourDropdown.value].text,
                out var disCol);
            _distractorColour = disCol;
            _randomizeDistractorColour = _randomizeDistractorColourToggle.isOn;
            _shownTime = int.Parse(_timeInMSField.text);
        }
        catch (Exception)
        {
            _errorMessage.gameObject.SetActive(true);
            return;
        }
        
        if (_perspective.options[_perspective.value].text.Equals("2D"))
            _camera.transform.SetPositionAndRotation(_camera2DPos, Quaternion.Euler(_camera2DRot));
        else
            _camera.transform.SetPositionAndRotation(_camera3DPos, Quaternion.Euler(_camera3DRot));

        SpawnObjects();
        
        _uiParent.gameObject.SetActive(false);
        _countdown.gameObject.SetActive(true);
        _countdown.text = "3";
        await Task.Run(async delegate { await Task.Delay(500); });
        _countdown.text = "2";
        await Task.Run(async delegate { await Task.Delay(500); });
        _countdown.text = "1";
        await Task.Run(async delegate { await Task.Delay(500); });
        _countdown.gameObject.SetActive(false);
        _canvas.gameObject.SetActive(false);
        await Task.Run(async delegate { await Task.Delay(_shownTime); });
        _canvas.gameObject.SetActive(true);
        _uiParent.gameObject.SetActive(true);
    }

    public void ExitTest()
    {
        Application.Quit();
    }

    public void ShowMainMenu(bool showMainMenu)
    {
        _canvas.gameObject.SetActive(showMainMenu);
    }

    private void SpawnObjects()
    {
        _objects = new GameObject[_numberOfObjects];
        var numberOfColumns = (int)Math.Ceiling(Mathf.Sqrt(_numberOfObjects));
        var numberOfRows = (int)Math.Ceiling((float)_numberOfObjects / numberOfColumns);
        var remainder = _numberOfObjects % numberOfRows;
        var startX = -((float)(numberOfColumns - 1) / 2 * _spawnDistance);
        var startZ = -((float)(numberOfRows    - 1) / 2 * _spawnDistance);

        var attractorIndex = new Random().Next(_numberOfObjects - 1);

        var index = 0;
        for (var i = 0; i < numberOfColumns; i++)
        {
            for (var j = 0; j < numberOfRows; j++)
            {
                if (remainder > 0 && i == numberOfColumns - 1 && j >= remainder)
                    return;

                var isAttractor = index == attractorIndex;
                var x = startX + i * _spawnDistance;
                var z = startZ + j * _spawnDistance;
                var position = new Vector3(x, 0.5f, z);
                
                if (isAttractor)
                {
                    var obj = GameObject.CreatePrimitive(_attractorShape);
                    obj.transform.parent = _parent;
                    obj.transform.position = position;
                    var matInstance = obj.GetComponent<MeshRenderer>().sharedMaterial = Instantiate(_material);
                    matInstance.color = _attractorColour;
                    _objects[index++] = obj;
                }
                else
                {
                    switch (_randomizeDistractorShape)
                    {
                        case true when !_randomizeDistractorColour && _distractorColourDropdown.value == _attractorColourDropdown.value:
                        {
                            var shape = GetRandomString(
                                _shapes.Where((_, pos) => pos != _attractorShapeDropdown.value).ToList());
                            _distractorShape = Enum.Parse<PrimitiveType>(shape);
                            break;
                        }
                        case true:
                            _distractorShape = Enum.Parse<PrimitiveType>(GetRandomString(_shapes));
                            break;
                    }
                    switch (_randomizeDistractorColour)
                    {
                        case true when _distractorShape == _attractorShape:
                        {
                            var colour = GetRandomString(
                                _colours.Where((_, pos) => pos != _attractorColourDropdown.value).ToList());
                            ColorUtility.TryParseHtmlString(colour, out _distractorColour);
                            break;
                        }
                        case true:
                        {
                            var colour = GetRandomString(_colours);
                            ColorUtility.TryParseHtmlString(colour, out _distractorColour);
                            break;
                        }
                    }

                    var obj = GameObject.CreatePrimitive(_distractorShape);
                    obj.transform.parent = _parent;
                    obj.transform.position = position;
                    var matInstance = obj.GetComponent<MeshRenderer>().sharedMaterial = Instantiate(_material);
                    matInstance.color = _distractorColour;
                    _objects[index++] = obj;
                }
            }
        }
    }

    private void CleanupObjects()
    {
        if (_objects == null) return;
        foreach (var t in _objects)
            Destroy(t);
    }

    private static string GetRandomString(List<string> strings)
    {
        return strings[new Random().Next(strings.Count - 1)];
    }
}
