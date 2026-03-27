using _Main.Scripts.CarSystem;
using _Main.Scripts.CharacterSystem;
using _Main.Scripts.Datas;
using _Main.Scripts.GridSystem;
using _Main.Scripts.HolderSystem;
using _Main.Scripts.LineSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "ReferenceManagerSO", menuName = "Data/Reference ManagerSO")]
public class ReferenceManagerSO : ScriptableObject
{
	#region Singleton

	private static ReferenceManagerSO _instance;

	public static ReferenceManagerSO Instance
	{
		get
		{
			if (_instance == null)
				_instance = Resources.Load<ReferenceManagerSO>("ReferenceManagerSO");
			return _instance;
		}
	}

	#endregion

	[Header("Canvas")]
	[SerializeField] private GameObject hardLevelCanvas;
	[SerializeField] private GameObject extremeLevelCanvas;

	[Header("Grid")]
	[SerializeField] private GridCell gridCellPrefab;

	[Header("Person")]
	[SerializeField] private PersonController personPrefab;
	[SerializeField] private MaterialDataSO personMaterialData;

	[Header("Holder")]
	[SerializeField] private Holder holderPrefab;

	[Header("Line")]
	[SerializeField] private Line linePrefab;

	[Header("Car")]
	[SerializeField] private CarController carPrefab;

	public GameObject HardLevelCanvas => hardLevelCanvas;
	public GameObject ExtremeLevelCanvas => extremeLevelCanvas;

	public GridCell GridCellPrefab => gridCellPrefab;
	public PersonController PersonPrefab => personPrefab;

	public Holder HolderPrefab => holderPrefab;

	public MaterialDataSO PersonMaterialData => personMaterialData;

	public Line LinePrefab => linePrefab;
	public CarController CarPrefab => carPrefab;
}