// Amplify Hub
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyHub
{
	[Serializable]
	public class Product
	{
		public string name;
		public string shortDescription;
		public string url;
		public bool free;
		public bool installed;
		public string version;
	}

	public static class Registry
	{
		private const string ProductCatalogGUID = "cfca246791520724b8f7363d585bf36b";

		private static readonly Dictionary<string, Product> s_products;

		static Registry()
		{
			s_products = new Dictionary<string, Product>();
			foreach ( var p in LoadCatalog() )
			{
				s_products[ p.name ] = p;
			}
		}

		public static void MarkInstalled( string name, string version )
		{
			if ( !s_products.TryGetValue( name, out var product ) )
			{
				return;
			}

			product.installed = true;
			product.version = version;
		}

		public static IEnumerable<Product> AllProducts()
		{
			return s_products.Values;
		}

		public static bool IsInstalled( string name )
		{
			return s_products.TryGetValue( name, out var p ) && p.installed;
		}

		private static List<Product> LoadCatalog()
		{
			var asset = AssetDatabase.LoadAssetAtPath<TextAsset>( AssetDatabase.GUIDToAssetPath( ProductCatalogGUID ) );
			if ( asset != null )
			{
				return Parse( asset.text );
			}
			return new List<Product>();				
		}

		private static List<Product> Parse( string json )
		{
			var list = new List<Product>();
			if ( !string.IsNullOrEmpty( json ) )
			{
				string wrapped = "{\"items\":" + json + "}";
				var wrapper = JsonUtility.FromJson<Wrapper>( wrapped );
				if ( wrapper != null )
				{
					list.AddRange( wrapper.items );
				}
			}
			return list;
		}

		[Serializable]
		private class Wrapper
		{
			public Product[] items;
		}
	}
}
