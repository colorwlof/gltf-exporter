﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityGLTF;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text.RegularExpressions;
using System;

namespace Plattar {
	public class PlattarExporter : EditorWindow {
		
		AnimBool showSettings;
		AnimBool showExportSettings;
		GameObject selectedObject;
		static Texture logo;
		
		[MenuItem("Plattar/GLTF Exporter")]
		static void Init() {
			logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Plattar/Editor/ExporterHeader.png", typeof(Texture2D));
		
			PlattarExporter window = (PlattarExporter)EditorWindow.GetWindow(typeof(PlattarExporter), false, "Plattar Exporter");
			window.minSize = new Vector2(350, 340);
			window.Show();
		}
		
		void OnEnable() {
			showSettings = new AnimBool(false);
			showSettings.valueChanged.AddListener(Repaint);
			
			showExportSettings = new AnimBool(false);
			showExportSettings.valueChanged.AddListener(Repaint);
		}
		
		void OnGUI() {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(logo);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			showSettings.target = EditorGUILayout.ToggleLeft("Exporter Options", showSettings.target);
			
			//Extra block that can be toggled on and off.
			if (EditorGUILayout.BeginFadeGroup(showSettings.faded)) {
				EditorGUI.indentLevel++;
				
				EditorGUILayout.BeginVertical();
				GLTFSceneExporter.ExportFullPath = EditorGUILayout.Toggle("Export using original path", GLTFSceneExporter.ExportFullPath);
				GLTFSceneExporter.ExportNames = EditorGUILayout.Toggle("Export names of nodes", GLTFSceneExporter.ExportNames);
				GLTFSceneExporter.RequireExtensions= EditorGUILayout.Toggle("Require extensions", GLTFSceneExporter.RequireExtensions);
				EditorGUILayout.EndVertical();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.Separator();
			
			EditorGUILayout.BeginVertical();
			EditorGUILayout.HelpBox("Select a GameObject from the scene to export into GLTF", MessageType.Info);
			selectedObject = (GameObject) EditorGUILayout.ObjectField("Export Object", selectedObject, typeof(GameObject), true);
			EditorGUILayout.EndVertical();
			
			if (selectedObject == null) {
				EditorGUILayout.BeginVertical();
				EditorGUILayout.HelpBox("You need to select a GameObject to continue", MessageType.Error);
				EditorGUILayout.EndVertical();
			}
			else {
				string selectionName = selectedObject.name;
				
				// ensure the name of our object is valid
				if (string.IsNullOrEmpty(selectionName) || Regex.Matches(selectionName,@"[a-zA-Z]").Count <= 0) {
					EditorGUILayout.BeginVertical();
					EditorGUILayout.HelpBox("Your Selected GameObject does not have a valid name", MessageType.Error);
					EditorGUILayout.EndVertical();

					return;
				}
				
				EditorGUILayout.Separator();
				EditorGUILayout.BeginVertical();
				
				showExportSettings.target = EditorGUILayout.ToggleLeft("Generator Options", showExportSettings.target);
			
				//Extra block that can be toggled on and off.
				if (EditorGUILayout.BeginFadeGroup(showExportSettings.faded)) {
					EditorGUI.indentLevel++;
					
					EditorGUILayout.BeginVertical();
					PlattarExporterOptions.IsZip = EditorGUILayout.Toggle("Export Zip Archive", PlattarExporterOptions.IsZip);
					EditorGUILayout.EndVertical();
					EditorGUI.indentLevel--;
				}
				
				EditorGUILayout.EndFadeGroup();

				if (GUILayout.Button("Export " + selectionName + " to GLTF")) {
					if (!PlattarExporterOptions.IsZip) {
						if (GenerateGLTF(selectedObject) != null) {
							EditorUtility.DisplayDialog("Successful Export", "GLTF Exported Successfully", "OK");
						}
					}
					else {
						if (GenerateGLTFZipped(selectedObject) != null) {
							EditorUtility.DisplayDialog("Successful Export", "GLTF Exported and Zipped Successfully", "OK");
						}
					}
				}
				
				EditorGUILayout.EndVertical();
			}
		}
		
		/**
		 * Generate the GLTF file and zip it all up
		 */
		public static Tuple<string, string> GenerateGLTFZipped(GameObject selectedObject) {
			var path = GenerateGLTF(selectedObject);
			
			if (path == null) {
				return path;
			}
			
			// otherwise, we need to zip up the entire directory
			// and delete the original
			string selectionName = selectedObject.name;
			PlattarExporterOptions.CompressDirectory(path.Item2, path.Item1 + "/" + selectionName);
			PlattarExporterOptions.DeleteDirectory(path.Item2);

			return path;
		}

		/**
		 * Generate a non-zipped GLTF file with all folders etc
		 */
		public static Tuple<string, string> GenerateGLTF(GameObject selectedObject) {
			var exporter = new GLTFSceneExporter(new Transform[] { selectedObject.transform }, RetrieveTexturePath);
			string selectionName = selectedObject.name;

			var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
			if (!string.IsNullOrEmpty(path)) {
				var newpath = path + "/" + selectionName + "_export_gltf";
				DirectoryInfo info = Directory.CreateDirectory(newpath);
				
				if (info.Exists) {
					exporter.SaveGLTFandBin(newpath, selectionName);

					return new Tuple<string, string>(path, newpath);
				}
				
				EditorUtility.DisplayDialog("Failed Export", "GLTF Could not be exported, could not create export path", "OK");
			}
			else {
				EditorGUILayout.HelpBox("Failed to export since the path is invalid", MessageType.Error);
				EditorUtility.DisplayDialog("Failed Export", "GLTF Could not be exported, invalid export path provided", "OK");
			}

			return null;
		}

		public static string RetrieveTexturePath(UnityEngine.Texture texture) {
			return AssetDatabase.GetAssetPath(texture);
		}
	}
}