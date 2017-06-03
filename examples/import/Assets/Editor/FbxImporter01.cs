//#define UNI_18972
//#define UNI_18971

// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.  
//
// Licensed under the ##LICENSENAME##. 
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using FbxSdk;

namespace FbxSdk.Examples
{
    namespace Editor
    {

        public class FbxImporter01 : System.IDisposable
        {
            const string Title =
                 "Example 01: import scene and report basic information";

            const string Subject =
                 @"Example FbxImporter01 illustrates how to:
                                1) create and initialize the importer
                                2) report sdk and file versions
                                3) report animation (stack) information
                                3) import the scene
                                4) report scene meta-data
                                5) check if the system unit and axis system are compatible with Unity
                    ";
            
            const string Keywords =
                 "import scene";

            const string Comments =
                 @"";

            const string MenuItemName = "File/Import FBX/01. Import Scene";

            /// <summary>
            /// Create instance of example
            /// </summary>
            public static FbxImporter01 Create () { return new FbxImporter01 (); }

            /// <summary>
            /// Import all from scene.
            /// Return the number of objects we imported.
            /// </summary>
            public int ImportAll (IEnumerable<UnityEngine.Object> unitySelectionSet)
            {
                // Create the FBX manager
                using (var fbxManager = FbxManager.Create ()) 
                {
                    FbxIOSettings fbxIOSettings = FbxIOSettings.Create (fbxManager, Globals.IOSROOT);

                    // Configure the IO settings.
                    fbxManager.SetIOSettings (fbxIOSettings);

                    // Get the version number of the FBX files generated by the
                    // version of FBX SDK that you are using.
                    int sdkMajor = -1, sdkMinor = -1, sdkRevision = -1;

                    FbxManager.GetFileFormatVersion (out sdkMajor, out sdkMinor, out sdkRevision);

                    // Create the importer 
                    var fbxImporter = FbxImporter.Create (fbxManager, "Importer");

                    // Initialize the importer.
                    int fileFormat = -1;

                    bool status = fbxImporter.Initialize (LastFilePath, fileFormat, fbxIOSettings);
                    FbxStatus fbxStatus = fbxImporter.GetStatus ();

                    // Get the version number of the FBX file format.
                    int fileMajor = -1, fileMinor = -1, fileRevision = -1;
                    fbxImporter.GetFileVersion (out fileMajor, out fileMinor, out fileRevision);

                    // Check that initialization of the fbxImporter was successful
                    if (!status) 
                    {
                        Debug.LogError (string.Format ("failed to initialize FbxImporter, error returned {0}", 
                                                       fbxStatus.GetErrorString ()));

                        if (fbxStatus.GetCode () == FbxStatus.EStatusCode.eInvalidFileVersion) 
                        {
                            Debug.LogError (string.Format ("Invalid file version detected\nSDK version: {0}.{1}.{2}\nFile version: {3}.{4}.{5}",
                                                sdkMajor, sdkMinor, sdkRevision,
                                                           fileMajor, fileMinor, fileRevision));

                        }

                        return 0;
                    }

                    MsgLine.Add( "Import Scene Report" );
                    MsgLine.Add( kBorderLine );

                    MsgLine.Add( kPadding + string.Format ("FilePath: {0}", LastFilePath));
                    MsgLine.Add( kPadding + string.Format ("SDK version: {0}.{1}.{2}", 
                                                           sdkMajor, sdkMinor, sdkRevision));

                    if (!fbxImporter.IsFBX ()) 
                    {
                        Debug.LogError (string.Format ("file does not contain FBX data {0}", LastFilePath));
                        return 0;   
                    }

                    MsgLine.Add( kPadding + string.Format ("File version: {0}.{1}.{2}",
                                                           fileMajor, fileMinor, fileRevision));

                    MsgLine.Add( kBorderLine );
                    MsgLine.Add( "Animation" );
                    MsgLine.Add( kBorderLine );

                    int numAnimStack = fbxImporter.GetAnimStackCount ();

                    MsgLine.Add( kPadding + string.Format ("number of stacks: {0}", numAnimStack));
                    MsgLine.Add( kPadding + string.Format ("active animation stack: \"{0}\"\n", fbxImporter.GetActiveAnimStackName()));

                    for (int i = 0; i < numAnimStack; i++) {
#if UNI_18972
                        FbxTakeInfo fbxTakeInfo = fbxImporter.GetTakeInfo (i);
                        MsgLine.Add( kPadding + string.Format ("Animation Stack ({0})", i));
                        MsgLine.Add( kPadding +string.Format ("name: \"{0}\"", fbxTakeInfo.mName) + string.kNewLine);
                        MsgLine.Add( kPadding +string.Format ("description: \"{0}\"", fbxTakeInfo.mDescription));
                        MsgLine.Add( kPadding +string.Format ("import name: \"{0}\"", fbxTakeInfo.mImportName));
                        MsgLine.Add( kPadding +string.Format ("import state: \"{0}\"", fbxTakeInfo.mSelect));
#endif
                    }

                    // Import options. Determine what kind of data is to be imported.
                    // The default is true, but here we set the options explictly.
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_MATERIAL,         false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_TEXTURE,          false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_ANIMATION,        false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_EXTRACT_EMBEDDED_DATA, false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_GLOBAL_SETTINGS,  true);

                    // Create a scene
                    var fbxScene = FbxScene.Create (fbxManager, "Scene");

                    // Import the scene to the file.
                    status = fbxImporter.Import (fbxScene);
                    fbxStatus = fbxImporter.GetStatus ();

                    if (status == false) 
                    {
                        if (fbxStatus.GetCode () == FbxStatus.EStatusCode.ePasswordError) 
                        {
                            Debug.LogError (string.Format ("failed to import, file is password protected ({0})", fbxStatus.GetErrorString ()));
                        } 
                        else 
                        {
                            Debug.LogError (string.Format ("failed to import file ({0})", fbxStatus.GetErrorString ()));
                        }
                    } 
                    else 
                    {
                        // import data into scene 
                        ProcessScene (fbxScene, unitySelectionSet);
                    }

                    // cleanup
                    fbxScene.Destroy ();
                    fbxImporter.Destroy ();

                    return status == true ? NumNodes : 0;
                }
            }

            /// <summary>
            /// Process fbx scene by doing nothing
            /// </summary>
            public void ProcessScene (FbxScene fbxScene, IEnumerable<UnityEngine.Object> unitySelectionSet)
            {
                FbxDocumentInfo sceneInfo = fbxScene.GetSceneInfo ();

                if (sceneInfo != null) 
                {
                    MsgLine.Add( kBorderLine );
                    MsgLine.Add( "Scene Meta-Data" );
                    MsgLine.Add( kBorderLine );

                    MsgLine.Add( kPadding + string.Format ("Title: \"{0}\"", sceneInfo.mTitle));
                    MsgLine.Add( kPadding + string.Format ("Subject: \"{0}\"", sceneInfo.mSubject));
                    MsgLine.Add( kPadding + string.Format ("Author: \"{0}\"", sceneInfo.mAuthor));
                    MsgLine.Add( kPadding + string.Format ("Keywords: \"{0}\"", sceneInfo.mKeywords));
                    MsgLine.Add( kPadding + string.Format ("Revision: \"{0}\"", sceneInfo.mRevision));
                    MsgLine.Add( kPadding + string.Format ("Comment: \"{0}\"", sceneInfo.mComment));
                }

                var fbxSettings = fbxScene.GetGlobalSettings ();

                MsgLine.Add( kBorderLine );
                MsgLine.Add( "Global Settings" );
                MsgLine.Add( kBorderLine );

                FbxSystemUnit fbxSystemUnit = fbxSettings.GetSystemUnit ();

                if (fbxSystemUnit != UnitySystemUnit) 
                {
                    Debug.LogWarning (string.Format("file system unit do not match Unity. Expected {0} Found {1}", 
                                             UnitySystemUnit.ToString(), fbxSystemUnit.ToString()));
                }

                MsgLine.Add (kPadding + string.Format ("SystemUnits: {0}", fbxSystemUnit.ToString()));

                // The Unity axis system has Y up, Z forward, X to the right.
                FbxAxisSystem fbxAxisSystem = fbxSettings.GetAxisSystem();

                if (fbxAxisSystem != UnityAxisSystem)
                {
                   Debug.LogWarning (string.Format ("file axis system do not match Unity, Expected [{0}, {1}, {2}] Found [{3}, {4}, {5}]",
                                                     UnityAxisSystem.GetUpVector().ToString (),
                                                     UnityAxisSystem.GetFrontVector().ToString (),
                                                     UnityAxisSystem.GetCoorSystem().ToString (),
                                                     fbxAxisSystem.GetUpVector().ToString (),
                                                     fbxAxisSystem.GetFrontVector().ToString (),
                                                     fbxAxisSystem.GetCoorSystem().ToString ()));
                }
                MsgLine.Add (kPadding + string.Format ("AxisSystem: {0}", AxisSystemToString(fbxAxisSystem)));

                // print report 
                Debug.Log(string.Join (kNewLine, MsgLine.ToArray ()));

                return;
            }

            // 
            // Create a simple user interface (menu items)
            //
            /// <summary>
            /// create menu item in the File menu
            /// </summary>
            [MenuItem (MenuItemName, false)]
            public static void OnMenuItem ()
            {
                OnImport();
            }

            /// <summary>
            // Validate the menu item defined by the function above.
            /// </summary>
            [MenuItem (MenuItemName, true)]
            public static bool OnValidateMenuItem ()
            {
                // Return true
                return true;
            }

            /// <summary>
            /// Number of nodes imported including siblings and decendents
            /// </summary>
            public int NumNodes { private set; get; }

            /// <summary>
            /// Clean up this class on garbage collection
            /// </summary>
            public void Dispose () { }

            public bool Verbose { private set; get; }

            /// <summary>
            /// manage the selection of a filename
            /// </summary>
            static string LastFilePath { get; set; }
            const string kExtension = "fbx";
            const string kBorderLine = "--------------------";
            const string kNewLine = "\n";
            const string kPadding = "    ";

            List<string> MsgLine = new List<string> ();

            private FbxSystemUnit UnitySystemUnit { get { return FbxSystemUnit.m; } }

            private FbxAxisSystem UnityAxisSystem { 
                get { return new FbxAxisSystem (FbxAxisSystem.EUpVector.eYAxis, 
                                                FbxAxisSystem.EFrontVector.eParityOdd, 
                                                FbxAxisSystem.ECoordSystem.eLeftHanded); }
            }

            private static string AxisSystemToString (FbxAxisSystem fbxAxisSystem)
            {
            	return string.Format ("[{0}, {1}, {2}]",
            						  fbxAxisSystem.GetUpVector ().ToString (),
            						  fbxAxisSystem.GetFrontVector ().ToString (),
            						  fbxAxisSystem.GetCoorSystem ().ToString ());
            }
                             
            // use the SaveFile panel to allow user to enter a file name
            private static void OnImport()
            {
                // Now that we know we have stuff to import, get the user-desired path.
                var directory = string.IsNullOrEmpty (LastFilePath) 
                                      ? Application.dataPath 
                                      : System.IO.Path.GetDirectoryName (LastFilePath);
                
                var title = "Import FBX";

                var filePath = EditorUtility.OpenFilePanel(title, directory, kExtension);

                if (string.IsNullOrEmpty (filePath)) 
                {
                    return;
                }

                LastFilePath = filePath;

                using (var fbxImporter = Create()) 
                {
                    if (fbxImporter.ImportAll(Selection.objects) > 0)
                    {
                        Debug.Log (string.Format ("Successfully imported: {0}", filePath));
                    }
                }
            }
        }
    }
}
