﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UMA;
using UnityEditorInternal;

namespace UMA.Editors
{
	[CustomPropertyDrawer(typeof(DNAEvaluator), true)]
	public class DNAEvaluatorPropertyDrawer : PropertyDrawer
	{
		private const string DNANAMEPROPERTY = "_dnaName";
		private const string EVALUATORPROPERTY = "_evaluator";
		private const string MULTIPLIERPROPERTY = "_multiplier";

		private const string DNANAMELABEL = "DNA Name";
		private const string EVALUATORLABEL = "Evaluator";
		private const string MULTIPLIERLABEL = "Multiplier";

		private DNAEvaluator _target;

		private bool _drawInline = true;
		private bool _drawLabels = true;
		private bool _alwaysExpanded = false;
		private float _multiplierLabelWidth = 55f;
		private Vector2 _dnaToEvaluatorRatio = new Vector2(2f, 3f);
		private float _padding = 2f;
		private bool _manuallyConfigured = false;

		//if this is drawn in a DynamicDNAPlugin it should give us dna names to choose from
		private DynamicDNAPlugin _dynamicDNAPlugin;

		public bool DrawInline
		{
			set
			{
				_drawInline = value;
				_manuallyConfigured = true;
			}
		}

		public bool DrawLabels
		{
			set
			{
				_drawLabels = value;
				_manuallyConfigured = true;
			}
		}

