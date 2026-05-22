// Amplify Hub
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyHub
{
	public class AmplifyHubWindow : EditorWindow
	{
		private List<RemoteProduct> m_remote = new List<RemoteProduct>();

		[MenuItem( "Window/Amplify Hub" )]
		public static void Open()
		{
			GetWindow<AmplifyHubWindow>( "Amplify Hub" );
		}

		private void OnEnable()
		{
			AmplifyHubRemote.Fetch( list =>
			{
				m_remote = list;
				Repaint();
			} );
		}

		private void OnGUI()
		{
			GUILayout.Space( 10 );

			GUILayout.Label( "Installed Products", EditorStyles.boldLabel );
			foreach ( var p in AmplifyHubRegistry.InstalledProducts() )
			{
				DrawInstalled( p );
			}

			GUILayout.Space( 20 );

			GUILayout.Label( "Available Products", EditorStyles.boldLabel );
			foreach ( var p in m_remote )
			{
				if ( !AmplifyHubRegistry.IsInstalled( p.name ) )
				{
					DrawRemote( p );
				}
			}
		}

		void DrawInstalled( AmplifyProduct p )
		{
			EditorGUILayout.BeginHorizontal( "box" );
			{
				GUILayout.Label( $"{p.Name} ({p.Version})" );

				GUILayout.FlexibleSpace();

				if ( GUILayout.Button( "Open", GUILayout.Width( 80 ) ) )
				{
					Debug.Log( $"Open {p.Name}" );
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		void DrawRemote( RemoteProduct p )
		{
			EditorGUILayout.BeginHorizontal( "box" );
			{
				GUILayout.Label( p.name );

				GUILayout.FlexibleSpace();

				if ( GUILayout.Button( "Store", GUILayout.Width( 80 ) ) )
				{
					Application.OpenURL( p.url );
				}
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}