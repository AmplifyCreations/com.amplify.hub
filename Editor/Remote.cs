// Amplify Hub
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AmplifyHub
{
	[Serializable]
	public class RemoteProduct
	{
		public string name;
		public string url;
	}

	public static class AmplifyHubRemote
	{
		private const string Url = "https://amplify.pt/Banner/Products.json";

		public static void Fetch( Action<List<RemoteProduct>> callback )
		{
			var req = UnityWebRequest.Get( Url );
			var op = req.SendWebRequest();

			op.completed += _ =>
			{
				var list = new List<RemoteProduct>();

				if ( req.result == UnityWebRequest.Result.Success )
				{
					string wrapped = "{\"items\":" + req.downloadHandler.text + "}";
					var wrapper = JsonUtility.FromJson<Wrapper>( wrapped );
					if ( wrapper != null )
					{
						list.AddRange( wrapper.items );
					}
				}

				callback?.Invoke( list );
			};
		}

		[Serializable]
		private class Wrapper
		{
			public RemoteProduct[] items;
		}
	}
}