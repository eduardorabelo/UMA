﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UMA.Editors
{
	[CustomPropertyDrawer(typeof(DNAEvaluationGraph), true)]
	public class DNAEvaluationGraphPropertyDrawer : PropertyDrawer
	{

		private DNAEvaluationGraph.EditorHelper _helper = new DNAEvaluationGraph.EditorHelper();

		private DNAEvaluationGraph.EditorHelper _drawhelper = new DNAEvaluationGraph.EditorHelper();

		private bool initialized = false;

		private static GUIStyle graphLabelStyle;

		private float padding = 3f;

		private float popupIconSize = 20f;

		private static Color _bgColor = new Color(0.337f, 0.337f, 0.337f, 1f);

		private static Color _bgColorHovered = new Color(0.337f, 0.45f, 0.337f, 1f);

		private static Color _bgColorSelected = new Color(0.337f, 0.55f, 0.337f, 1f);

		private static DNAEvaluationGraphPopupContent _popupContent;

		//Could be used to make all swatches show a -1f -> +1f range- isn't right now
		private Rect rangesRect;

		private void Init()
		{
			if (initialized)
				return;
			if(graphLabelStyle == null)
			{
				graphLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
				graphLabelStyle.normal.textColor = Color.white;
				graphLabelStyle.active.textColor = Color.white;
				graphLabelStyle.hover.textColor = Color.white;
				graphLabelStyle.fontStyle = FontStyle.Bold;
			}
			initialized = true;
			rangesRect = new Rect();
			rangesRect.xMin = 0f;
			rangesRect.xMax = 1f;
			rangesRect.yMin = -1f;
			rangesRect.yMax = 1f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Init();

			label = EditorGUI.BeginProperty(position, label, property);

			CopyValuesToHelper(property, _helper);

			if ((_helper._graph == null || _helper._graph.keys.Length == 0) && string.IsNullOrEmpty(_helper._name))
			{
				AddDefaultValues();
			}

			GUI.SetNextControlName("DNAEvaluationGraph");

			var fieldRect = EditorGUI.PrefixLabel(position, label);

			var prevIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			DrawPopup(fieldRect, property);
			EditorGUI.indentLevel = prevIndent;

			CopyValuesFromHelper(property, _helper);

			EditorGUI.EndProperty();
		}

		private void DrawPopup(Rect position, SerializedProperty property)
		{
			padding = 0f;
			var popupIconRect = new Rect((position.xMax - popupIconSize) + 1f, position.yMin, popupIconSize - 2f, position.height + 1f);
			var contentRect = new Rect(position.xMin + 1f, position.yMin + 0.5f, position.width - popupIconSize +3f, position.height - 2f);
			var overlayRect = new Rect(position.xMin, position.yMin - 1f, position.width, position.height);

			//make this look like a popup that has this graph swatch in
			//draw a popup style background-this makes the field look highlighted (in blue)
			EditorGUI.LabelField(position, GUIContent.none, EditorStyles.miniButton);
			
			//draw a little popup icon aswell
			EditorGUI.LabelField(popupIconRect, GUIContent.none, EditorStyles.popup);
			
			//now draw the graph swatch in the center of that (there is an optional rangesRect param that we could add here if we always wanted to draw the swatch with a vertical axis going fro -1f -> 1f)
			EditorGUIUtility.DrawCurveSwatch(contentRect, _helper._graph, null, new Color(1f, 1f, 0f, 0.7f), new Color(0.337f, 0.337f, 0.337f, 1f));
			
			//now draw a button over the whole thing, with a label for the selected swatch, that will open up the swatch selector when its clicked
			EditorGUI.DropShadowLabel(overlayRect, new GUIContent(_helper._name), graphLabelStyle);
			if (GUI.Button(overlayRect, new GUIContent(_helper._name, DNAEvaluationGraphPresets.GetTooltipFor(_helper.Target)), graphLabelStyle))
			{
				GUI.FocusControl("DNAEvaluationGraph");

				//Deal with missing settings
				//If this field contains a graph that is no longer in any preset libraries
				//they wont be able to get that graph back unless they add it to a library
				//so show a warning dialog giving them the option of doing that
				if (_helper.Target != null && !DNAEvaluationGraphPresets.AllGraphPresets.Contains(_helper.Target))
				{
					var _missingGraphChoice = EditorUtility.DisplayDialogComplex("Missing Preset", "The graph " + _helper.Target.name + " was not in any preset libraries in the project. If you change the graph this field is using you wont be able to select " + _helper.Target.name + " again. What would you like to do?", "Change Anyway", "Store and Change", "Cancel");
					if (_missingGraphChoice == 1)
					{
						Debug.Log("_missingGraphChoice == 1");
						//store and change
						//add to the first found lib and then carry on
						DNAEvaluationGraphPresets.AddNewPreset(_helper.Target.name, new AnimationCurve(_helper.Target.GraphKeys), _helper.Target.name);
					}
					else if(_missingGraphChoice == 2)
					{

						return;
					}
				}

				if (_popupContent == null)
					_popupContent = new DNAEvaluationGraphPopupContent();
				_popupContent.width = overlayRect.width;
				_popupContent.selectedPreset = new DNAEvaluationGraph(_helper.Target);
				_popupContent.property = property;
				_popupContent.OnSelected = PopupCallback;

				PopupWindow.Show(overlayRect, _popupContent);
			}
		}

		private void PopupCallback(DNAEvaluationGraph selectedGraph, SerializedProperty property)
		{
			_helper = new DNAEvaluationGraph.EditorHelper(selectedGraph);
			CopyValuesFromHelper(property, _helper);
			property.serializedObject.ApplyModifiedProperties();
			GUI.changed = true;
		}


		/// <summary>
		/// Draws a swatch for the given DNAEvaluationGraph at the given position.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="dnaGraph"></param>
		/// <param name="tooltip">An optional tooltip for when the swatch is hovered</param>
		/// <param name="hovered">Draw the swatch as hovered</param>
		/// <param name="selected">Draw the swatch as selected</param>
		/// <param name="callback">An optional callback to trigger when the swatch is clicked</param>
		public void DrawSwatch(Rect position, DNAEvaluationGraph dnaGraph, string tooltip = "", bool hovered = false, bool selected = false, System.Action<DNAEvaluationGraph> callback = null)
		{
			Init();

			_drawhelper = new DNAEvaluationGraph.EditorHelper(dnaGraph);

			var thisBgColor = hovered ? _bgColorHovered : (selected ? _bgColorSelected : _bgColor);
			EditorGUIUtility.DrawCurveSwatch(position, _drawhelper._graph, null, new Color(1f, 1f, 0f, 0.7f), thisBgColor);
			//now draw a button over the whole thing, with a label for the selected swatch, that will open up the swatch selector when its clicked
			var overlayRect = new Rect(position.xMin, position.yMin - 1f, position.width, position.height);

			EditorGUI.DropShadowLabel(overlayRect, new GUIContent(dnaGraph.name), graphLabelStyle);

			if (GUI.Button(overlayRect, new GUIContent(dnaGraph.name, tooltip), graphLabelStyle))
			{
				if (callback != null)
					callback(dnaGraph);

			}
		}

		private void AddDefaultValues()
		{
			_helper = new DNAEvaluationGraph.EditorHelper(DNAEvaluationGraph.Default);
		}

		private void AddDefaultValues(SerializedProperty property)
		{
			_helper = new DNAEvaluationGraph.EditorHelper(DNAEvaluationGraph.Default);

			CopyValuesFromHelper(property, _helper);

			property.serializedObject.ApplyModifiedProperties();
			property.serializedObject.Update();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight /*+ (EditorGUIUtility.standardVerticalSpacing *2f)*/;
		}

		#region Static Utils

		private static void CopyValuesToHelper(SerializedProperty property, DNAEvaluationGraph.EditorHelper helper)
		{
			helper._name = property.FindPropertyRelative("_name").stringValue;
			helper._graph = property.FindPropertyRelative("_graph").animationCurveValue;
		}

		private static void CopyValuesFromHelper(SerializedProperty property, DNAEvaluationGraph.EditorHelper helper)
		{
			property.FindPropertyRelative("_name").stringValue = helper._name;
			property.FindPropertyRelative("_graph").animationCurveValue = helper._graph;
		}

		#endregion
	}

}