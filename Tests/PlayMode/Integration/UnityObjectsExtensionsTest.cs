using GameLovers.GameData;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.GameData.Tests.PlayMode.Integration
{
	[TestFixture]
	public class UnityObjectsExtensionsTest
	{
		private GameObject _canvasGameObject;
		private RectTransform _rectTransform;

		[SetUp]
		public void Setup()
		{
			_canvasGameObject = new GameObject("TestCanvas", typeof(Canvas));

			var rectGameObject = new GameObject("TestRect", typeof(RectTransform));

			_rectTransform = rectGameObject.GetComponent<RectTransform>();
			_rectTransform.SetParent(_canvasGameObject.transform, false);
			_rectTransform.anchorMin = Vector2.zero;
			_rectTransform.anchorMax = Vector2.zero;
			_rectTransform.pivot = new Vector2(0.5f, 0.5f);
			_rectTransform.sizeDelta = new Vector2(100f, 50f);
			_rectTransform.anchoredPosition = new Vector2(10f, 20f);
		}

		[TearDown]
		public void TearDown()
		{
			Object.Destroy(_canvasGameObject);
		}

		[Test]
		// ADMIT: UnityObjectsExtensions.GetWorldCornersArray must push GetLocalCornersArray's rect-space corners
		// through `transform.localToWorldMatrix`.
		// RCR: UnityObjectsExtensions.cs GetWorldCornersArray — change
		// `corners[i] = matrix4x.MultiplyPoint(corners[i]);` to `corners[i] = corners[i];` → RED (world corners
		// equal the local corners despite the 90-degree rotation). 2026-08-01
		public void GetWorldCornersArray_OnRotatedRectTransform_ReturnsCornersInWorldSpace()
		{
			_rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);

			var localCorners = _rectTransform.GetLocalCornersArray();
			var worldCorners = _rectTransform.GetWorldCornersArray();

			Assert.AreNotEqual(localCorners[0], worldCorners[0]);
			Assert.AreNotEqual(localCorners[2], worldCorners[2]);
		}
	}
}