		public bool AlwaysExpanded
		{
			set
			{
				_alwaysExpanded = value;
				_manuallyConfigured = true;
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Event current = Event.current;
			label = EditorGUI.BeginProperty(position, label, property);

			//Try and get a DNAAsset from the serializedObject
			CheckDynamicDNAPlugin(property);

			if (!_manuallyConfigured)
			{
				if (this.fieldInfo != null)
				{
					var attrib = this.fieldInfo.GetCustomAttributes(typeof(DNAEvaluator.ConfigAttribute), true).FirstOrDefault() as DNAEvaluator.ConfigAttribute;
					if (attrib != null)
					{
						_drawInline = attrib.drawInline;
						_drawLabels = attrib.drawLabels;
						_alwaysExpanded = attrib.alwaysExpanded;
					}
				}
			}

			if (_drawInline)
			{
				if (!_alwaysExpanded)
				{
					var foldoutPos = new Rect(position.xMin, position.yMin, position.width, EditorGUIUtility.singleLineHeight);
					property.isExpanded = EditorGUI.Foldout(foldoutPos, property.isExpanded, label);
				}
				var reorderableListDefaults = new ReorderableList.Defaults();
				if (property.isExpanded || _alwaysExpanded)
				{
					EditorGUI.indentLevel++;
					position = EditorGUI.IndentedRect(position);
					if (!_alwaysExpanded)
						position.yMin = position.yMin + EditorGUIUtility.singleLineHeight;
					else
						position.yMin += 2f;
					position.xMin -= 15f;//make it the same width as a reorderable list
					if (_drawLabels)
					{
						//can we draw this so it looks like the header of a reorderable List?
						if(current.type == EventType.Repaint)
							reorderableListDefaults.headerBackground.Draw(position, GUIContent.none, false, false, false, false);
						var rect1 = new Rect(position.xMin + 6f, position.yMin + 1f, position.width - 12f, position.height);
						position = DoLabelsInline(rect1,(_alwaysExpanded ? label.text : DNANAMELABEL));
						position.xMin -= 6f;
						position.width += 6f;
						position.yMin -= 1f;
						position.height -= 3f;
					}
					if (current.type == EventType.Repaint)
						reorderableListDefaults.boxBackground.Draw(position, GUIContent.none, false, false, false, false);
					var rect2 = new Rect(position.xMin + 6f, position.yMin + 3f, position.width - 12f, position.height);
					DoFieldsInline(rect2, property);
					EditorGUI.indentLevel--;
				}
			}
			else
			{
				EditorGUI.PropertyField(position, property, label, true);
			}

			EditorGUI.EndProperty();
		}

		public Rect DoLabelsInline(Rect position, string label1 = DNANAMELABEL, string label2 = EVALUATORLABEL, string label3 = MULTIPLIERLABEL)
		{
			var prevIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			var fieldbaseRatio = (position.width - _multiplierLabelWidth) / (_dnaToEvaluatorRatio.x + _dnaToEvaluatorRatio.y);
			var dnafieldWidth = fieldbaseRatio * _dnaToEvaluatorRatio.x;
			var evaluatorFieldWidth = fieldbaseRatio * _dnaToEvaluatorRatio.y;

			var dnaNameLabelRect = new Rect(position.xMin, position.yMin, dnafieldWidth, EditorGUIUtility.singleLineHeight);
			var evaluatorLabelRect = new Rect(dnaNameLabelRect.xMax, position.yMin, evaluatorFieldWidth, EditorGUIUtility.singleLineHeight);
			var intensityLabelRect = new Rect(evaluatorLabelRect.xMax, position.yMin, _multiplierLabelWidth, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(dnaNameLabelRect, new GUIContent(label1, GetChildTooltip(DNANAMEPROPERTY)), EditorStyles.centeredGreyMiniLabel);
			EditorGUI.LabelField(evaluatorLabelRect, new GUIContent(label2, GetChildTooltip(EVALUATORPROPERTY)), EditorStyles.centeredGreyMiniLabel);
			EditorGUI.LabelField(intensityLabelRect, new GUIContent(label3, GetChildTooltip(MULTIPLIERPROPERTY)), EditorStyles.centeredGreyMiniLabel);
			position.yMin = dnaNameLabelRect.yMax + 2f;

			EditorGUI.indentLevel = prevIndent;

			return position;
		}

		/// <summary>
		/// Draws a DNAEvaluator with inline styling
		/// </summary>
		/// <param name="position"></param>
		/// <param name="property"></param>
		/// <param name="label"></param>
		public void DoFieldsInline(Rect position, SerializedProperty property)
		{
			CheckDynamicDNAPlugin(property);

			var prevIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			var dnaNameProp = property.FindPropertyRelative(DNANAMEPROPERTY);
			var evaluatorProp = property.FindPropertyRelative(EVALUATORPROPERTY);
			var intensityProp = property.FindPropertyRelative(MULTIPLIERPROPERTY);
			var fieldbaseRatio = (position.width - _multiplierLabelWidth) / (_dnaToEvaluatorRatio.x + _dnaToEvaluatorRatio.y);
			var dnafieldWidth = fieldbaseRatio * _dnaToEvaluatorRatio.x;
			var evaluatorFieldWidth = fieldbaseRatio * _dnaToEvaluatorRatio.y;

			position.height = EditorGUIUtility.singleLineHeight;//theres a space at the bottom so cut that off

			var dnaNameRect = new Rect(position.xMin + _padding, position.yMin, dnafieldWidth - (_padding * 2), position.height);
			var evaluatorRect = new Rect(dnaNameRect.xMax + (_padding * 2), position.yMin, evaluatorFieldWidth - (_padding * 2), position.height);
			var intensityRect = new Rect(evaluatorRect.xMax + (_padding * 2), position.yMin, _multiplierLabelWidth - (_padding * 2), position.height);
			if(_dynamicDNAPlugin == null)
				EditorGUI.PropertyField(dnaNameRect, dnaNameProp, GUIContent.none);
			else
			{
				_dynamicDNAPlugin.converterAsset.DNANamesPopup(dnaNameRect, dnaNameProp, dnaNameProp.stringValue);
			}
			EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(dnaNameProp.stringValue));
			EditorGUI.PropertyField(evaluatorRect, evaluatorProp, GUIContent.none);
			EditorGUI.PropertyField(intensityRect, intensityProp, GUIContent.none);
			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel = prevIndent;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!_manuallyConfigured)
			{
				if (this.fieldInfo != null)
				{
					var attrib = this.fieldInfo.GetCustomAttributes(typeof(DNAEvaluator.ConfigAttribute), true).FirstOrDefault() as DNAEvaluator.ConfigAttribute;
					if (attrib != null)
					{
						_drawInline = attrib.drawInline;
						_drawLabels = attrib.drawLabels;
						_alwaysExpanded = attrib.alwaysExpanded;
					}

				}
			}
			if (!_drawInline)
			{
				return EditorGUI.GetPropertyHeight(property, true);
			}
			else
			{
				if (property.isExpanded || _alwaysExpanded)
				{
					if (_drawLabels)
						return EditorGUIUtility.singleLineHeight * (_alwaysExpanded ? 2f : 3f) + (_padding * 4f) + 6f;
					else
						return EditorGUIUtility.singleLineHeight * (_alwaysExpanded ? 1f : 2f) + (_padding * 4f);
				}
				else
					return EditorGUI.GetPropertyHeight(property, true);
			}
		}

		/// <summary>
		/// if this property is in a DynamicDNAPlugin this will find it so we can use its DNAAsset etc
		/// </summary>
		/// <param name="property"></param>
		private void CheckDynamicDNAPlugin(SerializedProperty property)
		{
			//property.serializedObject.targetObject is the plugin and this has the dnaAsset assigned to it by the behaviour
			if (typeof(DynamicDNAPlugin).IsAssignableFrom((property.serializedObject.targetObject).GetType()))
			{
				_dynamicDNAPlugin = (DynamicDNAPlugin)property.serializedObject.targetObject;
			}
		}

		/// <summary>
		/// Gets the tooltip from the 'Tooltip' attribute defined in the type class (if set)
		/// </summary>
		private string GetChildTooltip(SerializedProperty property)
		{

			return GetChildTooltip(property.name);
		}
		/// <summary>
		/// Gets the tooltip from the 'Tooltip' attribute defined in the type class (if set)
		/// </summary>
		private string GetChildTooltip(string propertyName)
		{

			TooltipAttribute[] attributes = typeof(DNAEvaluator).GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetCustomAttributes(typeof(TooltipAttribute), true) as TooltipAttribute[];

			return attributes.Length > 0 ? attributes[0].tooltip : "";
		}

	}
}
