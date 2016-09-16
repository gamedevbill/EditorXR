﻿using System.Collections.Generic;
using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class InspectorListViewController : NestedListViewController<InspectorData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_CubeMaterial;
	private Material m_ExpandArrowMaterial;
	private Material m_TextMaterial;
	private Material m_GearMaterial;

	private readonly List<Material> m_InstancedMaterials = new List<Material>(4);

	private readonly Dictionary<string, Vector3> m_TemplateSizes = new Dictionary<string, Vector3>();

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<InspectorListItem>();
		item.GetMaterials(out m_CubeMaterial);

		foreach (var template in m_TemplateDictionary)
			m_TemplateSizes[template.Key] = GetObjectSize(template.Value.prefab);

		m_InstancedMaterials.Add(m_CubeMaterial);

		foreach (var template in m_Templates)
		{
			var componentItem = template.GetComponent<InspectorComponentItem>();
			if (componentItem)
			{
				componentItem.GetMaterials(out m_TextMaterial, out m_ExpandArrowMaterial, out m_GearMaterial);
				m_InstancedMaterials.Add(m_TextMaterial);
				m_InstancedMaterials.Add(m_ExpandArrowMaterial);
				m_InstancedMaterials.Add(m_GearMaterial);
			}
		}
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_StartPosition = bounds.extents.y * Vector3.up;

		var parentMatrix = transform.worldToLocalMatrix;
		SetMaterialClip(m_CubeMaterial, parentMatrix);
		SetMaterialClip(m_ExpandArrowMaterial, parentMatrix);
		SetMaterialClip(m_TextMaterial, parentMatrix);
	}

	protected override void UpdateItems()
	{
		var totalOffset = 0f;
		UpdateRecursively(m_Data, ref totalOffset);
		// Snap back if list scrolled too far
		if (totalOffset > 0 && -scrollOffset >= totalOffset)
			m_ScrollReturn = totalOffset - m_ItemSize.z; // m_ItemSize will be equal to the size of the last visible item
	}

	private void UpdateRecursively(InspectorData[] data, ref float totalOffset, int depth = 0)
	{
		foreach (var item in data)
		{
			m_ItemSize = m_TemplateSizes[item.template];
			if (totalOffset + scrollOffset + m_ItemSize.y < 0)
				CleanUpBeginning(item);
			else if (totalOffset + scrollOffset > bounds.size.y)
				CleanUpEnd(item);
			else
				UpdateItemRecursive(item, totalOffset, depth);
			totalOffset += m_ItemSize.z;
			if (item.children != null)
			{
				if (item.expanded)
					UpdateRecursively(item.children, ref totalOffset, depth + 1);
				else
					RecycleChildren(item);
			}
		}
	}

	private void UpdateItemRecursive(InspectorData data, float offset, int depth)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (InspectorListItem)data.item;
		item.UpdateTransforms(bounds.size.x - kClipMargin, depth);

		UpdateItem(item.transform, offset);
	}

	private void UpdateItem(Transform t, float offset)
	{
		t.localPosition = m_StartPosition + (offset + m_ScrollOffset) * Vector3.down;
		t.localRotation = Quaternion.identity;
	}

	protected override ListViewItem<InspectorData> GetItem(InspectorData listData)
	{
		var item = (InspectorListItem)base.GetItem(listData);
		item.SwapMaterials(m_CubeMaterial);

		var componentItem = item as InspectorComponentItem;
		if (componentItem)
			componentItem.SwapMaterials(m_TextMaterial, m_ExpandArrowMaterial, m_GearMaterial);
		return item;
	}

	private void OnDestroy()
	{
		foreach (var material in m_InstancedMaterials)
			U.Object.Destroy(material);
	}
}